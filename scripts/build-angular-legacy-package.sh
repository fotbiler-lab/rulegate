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

LEGACY_BUILDER_DIRECTORY="$REPOSITORY_ROOT/compatibility/angular-legacy-builder"

cleanup()
{
  rm -f "$LEGACY_BUILDER_DIRECTORY/package.json"
}

trap cleanup EXIT

cd "$REPOSITORY_ROOT"

"$REPOSITORY_ROOT/scripts/build-client-package.sh"

node \
  "$REPOSITORY_ROOT/scripts/prepare-angular-legacy-build.mjs"

test -f "$LEGACY_BUILDER_DIRECTORY/package.json"

pnpm \
  --dir "$LEGACY_BUILDER_DIRECTORY" \
  install \
  --config.node-linker=hoisted \
  --frozen-lockfile=false

docker run \
  --rm \
  --user "$(id -u):$(id -g)" \
  --tmpfs /tmp \
  --volume "$REPOSITORY_ROOT:$REPOSITORY_ROOT" \
  --workdir "$LEGACY_BUILDER_DIRECTORY" \
  node:14.21.3 \
  ./node_modules/.bin/ng-packagr \
  --project .work/ng-package.json \
  --config .work/tsconfig.lib.json

cp \
  "$REPOSITORY_ROOT/LICENSE" \
  "$REPOSITORY_ROOT/dist/rulegate-angular-legacy/LICENSE"

echo "Built @fotbiler/rulegate-angular-legacy with its MIT license."
