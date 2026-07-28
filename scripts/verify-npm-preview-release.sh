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

ALLOW_DIRTY="false"
CHECK_REGISTRY_AVAILABILITY="false"

for argument in "$@"
do
  case "$argument" in
    --allow-dirty)
      ALLOW_DIRTY="true"
      ;;

    --check-registry-availability)
      CHECK_REGISTRY_AVAILABILITY="true"
      ;;

    *)
      echo "Usage: $0 [--allow-dirty] [--check-registry-availability]"
      exit 2
      ;;
  esac
done

EXPECTED_PACKAGE_NAME="@fotbiler/rulegate-angular"
EXPECTED_PACKAGE_VERSION="0.4.0-preview.2"
EXPECTED_PACKAGE_FILE="fotbiler-rulegate-angular-$EXPECTED_PACKAGE_VERSION.tgz"
EXPECTED_REPOSITORY="git+https://github.com/fotbiler-lab/rulegate.git"
EXPECTED_REPOSITORY_DIRECTORY="src/Fotbiler.RuleGate.Angular"
EXPECTED_LICENSE="Apache-2.0"
EXPECTED_AUTHOR="Fotbiler"

PACKAGE_DIRECTORY="$REPOSITORY_ROOT/artifacts/npm"
PACKAGE_PATH="$PACKAGE_DIRECTORY/$EXPECTED_PACKAGE_FILE"

printf '\n== Verify repository state ==\n'

if [[ "$ALLOW_DIRTY" == "false" ]] &&
   [[ -n "$(git status --porcelain)" ]]
then
  echo "ERROR: Working tree is not clean."
  git status --short
  exit 1
fi

if [[ "$ALLOW_DIRTY" == "true" ]]
then
  echo "Dirty working tree is allowed for this verification run."
else
  echo "Working tree is clean."
fi

HEAD_COMMIT="$(git rev-parse HEAD)"
printf 'Commit: %s\n' "$HEAD_COMMIT"

printf '\n== Verify package source metadata ==\n'

node \
  --input-type=module \
  - \
  "src/Fotbiler.RuleGate.Angular/package.json" \
  "$EXPECTED_PACKAGE_NAME" \
  "$EXPECTED_PACKAGE_VERSION" \
  "$EXPECTED_REPOSITORY" \
  "$EXPECTED_REPOSITORY_DIRECTORY" \
  "$EXPECTED_LICENSE" \
  "$EXPECTED_AUTHOR" <<'JS'
import { readFile } from 'node:fs/promises';

const [
  ,
  ,
  manifestPath,
  expectedName,
  expectedVersion,
  expectedRepository,
  expectedDirectory,
  expectedLicense,
  expectedAuthor,
] = process.argv;

const manifest = JSON.parse(await readFile(manifestPath, 'utf8'));

const expectations = [
  ['name', manifest.name, expectedName],
  ['version', manifest.version, expectedVersion],
  ['repository URL', manifest.repository?.url, expectedRepository],
  ['repository directory', manifest.repository?.directory, expectedDirectory],
  ['license', manifest.license, expectedLicense],
  ['author', manifest.author, expectedAuthor],
  ['public access', manifest.publishConfig?.access, 'public'],
  ['provenance', manifest.publishConfig?.provenance, true],
  ['side effects', manifest.sideEffects, false],
];

for (const [description, actual, expected] of expectations) {
  if (actual !== expected) {
    throw new Error(`Unexpected ${description}: ${String(actual)}`);
  }
}
JS

printf 'Package: %s@%s\n' \
  "$EXPECTED_PACKAGE_NAME" \
  "$EXPECTED_PACKAGE_VERSION"

if [[ "$CHECK_REGISTRY_AVAILABILITY" == "true" ]]
then
  printf '\n== Verify registry version availability ==\n'

  REGISTRY_OUTPUT="$REPOSITORY_ROOT/artifacts/npm-registry-check.log"
  mkdir -p "$REPOSITORY_ROOT/artifacts"

  set +e

  npm view \
    "$EXPECTED_PACKAGE_NAME@$EXPECTED_PACKAGE_VERSION" \
    version \
    >"$REGISTRY_OUTPUT" \
    2>&1

  REGISTRY_EXIT_CODE="$?"

  set -e

  if [[ "$REGISTRY_EXIT_CODE" -eq 0 ]]
  then
    echo "ERROR: Package version already exists on npm."
    cat "$REGISTRY_OUTPUT"
    exit 1
  fi

  if ! grep -F 'E404' "$REGISTRY_OUTPUT" >/dev/null
  then
    echo "ERROR: Registry availability could not be determined."
    cat "$REGISTRY_OUTPUT"
    exit 1
  fi

  echo "Registry version is available."
fi

printf '\n== Install locked dependencies ==\n'

pnpm install --frozen-lockfile

printf '\n== Verify Angular formatting ==\n'

pnpm angular:format:check

printf '\n== Build Angular package ==\n'

pnpm angular:build

printf '\n== Test Angular package ==\n'

pnpm angular:test

printf '\n== Verify package-only consumer ==\n'

./scripts/test-angular-package-smoke.sh \
  --package-ready

if [[ ! -f "$PACKAGE_PATH" ]]
then
  echo "ERROR: Expected npm package was not produced: $PACKAGE_PATH"
  exit 1
fi

printf '\n== Verify packed public API ==\n'

PACKAGE_FILES="$(tar --list --gzip --file "$PACKAGE_PATH")"

for expected_file in \
  package/package.json \
  package/README.md \
  package/LICENSE \
  package/bin/rulegate-angular.mjs \
  package/fesm2022/fotbiler-rulegate-angular.mjs \
  package/types/fotbiler-rulegate-angular.d.ts
do
  if ! grep -Fx "$expected_file" \
    <<<"$PACKAGE_FILES" \
    >/dev/null
  then
    echo "ERROR: npm package does not contain $expected_file."
    exit 1
  fi
done

TYPE_DECLARATIONS="$(
  tar \
    --extract \
    --to-stdout \
    --gzip \
    --file "$PACKAGE_PATH" \
    package/types/fotbiler-rulegate-angular.d.ts
)"

for public_api in \
  RuleGateAuthorizationClient \
  RuleGateAuthorizationSnapshot \
  RuleGateAuthorizationRequirement \
  RuleGateCanDirective \
  RuleGateDisableDirective \
  ruleGateGuard \
  ruleGateRouteData \
  provideRuleGateDeniedNavigation \
  ruleGatePermissionGuard \
  ruleGatePolicyGuard
do
  if ! grep -F "$public_api" \
    <<<"$TYPE_DECLARATIONS" \
    >/dev/null
  then
    echo "ERROR: Packed declarations do not contain $public_api."
    exit 1
  fi
done

printf '\n== Verify normal CI does not publish ==\n'

if grep -RInE \
  'npm (publish|stage publish)|NODE_AUTH_TOKEN|NPM_TOKEN|id-token: write' \
  .github/workflows/ci.yml
then
  echo "ERROR: npm publishing configuration exists in normal CI."
  exit 1
fi

echo "Normal CI contains no npm publishing configuration."

printf '\n== npm preview verification succeeded ==\n'

printf 'Package: %s\n' "$(basename "$PACKAGE_PATH")"
printf 'SHA-256: %s\n' "$(sha256sum "$PACKAGE_PATH" | awk '{ print $1 }')"
printf 'Commit:  %s\n' "$HEAD_COMMIT"
