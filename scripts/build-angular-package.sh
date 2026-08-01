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

"$REPOSITORY_ROOT/scripts/build-client-package.sh"

node \
  "$REPOSITORY_ROOT/scripts/prepare-angular-modern-build.mjs"

pnpm \
  --dir compatibility/angular-modern-builder \
  exec \
  ng-packagr \
  --project .work/ng-package.json

cp \
  "$REPOSITORY_ROOT/LICENSE" \
  "$REPOSITORY_ROOT/dist/rulegate-angular/LICENSE"

echo "Copied MIT license into the Angular package output."

node \
  "$REPOSITORY_ROOT/scripts/test-keycloak-normalization-vectors.mjs"
