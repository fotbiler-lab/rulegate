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

dotnet run \
  --project "$REPOSITORY_ROOT/benchmarks/Fotbiler.RuleGate.Benchmarks/Fotbiler.RuleGate.Benchmarks.csproj" \
  --configuration Release \
  --no-build \
  -- \
  --job dry \
  --filter '*'
