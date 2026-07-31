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

OUTPUT_DIRECTORY="$REPOSITORY_ROOT/src/Fotbiler.RuleGate.Client/dist"

rm -rf "$OUTPUT_DIRECTORY"

cd "$REPOSITORY_ROOT"

pnpm exec tsc \
  --project src/Fotbiler.RuleGate.Client/tsconfig.json

echo "Built @fotbiler/rulegate-client."
