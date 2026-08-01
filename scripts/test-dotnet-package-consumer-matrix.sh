#!/usr/bin/env bash
set -euo pipefail

REPOSITORY_ROOT="$(
  cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." &&
  pwd
)"

cd "$REPOSITORY_ROOT"

CURRENT_VERSION="${1:-1.0.0-rc.1}"
BASELINE_VERSION="${2:-0.9.0-preview.4}"

if [[ "$#" -ne 0 && "$#" -ne 2 ]]
then
  echo "Usage: $0 [<current-version> <published-baseline-version>]"
  exit 2
fi

SOLUTION="$REPOSITORY_ROOT/Fotbiler.RuleGate.slnx"

MAIN_CONSUMER_DIRECTORY="$REPOSITORY_ROOT/tests/Fotbiler.RuleGate.PackageConsumer.Smoke"
MAIN_CONSUMER_PROJECT="$MAIN_CONSUMER_DIRECTORY/Fotbiler.RuleGate.PackageConsumer.Smoke.csproj"

KEYCLOAK_CONSUMER_DIRECTORY="$REPOSITORY_ROOT/tests/Fotbiler.RuleGate.Keycloak.PackageConsumer.Smoke"
KEYCLOAK_CONSUMER_PROJECT="$KEYCLOAK_CONSUMER_DIRECTORY/Fotbiler.RuleGate.Keycloak.PackageConsumer.Smoke.csproj"

PROJECTS=(
  "src/Fotbiler.RuleGate.Abstractions/Fotbiler.RuleGate.Abstractions.csproj"
  "src/Fotbiler.RuleGate.Core/Fotbiler.RuleGate.Core.csproj"
  "src/Fotbiler.RuleGate.Manifest/Fotbiler.RuleGate.Manifest.csproj"
  "src/Fotbiler.RuleGate.AspNetCore/Fotbiler.RuleGate.AspNetCore.csproj"
  "src/Fotbiler.RuleGate.Cli/Fotbiler.RuleGate.Cli.csproj"
  "src/Fotbiler.RuleGate.Keycloak/Fotbiler.RuleGate.Keycloak.csproj"
)

PACKAGE_IDS=(
  "Fotbiler.RuleGate.Abstractions"
  "Fotbiler.RuleGate.Core"
  "Fotbiler.RuleGate.Manifest"
  "Fotbiler.RuleGate.AspNetCore"
  "Fotbiler.RuleGate.Cli"
  "Fotbiler.RuleGate.Keycloak"
)

CURRENT_FRAMEWORKS=(
  "net8.0"
  "net9.0"
  "net10.0"
)

LEGACY_FRAMEWORKS=(
  "netcoreapp3.1:3.1"
  "net5.0:5.0"
  "net6.0:6.0"
  "net7.0:7.0"
)

NUGET_SOURCE="https://api.nuget.org/v3/index.json"

if ! command -v docker >/dev/null 2>&1
then
  echo "ERROR: Docker is required for legacy runtime verification."
  exit 1
fi

for project in \
  "$MAIN_CONSUMER_PROJECT" \
  "$KEYCLOAK_CONSUMER_PROJECT"
do
  test -f "$project"

  if grep -q '<ProjectReference' "$project"
  then
    echo "ERROR: package consumer contains ProjectReference: $project"
    exit 1
  fi

  grep -F -q \
    'Version="$(RuleGatePackageVersion)"' \
    "$project"
done

TMP_DIRECTORY="$(
  mktemp \
    --directory \
    /tmp/rulegate-package-consumer-matrix-XXXXXX
)"

cleanup()
{
  rm -rf "$TMP_DIRECTORY"

  rm -rf \
    "$MAIN_CONSUMER_DIRECTORY/bin" \
    "$MAIN_CONSUMER_DIRECTORY/obj" \
    "$KEYCLOAK_CONSUMER_DIRECTORY/bin" \
    "$KEYCLOAK_CONSUMER_DIRECTORY/obj"
}

trap cleanup EXIT

CURRENT_PACKAGE_DIRECTORY="$TMP_DIRECTORY/current-packages"

mkdir -p "$CURRENT_PACKAGE_DIRECTORY"

export DOTNET_NOLOGO=true
export DOTNET_CLI_TELEMETRY_OPTOUT=true
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=true

printf '\n========================================\n'
printf 'Build current local RC packages\n'
printf '========================================\n'

dotnet restore \
  "$SOLUTION" \
  --locked-mode

for project in "${PROJECTS[@]}"
do
  dotnet pack \
    "$project" \
    --configuration Release \
    --no-restore \
    -p:Version="$CURRENT_VERSION" \
    -p:ContinuousIntegrationBuild=true \
    -p:Deterministic=true \
    -p:DebugType=portable \
    --output "$CURRENT_PACKAGE_DIRECTORY"
done

for package_id in "${PACKAGE_IDS[@]}"
do
  test -f \
    "$CURRENT_PACKAGE_DIRECTORY/$package_id.$CURRENT_VERSION.nupkg"

  test -f \
    "$CURRENT_PACKAGE_DIRECTORY/$package_id.$CURRENT_VERSION.snupkg"
done

test "$(
  find "$CURRENT_PACKAGE_DIRECTORY" \
    -maxdepth 1 \
    -type f \
    \( \
      -name '*.nupkg' \
      -o \
      -name '*.snupkg' \
    \) |
    wc -l
)" -eq 12

verify_restored_packages()
{
  local mode="$1"
  local version="$2"
  local cache="$3"
  local project="$4"
  local assets_file="$5"
  local local_source="${6:-}"

  shift 6

  local expected_packages=(
    "$@"
  )

  PYTHONDONTWRITEBYTECODE=1 \
  python3 - \
    "$mode" \
    "$version" \
    "$cache" \
    "$assets_file" \
    "$local_source" \
    "${expected_packages[@]}" <<'PY'
from __future__ import annotations

from base64 import b64encode
from hashlib import sha512
from pathlib import Path
from urllib.parse import unquote, urlparse
import json
import sys

mode = sys.argv[1]
version = sys.argv[2]
cache = Path(sys.argv[3])
assets_path = Path(sys.argv[4])
local_source_value = sys.argv[5]
package_ids = sys.argv[6:]

assets = json.loads(
    assets_path.read_text(
        encoding="utf-8"
    )
)

expected_frameworks = {
    "netcoreapp3.1",
    "net5.0",
    "net6.0",
    "net7.0",
    "net8.0",
    "net9.0",
    "net10.0",
}

actual_frameworks = set(
    assets.get(
        "project",
        {},
    ).get(
        "frameworks",
        {},
    )
)

if actual_frameworks != expected_frameworks:
    raise SystemExit(
        "ERROR: restored framework matrix differs.\n"
        f"Expected: {sorted(expected_frameworks)!r}\n"
        f"Actual: {sorted(actual_frameworks)!r}"
    )

libraries = set(
    assets.get(
        "libraries",
        {},
    )
)

for package_id in package_ids:
    identity = f"{package_id}/{version}"

    if identity not in libraries:
        raise SystemExit(
            "ERROR: package was not restored at "
            f"the exact version: {identity}"
        )

    package_root = (
        cache
        / package_id.lower()
        / version
    )

    metadata_path = (
        package_root
        / ".nupkg.metadata"
    )

    if not metadata_path.is_file():
        raise SystemExit(
            "ERROR: NuGet package metadata is missing: "
            f"{metadata_path}"
        )

    metadata = json.loads(
        metadata_path.read_text(
            encoding="utf-8"
        )
    )

    source = str(
        metadata.get(
            "source",
            "",
        )
    )

    content_hash = str(
        metadata.get(
            "contentHash",
            "",
        )
    )

    if mode == "current":
        local_source = Path(
            local_source_value
        ).resolve()

        parsed = urlparse(
            source
        )

        if parsed.scheme == "file":
            source_path = Path(
                unquote(parsed.path)
            ).resolve()
        else:
            source_path = Path(
                source
            ).resolve()

        if source_path.is_file():
            source_path = source_path.parent

        if source_path != local_source:
            raise SystemExit(
                "ERROR: current RuleGate package was "
                "not restored from the local RC feed.\n"
                f"Package: {package_id}\n"
                f"Source : {source}"
            )

        package_path = (
            local_source
            / f"{package_id}.{version}.nupkg"
        )

        if not package_path.is_file():
            raise SystemExit(
                "ERROR: expected local package is missing: "
                f"{package_path}"
            )

        expected_hash = b64encode(
            sha512(
                package_path.read_bytes()
            ).digest()
        ).decode("ascii")

        if content_hash != expected_hash:
            raise SystemExit(
                "ERROR: restored current package hash "
                "does not match the locally packed artifact.\n"
                f"Package : {package_id}\n"
                f"Expected: {expected_hash}\n"
                f"Actual  : {content_hash}"
            )

    elif mode == "published":
        if "nuget.org" not in source.lower():
            raise SystemExit(
                "ERROR: published baseline package "
                "was not restored from NuGet.org.\n"
                f"Package: {package_id}\n"
                f"Source : {source}"
            )

    else:
        raise SystemExit(
            f"ERROR: unsupported mode: {mode}"
        )

print(
    f"{mode} package graph verified for "
    f"{assets_path.parent.parent.name}: "
    + ", ".join(package_ids)
)
PY
}

run_consumer_mode()
{
  local mode="$1"
  local version="$2"
  local local_source="${3:-}"

  local cache="$TMP_DIRECTORY/$mode-global-packages"

  rm -rf \
    "$cache" \
    "$MAIN_CONSUMER_DIRECTORY/bin" \
    "$MAIN_CONSUMER_DIRECTORY/obj" \
    "$KEYCLOAK_CONSUMER_DIRECTORY/bin" \
    "$KEYCLOAK_CONSUMER_DIRECTORY/obj"

  mkdir -p "$cache"

  export NUGET_PACKAGES="$cache"

  local source_arguments=()

  if [[ "$mode" == "current" ]]
  then
    test -d "$local_source"

    source_arguments=(
      --source "$local_source"
      --source "$NUGET_SOURCE"
    )
  elif [[ "$mode" == "published" ]]
  then
    source_arguments=(
      --source "$NUGET_SOURCE"
    )
  else
    echo "ERROR: unsupported consumer mode: $mode"
    exit 1
  fi

  printf '\n========================================\n'
  printf 'Restore %s consumer matrix: %s\n' \
    "$mode" \
    "$version"
  printf '========================================\n'

  dotnet restore \
    "$MAIN_CONSUMER_PROJECT" \
    --force \
    --no-cache \
    -p:RuleGatePackageVersion="$version" \
    "${source_arguments[@]}"

  dotnet restore \
    "$KEYCLOAK_CONSUMER_PROJECT" \
    --force \
    --no-cache \
    -p:RuleGatePackageVersion="$version" \
    "${source_arguments[@]}"

  verify_restored_packages \
    "$mode" \
    "$version" \
    "$cache" \
    "$MAIN_CONSUMER_PROJECT" \
    "$MAIN_CONSUMER_DIRECTORY/obj/project.assets.json" \
    "$local_source" \
    Fotbiler.RuleGate.Abstractions \
    Fotbiler.RuleGate.Core \
    Fotbiler.RuleGate.Manifest \
    Fotbiler.RuleGate.AspNetCore

  verify_restored_packages \
    "$mode" \
    "$version" \
    "$cache" \
    "$KEYCLOAK_CONSUMER_PROJECT" \
    "$KEYCLOAK_CONSUMER_DIRECTORY/obj/project.assets.json" \
    "$local_source" \
    Fotbiler.RuleGate.Abstractions \
    Fotbiler.RuleGate.Core \
    Fotbiler.RuleGate.Manifest \
    Fotbiler.RuleGate.AspNetCore \
    Fotbiler.RuleGate.Keycloak

  printf '\n========================================\n'
  printf 'Build %s consumer matrix\n' \
    "$mode"
  printf '========================================\n'

  dotnet build \
    "$MAIN_CONSUMER_PROJECT" \
    --configuration Release \
    --no-restore \
    -p:RuleGatePackageVersion="$version"

  dotnet build \
    "$KEYCLOAK_CONSUMER_PROJECT" \
    --configuration Release \
    --no-restore \
    -p:RuleGatePackageVersion="$version"

  printf '\n========================================\n'
  printf 'Run %s current-runtime consumers\n' \
    "$mode"
  printf '========================================\n'

  for framework in "${CURRENT_FRAMEWORKS[@]}"
  do
    dotnet \
      "$MAIN_CONSUMER_DIRECTORY/bin/Release/$framework/Fotbiler.RuleGate.PackageConsumer.Smoke.dll"

    dotnet \
      "$KEYCLOAK_CONSUMER_DIRECTORY/bin/Release/$framework/Fotbiler.RuleGate.Keycloak.PackageConsumer.Smoke.dll"

    printf '%s consumers passed on %s.\n' \
      "$mode" \
      "$framework"
  done

  printf '\n========================================\n'
  printf 'Run %s legacy-runtime consumers\n' \
    "$mode"
  printf '========================================\n'

  for entry in "${LEGACY_FRAMEWORKS[@]}"
  do
    framework="${entry%%:*}"
    runtime_version="${entry#*:}"

    image="mcr.microsoft.com/dotnet/aspnet:$runtime_version"

    for consumer in \
      Fotbiler.RuleGate.PackageConsumer.Smoke \
      Fotbiler.RuleGate.Keycloak.PackageConsumer.Smoke
    do
      assembly="/workspace/tests/$consumer/bin/Release/$framework/$consumer.dll"

      docker run \
        --rm \
        --read-only \
        --tmpfs /tmp \
        --network none \
        --volume "$REPOSITORY_ROOT:/workspace:ro" \
        "$image" \
        dotnet "$assembly"
    done

    printf '%s consumers passed on %s.\n' \
      "$mode" \
      "$framework"
  done

  echo
  echo "$mode package-consumer matrix passed for $version."
}

verify_current_cli_package()
{
  printf '\n========================================\n'
  printf 'Verify current RC CLI local package\n'
  printf '========================================\n'

  local cli_package="$CURRENT_PACKAGE_DIRECTORY/Fotbiler.RuleGate.Cli.$CURRENT_VERSION.nupkg"

  test -f "$cli_package"

  for framework in "${CURRENT_FRAMEWORKS[@]}"
  do
    local tool_directory="$TMP_DIRECTORY/current-cli-$framework"
    local tool_cache="$TMP_DIRECTORY/current-cli-cache-$framework"

    mkdir -p \
      "$tool_directory" \
      "$tool_cache"

    NUGET_PACKAGES="$tool_cache" \
    dotnet tool install \
      Fotbiler.RuleGate.Cli \
      --tool-path "$tool_directory" \
      --version "$CURRENT_VERSION" \
      --source "$CURRENT_PACKAGE_DIRECTORY" \
      --framework "$framework" \
      --no-cache

    local cli="$tool_directory/rulegate"

    test -x "$cli"

    "$cli" \
      --version |
      grep -F \
        "$CURRENT_VERSION" \
        >/dev/null

    "$cli" \
      info |
      grep -F \
        "Version: $CURRENT_VERSION" \
        >/dev/null

    printf 'Current RC CLI passed on %s.\n' \
      "$framework"
  done

  echo 'Current RC CLI package matrix passed.'
}

printf '\n========================================\n'
printf 'Negative version-override control\n'
printf '========================================\n'

NEGATIVE_CACHE="$TMP_DIRECTORY/negative-global-packages"

mkdir -p "$NEGATIVE_CACHE"

set +e

NUGET_PACKAGES="$NEGATIVE_CACHE" \
dotnet restore \
  "$MAIN_CONSUMER_PROJECT" \
  --force \
  --no-cache \
  -p:RuleGatePackageVersion="999.0.0-rulegate-negative-control" \
  --source "$CURRENT_PACKAGE_DIRECTORY" \
  --source "$NUGET_SOURCE" \
  >"$TMP_DIRECTORY/negative-restore.out" \
  2>"$TMP_DIRECTORY/negative-restore.err"

NEGATIVE_EXIT_CODE="$?"

set -e

if [[ "$NEGATIVE_EXIT_CODE" -eq 0 ]]
then
  echo 'ERROR: invalid package-version override unexpectedly restored.'
  exit 1
fi

echo 'Package-version override negative control passed.'

rm -rf \
  "$MAIN_CONSUMER_DIRECTORY/bin" \
  "$MAIN_CONSUMER_DIRECTORY/obj"

run_consumer_mode \
  current \
  "$CURRENT_VERSION" \
  "$CURRENT_PACKAGE_DIRECTORY"

verify_current_cli_package

run_consumer_mode \
  published \
  "$BASELINE_VERSION" \
  ""

echo
echo 'CURRENT_LOCAL_RC_PACKAGE_CONSUMER_MATRIX_PASSED'
echo 'PUBLISHED_BASELINE_PACKAGE_CONSUMER_MATRIX_PASSED'
echo 'DOTNET_PACKAGE_CONSUMER_MATRICES_PASSED'
