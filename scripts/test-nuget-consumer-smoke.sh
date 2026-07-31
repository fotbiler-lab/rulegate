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

SMOKE_DIRECTORY="$REPOSITORY_ROOT/tests/Fotbiler.RuleGate.PackageConsumer.Smoke"

SMOKE_PROJECT="$SMOKE_DIRECTORY/Fotbiler.RuleGate.PackageConsumer.Smoke.csproj"

PACKAGE_DIRECTORY="$REPOSITORY_ROOT/artifacts/packages"

CONSUMER_PACKAGE_CACHE="$REPOSITORY_ROOT/artifacts/package-consumer-global-packages"

PACKAGE_VERSION="0.9.0-preview.3"

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

printf '\n== Verify package-only consumer ==\n'

if grep -q '<ProjectReference' "$SMOKE_PROJECT"
then
  echo "ERROR: The smoke consumer contains a ProjectReference."
  exit 1
fi

echo "No ProjectReference found."

printf '\n== Clean consumer outputs ==\n'

rm -rf \
  "$SMOKE_DIRECTORY/bin" \
  "$SMOKE_DIRECTORY/obj" \
  "$CONSUMER_PACKAGE_CACHE"

if [[ "$PACKAGES_READY" == "false" ]]
then
  printf '\n== Clean previous packages ==\n'

  rm -rf "$PACKAGE_DIRECTORY"

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

  printf '\n== Pack production projects ==\n'

  dotnet pack \
    "$SOLUTION" \
    --configuration Release \
    --no-build
else
  printf '\n== Use packages produced by the current pipeline ==\n'
fi

printf '\n== Verify local packages ==\n'

for package_id in \
  Fotbiler.RuleGate.Abstractions \
  Fotbiler.RuleGate.Core \
  Fotbiler.RuleGate.Manifest \
  Fotbiler.RuleGate.AspNetCore
do
  package_path="$PACKAGE_DIRECTORY/$package_id.$PACKAGE_VERSION.nupkg"

  if test ! -f "$package_path"
  then
    echo "ERROR: Missing package: $package_path"
    exit 1
  fi

  printf 'Found: %s\n' \
    "$(basename "$package_path")"
done

printf '\n== Restore consumer from local packages ==\n'

export NUGET_PACKAGES="$CONSUMER_PACKAGE_CACHE"

printf 'Consumer package cache: %s\n' \
  "$NUGET_PACKAGES"

dotnet restore \
  "$SMOKE_PROJECT" \
  --force \
  --no-cache \
  --source "$PACKAGE_DIRECTORY" \
  --source "https://api.nuget.org/v3/index.json"

ASSETS_FILE="$SMOKE_DIRECTORY/obj/project.assets.json"

printf '\n== Verify restored target frameworks ==\n'

for framework in "${EXPECTED_FRAMEWORKS[@]}"
do
  if ! grep -F \
    "\"$framework\":" \
    "$ASSETS_FILE" \
    >/dev/null
  then
    echo "ERROR: Consumer assets do not contain $framework."
    exit 1
  fi

  echo "Restored: $framework"
done

printf '\n== Verify resolved package graph ==\n'

for dependency in \
  "Fotbiler.RuleGate.Abstractions/$PACKAGE_VERSION" \
  "Fotbiler.RuleGate.Core/$PACKAGE_VERSION" \
  "Fotbiler.RuleGate.Manifest/$PACKAGE_VERSION" \
  "Fotbiler.RuleGate.AspNetCore/$PACKAGE_VERSION" \
  "YamlDotNet/18.1.0"
do
  if ! grep -F "\"$dependency\"" \
    "$ASSETS_FILE" \
    >/dev/null
  then
    echo "ERROR: Dependency was not resolved: $dependency"
    exit 1
  fi

  echo "Resolved: $dependency"
done

printf '\n== Consumer package graph ==\n'

dotnet list \
  "$SMOKE_PROJECT" \
  package \
  --include-transitive

printf '\n== Run package consumer ==\n'

for framework in "${EXPECTED_FRAMEWORKS[@]}"
do
  printf '\nFRAMEWORK: %s\n' "$framework"

  dotnet run \
    --project "$SMOKE_PROJECT" \
    --configuration Release \
    --framework "$framework" \
    --no-restore
done

printf '\nNuGet package consumer smoke tests passed on all frameworks.\n'
