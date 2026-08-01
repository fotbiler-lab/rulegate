#!/usr/bin/env bash
set -euo pipefail

REPOSITORY_ROOT="$(
  cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." &&
  pwd
)"

cd "$REPOSITORY_ROOT"

PYTHON_SCRIPT="scripts/verify-npm-artifact-reproducibility.py"

CLIENT_SOURCE="src/Fotbiler.RuleGate.Client"
MODERN_BUILD="dist/rulegate-angular"
LEGACY_BUILD="dist/rulegate-angular-legacy"

MODERN_WORK="compatibility/angular-modern-builder/.work"
LEGACY_WORK="compatibility/angular-legacy-builder/.work"

DELETED_LEGACY_MANIFEST="compatibility/angular-legacy-builder/package.json"

PACKAGE_MANIFESTS=(
  "src/Fotbiler.RuleGate.Client/package.json"
  "src/Fotbiler.RuleGate.Angular/package.json"
  "src/Fotbiler.RuleGate.Angular.Legacy/package.json"
)

test -x "$PYTHON_SCRIPT"
test ! -e "$DELETED_LEGACY_MANIFEST"

TMP_DIRECTORY="$(
  mktemp \
    --directory \
    /tmp/rulegate-npm-reproducibility-XXXXXX
)"

RUN_ONE="$TMP_DIRECTORY/run-one"
RUN_TWO="$TMP_DIRECTORY/run-two"
REPORT="$TMP_DIRECTORY/report.txt"

cleanup()
{
  rm -rf \
    "$CLIENT_SOURCE/dist" \
    "$MODERN_BUILD" \
    "$LEGACY_BUILD" \
    "$MODERN_WORK" \
    "$LEGACY_WORK"

  rm -f "$DELETED_LEGACY_MANIFEST"
  rm -rf "$TMP_DIRECTORY"
}

trap cleanup EXIT

mkdir -p \
  "$RUN_ONE" \
  "$RUN_TWO"

NPM_VERSION="$(
  PYTHONDONTWRITEBYTECODE=1 \
  python3 - \
    "${PACKAGE_MANIFESTS[@]}" <<'PY'
from pathlib import Path
import json
import sys

expected_names = {
    "@fotbiler/rulegate-client",
    "@fotbiler/rulegate-angular",
    "@fotbiler/rulegate-angular-legacy",
}

packages = {}

for value in sys.argv[1:]:
    path = Path(value)

    payload = json.loads(
        path.read_text(
            encoding="utf-8"
        )
    )

    name = payload.get(
        "name"
    )

    version = payload.get(
        "version"
    )

    if name not in expected_names:
        raise SystemExit(
            f"ERROR: unexpected npm package: {name!r}"
        )

    if name in packages:
        raise SystemExit(
            f"ERROR: duplicate npm package: {name}"
        )

    if payload.get(
        "private",
        False,
    ):
        raise SystemExit(
            f"ERROR: publishable package is private: {name}"
        )

    publish_config = payload.get(
        "publishConfig",
        {},
    )

    if publish_config.get(
        "access"
    ) != "public":
        raise SystemExit(
            f"ERROR: public npm access is missing: {name}"
        )

    if publish_config.get(
        "provenance"
    ) is not True:
        raise SystemExit(
            f"ERROR: npm provenance is disabled: {name}"
        )

    packages[name] = version

if set(packages) != expected_names:
    raise SystemExit(
        "ERROR: npm package inventory differs."
    )

versions = set(
    packages.values()
)

if len(versions) != 1:
    raise SystemExit(
        "ERROR: npm package-family versions differ: "
        f"{sorted(versions)!r}"
    )

version = versions.pop()

if not isinstance(
    version,
    str,
) or not version:
    raise SystemExit(
        "ERROR: invalid npm package version."
    )

print(version)
PY
)"

EXPECTED_FILES=(
  "fotbiler-rulegate-client-$NPM_VERSION.tgz"
  "fotbiler-rulegate-angular-$NPM_VERSION.tgz"
  "fotbiler-rulegate-angular-legacy-$NPM_VERSION.tgz"
)

PYTHONDONTWRITEBYTECODE=1 \
  python3 \
    "$PYTHON_SCRIPT" \
    --self-test

pnpm install \
  --frozen-lockfile

pnpm audit \
  --prod

pnpm audit

pnpm api:frontend:check

PYTHONDONTWRITEBYTECODE=1 \
python3 <<'PY'
from pathlib import Path
import re

workspace = Path(
    "pnpm-workspace.yaml"
).read_text(
    encoding="utf-8"
)

if not re.search(
    r"(?m)^linkWorkspacePackages:\s*false\s*$",
    workspace,
):
    raise SystemExit(
        "ERROR: linkWorkspacePackages must remain false."
    )

match = re.search(
    r"(?ms)^allowBuilds:\n"
    r"(?P<body>(?:  [^\n]+\n)+)",
    workspace,
)

if match is None:
    raise SystemExit(
        "ERROR: allowBuilds block is missing."
    )

actual = {}

for line in match.group("body").splitlines():
    key, separator, value = line.strip().partition(":")

    if not separator:
        raise SystemExit(
            f"ERROR: malformed allowBuilds entry: {line!r}"
        )

    actual[
        key.strip("'\"")
    ] = (
        value.strip()
        == "true"
    )

expected = {
    "@parcel/watcher": False,
    "esbuild": True,
    "lmdb": False,
    "msgpackr-extract": False,
}

if actual != expected:
    raise SystemExit(
        "ERROR: workspace build policy differs.\n"
        f"Expected: {expected!r}\n"
        f"Actual  : {actual!r}"
    )

lockfile = Path(
    "pnpm-lock.yaml"
).read_text(
    encoding="utf-8"
)

for description, pattern in {
    "Git dependency": r"(?mi)\bgit\+",
    "GitHub dependency": r"(?mi)(?:^|\s)github:",
    "HTTP dependency": r"(?mi)(?:^|\s)https?://",
    "file dependency": r"(?mi)(?:^|\s)file:",
    "external tarball": r"(?mi)(?:^|\s)tarball:",
}.items():
    if re.search(
        pattern,
        lockfile,
    ):
        raise SystemExit(
            "ERROR: forbidden registry-external "
            f"dependency: {description}"
        )

links = sorted(
    re.findall(
        r"(?m)^\s*version:\s+"
        r"(link:[^\s]+)\s*$",
        lockfile,
    )
)

expected_links = sorted(
    [
        "link:src/Fotbiler.RuleGate.Client",
        "link:../../src/Fotbiler.RuleGate.Client",
        "link:../Fotbiler.RuleGate.Client",
    ]
)

if links != expected_links:
    raise SystemExit(
        "ERROR: local link inventory differs.\n"
        f"Expected: {expected_links!r}\n"
        f"Actual  : {links!r}"
    )

print(
    "NPM_SUPPLY_CHAIN_STATIC_POLICY_VERIFIED"
)
PY

clean_outputs()
{
  rm -rf \
    "$CLIENT_SOURCE/dist" \
    "$MODERN_BUILD" \
    "$LEGACY_BUILD" \
    "$MODERN_WORK" \
    "$LEGACY_WORK"

  rm -f "$DELETED_LEGACY_MANIFEST"
}

build_and_pack()
{
  local destination="$1"

  clean_outputs
  mkdir -p "$destination"

  pnpm angular:build

  test -f \
    "$CLIENT_SOURCE/dist/index.js"

  test -f \
    "$CLIENT_SOURCE/dist/index.d.ts"

  test -f \
    "$MODERN_BUILD/fesm2022/fotbiler-rulegate-angular.mjs"

  test -f \
    "$MODERN_BUILD/fesm2022/fotbiler-rulegate-angular-keycloak.mjs"

  test -f \
    "$LEGACY_BUILD/fesm2015/fotbiler-rulegate-angular-legacy.js"

  test -f \
    "$LEGACY_BUILD/fotbiler-rulegate-angular-legacy.d.ts"

  test ! -e "$DELETED_LEGACY_MANIFEST"

  pnpm \
    --dir "$CLIENT_SOURCE" \
    pack \
    --pack-destination "$destination"

  pnpm \
    --dir "$MODERN_BUILD" \
    pack \
    --pack-destination "$destination"

  pnpm \
    --dir "$LEGACY_BUILD" \
    pack \
    --pack-destination "$destination"

  test "$(
    find "$destination" \
      -maxdepth 1 \
      -type f \
      -name '*.tgz' |
    wc -l
  )" -eq 3

  for filename in "${EXPECTED_FILES[@]}"
  do
    test -f \
      "$destination/$filename"
  done
}

build_and_pack "$RUN_ONE"
sleep 2
build_and_pack "$RUN_TWO"

PYTHONDONTWRITEBYTECODE=1 \
  python3 \
    "$PYTHON_SCRIPT" \
    "$RUN_ONE" \
    "$RUN_TWO" \
    "$NPM_VERSION" |
  tee "$REPORT"

grep -F -q \
  'npm artifacts checked       : 3' \
  "$REPORT"

grep -F -q \
  'Raw archive mismatches       : 0' \
  "$REPORT"

grep -F -q \
  'Meaningful payload mismatches: 0' \
  "$REPORT"

grep -F -q \
  'Metadata mismatches          : 0' \
  "$REPORT"

grep -F -q \
  'Entry-order mismatches       : 0' \
  "$REPORT"

grep -F -q \
  'NPM_TARBALLS_BYTE_FOR_BYTE_REPRODUCIBLE' \
  "$REPORT"

grep -F -q \
  'NPM_ARTIFACT_REPRODUCIBILITY_VERIFIED' \
  "$REPORT"

clean_outputs

echo
echo 'PERMANENT_NPM_ARTIFACT_REPRODUCIBILITY_GATE_PASSED'
