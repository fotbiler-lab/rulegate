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

EXPECTED_PACKAGE_VERSION="1.0.0-rc.1"
EXPECTED_REPOSITORY="git+https://github.com/fotbiler-lab/rulegate.git"
EXPECTED_LICENSE="MIT"
EXPECTED_AUTHOR="Fotbiler"

PACKAGE_DIRECTORY="$REPOSITORY_ROOT/artifacts/npm"

PACKAGE_NAMES=(
  "@fotbiler/rulegate-client"
  "@fotbiler/rulegate-angular-legacy"
  "@fotbiler/rulegate-angular"
)

declare -A PACKAGE_SOURCE_DIRECTORIES=(
  ["@fotbiler/rulegate-client"]="src/Fotbiler.RuleGate.Client"
  ["@fotbiler/rulegate-angular-legacy"]="src/Fotbiler.RuleGate.Angular.Legacy"
  ["@fotbiler/rulegate-angular"]="src/Fotbiler.RuleGate.Angular"
)

declare -A PACKAGE_FILES=(
  ["@fotbiler/rulegate-client"]="fotbiler-rulegate-client-$EXPECTED_PACKAGE_VERSION.tgz"
  ["@fotbiler/rulegate-angular-legacy"]="fotbiler-rulegate-angular-legacy-$EXPECTED_PACKAGE_VERSION.tgz"
  ["@fotbiler/rulegate-angular"]="fotbiler-rulegate-angular-$EXPECTED_PACKAGE_VERSION.tgz"
)

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

for package_name in "${PACKAGE_NAMES[@]}"
do
  source_directory="${PACKAGE_SOURCE_DIRECTORIES[$package_name]}"

  node \
    --input-type=module \
    - \
    "$source_directory/package.json" \
    "$package_name" \
    "$EXPECTED_PACKAGE_VERSION" \
    "$EXPECTED_REPOSITORY" \
    "$source_directory" \
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
    "$package_name" \
    "$EXPECTED_PACKAGE_VERSION"
done

if [[ "$CHECK_REGISTRY_AVAILABILITY" == "true" ]]
then
  printf '\n== Verify registry version availability ==\n'

  REGISTRY_OUTPUT="$REPOSITORY_ROOT/artifacts/npm-registry-check.log"
  mkdir -p "$REPOSITORY_ROOT/artifacts"

  set +e

  for package_name in "${PACKAGE_NAMES[@]}"
  do
    : >"$REGISTRY_OUTPUT"

    npm view \
      "$package_name@$EXPECTED_PACKAGE_VERSION" \
      version \
      >"$REGISTRY_OUTPUT" \
      2>&1

    REGISTRY_EXIT_CODE="$?"

    if [[ "$REGISTRY_EXIT_CODE" -eq 0 ]]
    then
      echo "ERROR: Package version already exists on npm: $package_name"
      cat "$REGISTRY_OUTPUT"
      exit 1
    fi

    if ! grep -F 'E404' "$REGISTRY_OUTPUT" >/dev/null
    then
      echo "ERROR: Registry availability could not be determined for $package_name."
      cat "$REGISTRY_OUTPUT"
      exit 1
    fi

    echo "Registry version is available: $package_name"
  done

  set -e
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

for package_name in "${PACKAGE_NAMES[@]}"
do
  package_path="$PACKAGE_DIRECTORY/${PACKAGE_FILES[$package_name]}"

  if [[ ! -f "$package_path" ]]
  then
    echo "ERROR: Expected npm package was not produced: $package_path"
    exit 1
  fi
done

printf '\n== Verify packed public API ==\n'

for package_name in "${PACKAGE_NAMES[@]}"
do
  package_path="$PACKAGE_DIRECTORY/${PACKAGE_FILES[$package_name]}"
  packed_files="$(tar --list --gzip --file "$package_path")"

  for expected_file in package/package.json package/README.md package/LICENSE
  do
    if ! grep -Fx "$expected_file" <<<"$packed_files" >/dev/null
    then
      echo "ERROR: $package_name does not contain $expected_file."
      exit 1
    fi
  done

  case "$package_name" in
    @fotbiler/rulegate-client)
      expected_files=(
        package/dist/index.js
        package/dist/index.d.ts
        package/dist/models.d.ts
        package/dist/rule-gate-authorization-store.d.ts
      )
      public_apis=(
        RuleGateAuthorizationStore
        RuleGateAuthorizationSnapshot
        RuleGateAuthorizationRequirement
      )
      ;;

    @fotbiler/rulegate-angular-legacy)
      expected_files=(
        package/fesm2015/fotbiler-rulegate-angular-legacy.js
        package/fotbiler-rulegate-angular-legacy.d.ts
        package/public-api.d.ts
      )
      public_apis=(
        RuleGateLegacyAuthorizationClient
        RuleGateLegacyCanDirective
        RuleGateLegacyDisableDirective
        RuleGateLegacyGuard
        RuleGateLegacyModule
        ruleGateLegacyRouteData
      )
      ;;

    @fotbiler/rulegate-angular)
      expected_files=(
        package/bin/rulegate-angular.mjs
        package/fesm2022/fotbiler-rulegate-angular.mjs
        package/fesm2022/fotbiler-rulegate-angular-keycloak.mjs
        package/index.d.ts
        package/keycloak/index.d.ts
      )
      public_apis=(
        RuleGateAuthorizationClient
        RuleGateCanDirective
        RuleGateDisableDirective
        ruleGateGuard
        ruleGateRouteData
        provideRuleGateDeniedNavigation
        RuleGateKeycloakAdapter
        createRuleGateSnapshotFromKeycloak
      )
      ;;
  esac

  for expected_file in "${expected_files[@]}"
  do
    if ! grep -Fx "$expected_file" <<<"$packed_files" >/dev/null
    then
      echo "ERROR: $package_name does not contain $expected_file."
      exit 1
    fi
  done

  declarations_directory="$(mktemp --directory /tmp/rulegate-npm-declarations-XXXXXX)"
  tar --extract --gzip --file "$package_path" --directory "$declarations_directory"

  for public_api in "${public_apis[@]}"
  do
    if ! grep -R --include='*.d.ts' -F "$public_api" "$declarations_directory/package" >/dev/null
    then
      echo "ERROR: $package_name declarations do not contain $public_api."
      rm -rf "$declarations_directory"
      exit 1
    fi
  done

  rm -rf "$declarations_directory"
  echo "Verified: $package_name"
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

printf '\n== npm prerelease verification succeeded ==\n'

for package_name in "${PACKAGE_NAMES[@]}"
do
  package_path="$PACKAGE_DIRECTORY/${PACKAGE_FILES[$package_name]}"
  printf 'Package: %s\n' "$(basename "$package_path")"
  printf 'SHA-256: %s\n' "$(sha256sum "$package_path" | awk '{ print $1 }')"
done
printf 'Commit:  %s\n' "$HEAD_COMMIT"
