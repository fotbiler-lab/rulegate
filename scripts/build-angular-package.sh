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

cd "$REPOSITORY_ROOT"

pnpm exec ng \
  build \
  rulegate-angular

cp \
  "$REPOSITORY_ROOT/LICENSE" \
  "$REPOSITORY_ROOT/dist/rulegate-angular/LICENSE"

echo "Copied Apache-2.0 license into the Angular package output."

node \
  "$REPOSITORY_ROOT/scripts/test-keycloak-normalization-vectors.mjs"
