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
  "$REPOSITORY_ROOT/scripts/prepare-angular-legacy-build.mjs"

pnpm \
  --dir compatibility/angular-legacy-builder \
  install \
  --config.node-linker=hoisted \
  --frozen-lockfile=false

docker run \
  --rm \
  --user "$(id -u):$(id -g)" \
  --tmpfs /tmp \
  --volume "$REPOSITORY_ROOT:$REPOSITORY_ROOT" \
  --workdir "$REPOSITORY_ROOT/compatibility/angular-legacy-builder" \
  node:14.21.3 \
  ./node_modules/.bin/ng-packagr \
  --project .work/ng-package.json \
  --config .work/tsconfig.lib.json

cp \
  "$REPOSITORY_ROOT/LICENSE" \
  "$REPOSITORY_ROOT/dist/rulegate-angular-legacy/LICENSE"

echo "Built @fotbiler/rulegate-angular-legacy with its Apache-2.0 license."
