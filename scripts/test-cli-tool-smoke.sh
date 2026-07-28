#!/usr/bin/env bash

set -euo pipefail

SCRIPT_DIRECTORY="$(
  cd -- "$(dirname -- "${BASH_SOURCE[0]}")" &&
  pwd
)"

REPOSITORY_ROOT="$(
  cd -- "$SCRIPT_DIRECTORY/.." &&
  pwd
)"

SOLUTION="$REPOSITORY_ROOT/Fotbiler.RuleGate.slnx"
PACKAGE_DIRECTORY="$REPOSITORY_ROOT/artifacts/packages"

EXPECTED_FRAMEWORKS=(
  "net8.0"
  "net9.0"
  "net10.0"
)

VERSION_PREFIX="$(
  sed -n \
    's:.*<VersionPrefix>\(.*\)</VersionPrefix>.*:\1:p' \
    "$REPOSITORY_ROOT/Directory.Build.props" |
    head -n 1
)"

VERSION_SUFFIX="$(
  sed -n \
    's:.*<VersionSuffix>\(.*\)</VersionSuffix>.*:\1:p' \
    "$REPOSITORY_ROOT/Directory.Build.props" |
    head -n 1
)"

test -n "$VERSION_PREFIX"

if [[ -n "$VERSION_SUFFIX" ]]
then
  PACKAGE_VERSION="$VERSION_PREFIX-$VERSION_SUFFIX"
else
  PACKAGE_VERSION="$VERSION_PREFIX"
fi

PACKAGE_ID="Fotbiler.RuleGate.Cli"
PACKAGE_PATH="$PACKAGE_DIRECTORY/$PACKAGE_ID.$PACKAGE_VERSION.nupkg"

PACKAGES_READY="false"

case "${1:-}" in
  "")
    ;;

  --packages-ready)
    PACKAGES_READY="true"
    ;;

  *)
    echo "Usage: $0 [--packages-ready]"
    exit 2
    ;;
esac

TEMP_DIRECTORY="$(
  mktemp \
    --directory \
    /tmp/rulegate-cli-tool-smoke-XXXXXX
)"

cleanup()
{
  rm -rf "$TEMP_DIRECTORY"
}

trap cleanup EXIT

WORK_DIRECTORY="$TEMP_DIRECTORY/work"
GLOBAL_PACKAGES="$TEMP_DIRECTORY/global-packages"

mkdir -p \
  "$WORK_DIRECTORY" \
  "$GLOBAL_PACKAGES"

export NUGET_PACKAGES="$GLOBAL_PACKAGES"
export DOTNET_CLI_HOME="$TEMP_DIRECTORY/dotnet-home"
export DOTNET_NOLOGO=true
export DOTNET_CLI_TELEMETRY_OPTOUT=true
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=true

if [[ "$PACKAGES_READY" == "false" ]]
then
  printf '\n== Restore solution ==\n'

  dotnet restore \
    "$SOLUTION"

  printf '\n== Build solution ==\n'

  dotnet build \
    "$SOLUTION" \
    --configuration Release \
    --no-restore

  printf '\n== Test solution ==\n'

  dotnet test \
    "$SOLUTION" \
    --configuration Release \
    --no-build

  printf '\n== Pack solution ==\n'

  rm -rf "$PACKAGE_DIRECTORY"

  dotnet pack \
    "$SOLUTION" \
    --configuration Release \
    --no-build
fi

printf '\n== Verify CLI package ==\n'

test -f "$PACKAGE_PATH"

PACKAGE_FILES="$(
  unzip -Z1 "$PACKAGE_PATH"
)"

grep -Fx \
  'README.md' \
  <<<"$PACKAGE_FILES" \
  >/dev/null

for framework in "${EXPECTED_FRAMEWORKS[@]}"
do
  grep -Fx \
    "tools/$framework/any/DotnetToolSettings.xml" \
    <<<"$PACKAGE_FILES" \
    >/dev/null

  grep -Fx \
    "tools/$framework/any/Fotbiler.RuleGate.Cli.dll" \
    <<<"$PACKAGE_FILES" \
    >/dev/null

  grep -Fx \
    "tools/$framework/any/Fotbiler.RuleGate.Manifest.dll" \
    <<<"$PACKAGE_FILES" \
    >/dev/null

  printf 'Package asset verified: %s\n' \
    "$framework"
done

printf 'Package: %s\n' \
  "$(basename "$PACKAGE_PATH")"

cat > "$WORK_DIRECTORY/rulegate.yaml" <<'YAML'
schemaVersion: 1

application:
  id: cli-smoke
  name: CLI Smoke

policies:
  - id: documents.read
    resourceType: document
    action: read
    requirement:
      permission: DOC.READ
YAML

cat > "$WORK_DIRECTORY/invalid.yaml" <<'YAML'
schemaVersion: 999

application:
  id: cli-smoke
  name: CLI Smoke

policies: []
YAML

for framework in "${EXPECTED_FRAMEWORKS[@]}"
do
  printf '\n========================================\n'
  printf 'CLI FRAMEWORK: %s\n' "$framework"
  printf '========================================\n'

  TOOL_DIRECTORY="$TEMP_DIRECTORY/tool-$framework"

  mkdir -p "$TOOL_DIRECTORY"

  printf '\n== Install exact local package ==\n'

  dotnet tool install \
    "$PACKAGE_ID" \
    --tool-path "$TOOL_DIRECTORY" \
    --version "$PACKAGE_VERSION" \
    --source "$PACKAGE_DIRECTORY" \
    --framework "$framework" \
    --no-cache

  CLI="$TOOL_DIRECTORY/rulegate"

  test -x "$CLI"

  printf '\n== Verify root help ==\n'

  "$CLI" \
    --help \
    >"$TEMP_DIRECTORY/help-$framework.out" \
    2>"$TEMP_DIRECTORY/help-$framework.err"

  test ! -s "$TEMP_DIRECTORY/help-$framework.err"

  grep -F \
    'validate' \
    "$TEMP_DIRECTORY/help-$framework.out" \
    >/dev/null

  grep -F \
    'info' \
    "$TEMP_DIRECTORY/help-$framework.out" \
    >/dev/null

  grep -F \
    'rulegate [command] [options]' \
    "$TEMP_DIRECTORY/help-$framework.out" \
    >/dev/null

  if grep -F \
    'Fotbiler.RuleGate.Cli [command]' \
    "$TEMP_DIRECTORY/help-$framework.out"
  then
    printf 'Assembly name leaked into help usage.\n' >&2
    exit 1
  fi

  printf '\n== Verify validate help ==\n'

  "$CLI" \
    validate \
    --help \
    >"$TEMP_DIRECTORY/validate-help-$framework.out" \
    2>"$TEMP_DIRECTORY/validate-help-$framework.err"

  test ! -s \
    "$TEMP_DIRECTORY/validate-help-$framework.err"

  grep -F \
    -- '--format' \
    "$TEMP_DIRECTORY/validate-help-$framework.out" \
    >/dev/null

  grep -F \
    'rulegate validate [<file>] [options]' \
    "$TEMP_DIRECTORY/validate-help-$framework.out" \
    >/dev/null

  printf '\n== Verify version ==\n'

  "$CLI" \
    --version \
    >"$TEMP_DIRECTORY/version-$framework.out" \
    2>"$TEMP_DIRECTORY/version-$framework.err"

  test ! -s \
    "$TEMP_DIRECTORY/version-$framework.err"

  grep -F \
    "$PACKAGE_VERSION" \
    "$TEMP_DIRECTORY/version-$framework.out" \
    >/dev/null

  printf '\n== Verify info ==\n'

  "$CLI" \
    info \
    >"$TEMP_DIRECTORY/info-$framework.out" \
    2>"$TEMP_DIRECTORY/info-$framework.err"

  test ! -s "$TEMP_DIRECTORY/info-$framework.err"

  grep -F \
    "Version: $PACKAGE_VERSION" \
    "$TEMP_DIRECTORY/info-$framework.out" \
    >/dev/null

  grep -F \
    'Default manifest: rulegate.yaml' \
    "$TEMP_DIRECTORY/info-$framework.out" \
    >/dev/null

  grep -F \
    'Supported schema version: 1' \
    "$TEMP_DIRECTORY/info-$framework.out" \
    >/dev/null

  printf '\n== Verify default manifest discovery ==\n'

  (
    cd "$WORK_DIRECTORY"

    "$CLI" \
      validate \
      >"$TEMP_DIRECTORY/valid-$framework.out" \
      2>"$TEMP_DIRECTORY/valid-$framework.err"
  )

  test ! -s "$TEMP_DIRECTORY/valid-$framework.err"

  grep -F \
    'RuleGate manifest is valid.' \
    "$TEMP_DIRECTORY/valid-$framework.out" \
    >/dev/null

  grep -F \
    'Policies: 1' \
    "$TEMP_DIRECTORY/valid-$framework.out" \
    >/dev/null

  printf '\n== Verify JSON validation failure ==\n'

  set +e

  "$CLI" \
    validate \
    "$WORK_DIRECTORY/invalid.yaml" \
    --format json \
    >"$TEMP_DIRECTORY/invalid-$framework.json" \
    2>"$TEMP_DIRECTORY/invalid-$framework.err"

  INVALID_EXIT_CODE="$?"

  set -e

  test "$INVALID_EXIT_CODE" -eq 1
  test ! -s "$TEMP_DIRECTORY/invalid-$framework.err"

  python3 - \
    "$TEMP_DIRECTORY/invalid-$framework.json" <<'PY'
import json
from pathlib import Path
import sys

path = Path(sys.argv[1])
payload = json.loads(path.read_text(encoding="utf-8"))

assert payload["isValid"] is False
assert payload["policyCount"] == 0
assert payload["errors"]
assert (
    payload["errors"][0]["category"]
    == "validation"
)
assert (
    payload["errors"][0]["code"]
    == "MANIFEST_UNSUPPORTED_SCHEMA_VERSION"
)

print("JSON validation output verified.")
PY

  printf '\n== Verify usage exit code ==\n'

  set +e

  "$CLI" \
    validte \
    >"$TEMP_DIRECTORY/usage-$framework.out" \
    2>"$TEMP_DIRECTORY/usage-$framework.err"

  USAGE_EXIT_CODE="$?"

  set -e

  test "$USAGE_EXIT_CODE" -eq 2

  grep -F \
    'error:' \
    "$TEMP_DIRECTORY/usage-$framework.err" \
    >/dev/null

  grep -F \
    'rulegate --help' \
    "$TEMP_DIRECTORY/usage-$framework.err" \
    >/dev/null

  printf '\nCLI smoke passed for %s.\n' \
    "$framework"
done

printf '\nPackaged RuleGate CLI smoke tests passed on all frameworks.\n'
