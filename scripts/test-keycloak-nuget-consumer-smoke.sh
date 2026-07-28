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
SMOKE_DIRECTORY="$REPOSITORY_ROOT/tests/Fotbiler.RuleGate.Keycloak.PackageConsumer.Smoke"
SMOKE_PROJECT="$SMOKE_DIRECTORY/Fotbiler.RuleGate.Keycloak.PackageConsumer.Smoke.csproj"
PACKAGE_DIRECTORY="$REPOSITORY_ROOT/artifacts/packages"
CONSUMER_PACKAGE_CACHE="$REPOSITORY_ROOT/artifacts/keycloak-package-consumer-global-packages"
KEYCLOAK_PACKAGE_VERSION="0.5.0-preview.1"
BASE_PACKAGE_VERSION="0.3.0-preview.2"

EXPECTED_FRAMEWORKS=(
  "net8.0"
  "net9.0"
  "net10.0"
)

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

if grep -q '<ProjectReference' "$SMOKE_PROJECT"
then
  echo "ERROR: The Keycloak smoke consumer contains a ProjectReference."
  exit 1
fi

rm -rf \
  "$SMOKE_DIRECTORY/bin" \
  "$SMOKE_DIRECTORY/obj" \
  "$CONSUMER_PACKAGE_CACHE"

if [[ "$PACKAGES_READY" == "false" ]]
then
  rm -rf "$PACKAGE_DIRECTORY"

  dotnet restore "$SOLUTION"
  dotnet build "$SOLUTION" --configuration Release --no-restore
  dotnet test "$SOLUTION" --configuration Release --no-build
  dotnet pack "$SOLUTION" --configuration Release --no-build
fi

for package in \
  "Fotbiler.RuleGate.Keycloak/$KEYCLOAK_PACKAGE_VERSION" \
  "Fotbiler.RuleGate.AspNetCore/$BASE_PACKAGE_VERSION" \
  "Fotbiler.RuleGate.Core/$BASE_PACKAGE_VERSION" \
  "Fotbiler.RuleGate.Abstractions/$BASE_PACKAGE_VERSION"
do
  package_id="${package%%/*}"
  package_version="${package#*/}"
  package_path="$PACKAGE_DIRECTORY/$package_id.$package_version.nupkg"

  if [[ ! -f "$package_path" ]]
  then
    echo "ERROR: Missing package: $package_path"
    exit 1
  fi

  printf 'Found: %s\n' "$(basename "$package_path")"
done

export NUGET_PACKAGES="$CONSUMER_PACKAGE_CACHE"

dotnet restore \
  "$SMOKE_PROJECT" \
  --force \
  --no-cache \
  --source "$PACKAGE_DIRECTORY" \
  --source "https://api.nuget.org/v3/index.json"

ASSETS_FILE="$SMOKE_DIRECTORY/obj/project.assets.json"

for dependency in \
  "Fotbiler.RuleGate.Keycloak/$KEYCLOAK_PACKAGE_VERSION" \
  "Fotbiler.RuleGate.AspNetCore/$BASE_PACKAGE_VERSION" \
  "Fotbiler.RuleGate.Core/$BASE_PACKAGE_VERSION" \
  "Fotbiler.RuleGate.Abstractions/$BASE_PACKAGE_VERSION"
do
  if ! grep -F "\"$dependency\"" "$ASSETS_FILE" >/dev/null
  then
    echo "ERROR: Dependency was not resolved: $dependency"
    exit 1
  fi

  echo "Resolved: $dependency"
done

for framework in "${EXPECTED_FRAMEWORKS[@]}"
do
  dotnet run \
    --project "$SMOKE_PROJECT" \
    --configuration Release \
    --framework "$framework" \
    --no-restore
done

printf '\nKeycloak NuGet package consumer smoke tests passed on all frameworks.\n'
