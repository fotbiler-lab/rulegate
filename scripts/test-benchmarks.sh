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

OUTPUT_FILE="$(mktemp)"
trap 'rm -f -- "$OUTPUT_FILE"' EXIT

dotnet run \
  --project "$REPOSITORY_ROOT/benchmarks/Fotbiler.RuleGate.Benchmarks/Fotbiler.RuleGate.Benchmarks.csproj" \
  --configuration Release \
  --no-build \
  -- \
  --job dry \
  --filter '*' |
  tee "$OUTPUT_FILE"

if grep -q 'Build Error:' "$OUTPUT_FILE"; then
  printf 'BenchmarkDotNet reported a generated-project build error.\n' >&2
  exit 1
fi

if ! grep -Eq 'Global total time: .*executed benchmarks: 18$' "$OUTPUT_FILE"; then
  printf 'Benchmark dry run did not execute all 18 benchmarks.\n' >&2
  exit 1
fi
