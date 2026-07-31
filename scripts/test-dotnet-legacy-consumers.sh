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

if ! command -v docker >/dev/null 2>&1
then
  echo "ERROR: Docker is required for end-of-life .NET runtime verification."
  exit 1
fi

if [[ "$PACKAGES_READY" == "false" ]]
then
  "$REPOSITORY_ROOT/scripts/test-nuget-consumer-smoke.sh"
  "$REPOSITORY_ROOT/scripts/test-keycloak-nuget-consumer-smoke.sh" \
    --packages-ready
else
  "$REPOSITORY_ROOT/scripts/test-nuget-consumer-smoke.sh" \
    --packages-ready
  "$REPOSITORY_ROOT/scripts/test-keycloak-nuget-consumer-smoke.sh" \
    --packages-ready
fi

dotnet build \
  "$REPOSITORY_ROOT/tests/Fotbiler.RuleGate.PackageConsumer.Smoke/Fotbiler.RuleGate.PackageConsumer.Smoke.csproj" \
  --configuration Release \
  --no-restore

dotnet build \
  "$REPOSITORY_ROOT/tests/Fotbiler.RuleGate.Keycloak.PackageConsumer.Smoke/Fotbiler.RuleGate.Keycloak.PackageConsumer.Smoke.csproj" \
  --configuration Release \
  --no-restore

LEGACY_FRAMEWORKS=(
  "netcoreapp3.1:3.1"
  "net5.0:5.0"
  "net6.0:6.0"
  "net7.0:7.0"
)

for entry in "${LEGACY_FRAMEWORKS[@]}"
do
  framework="${entry%%:*}"
  runtime_version="${entry#*:}"
  image="mcr.microsoft.com/dotnet/aspnet:$runtime_version"

  printf '\n== Run package consumers on %s ==\n' "$framework"

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
done

printf '\nLegacy .NET package consumers passed on .NET Core 3.1 through .NET 7.\n'
