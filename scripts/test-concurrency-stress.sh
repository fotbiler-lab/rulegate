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

DURATION_SECONDS="${1:-60}"

if ! [[ "$DURATION_SECONDS" =~ ^[0-9]+$ ]] ||
   (( DURATION_SECONDS < 1 || DURATION_SECONDS > 3600 ))
then
  echo "Usage: $0 [duration-seconds: 1-3600]" >&2
  exit 2
fi

dotnet run \
  --project "$REPOSITORY_ROOT/tests/Fotbiler.RuleGate.Concurrency.Stress/Fotbiler.RuleGate.Concurrency.Stress.csproj" \
  --configuration Release \
  --no-build \
  -- \
  --duration-seconds "$DURATION_SECONDS"
