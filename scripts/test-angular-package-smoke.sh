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

PACKAGE_NAME="@fotbiler/rulegate-angular"
CLIENT_PACKAGE_NAME="@fotbiler/rulegate-client"
LEGACY_PACKAGE_NAME="@fotbiler/rulegate-angular-legacy"
PACKAGE_VERSION="0.9.0-preview.4"
PACKAGE_BUILD_DIRECTORY="$REPOSITORY_ROOT/dist/rulegate-angular"
CLIENT_PACKAGE_DIRECTORY="$REPOSITORY_ROOT/src/Fotbiler.RuleGate.Client"
LEGACY_PACKAGE_BUILD_DIRECTORY="$REPOSITORY_ROOT/dist/rulegate-angular-legacy"
PACKAGE_ARTIFACT_DIRECTORY="$REPOSITORY_ROOT/artifacts/npm"

PACKAGE_READY="false"

case "${1:-}" in
  "")
    ;;

  --package-ready)
    PACKAGE_READY="true"
    ;;

  *)
    echo "Usage: $0 [--package-ready]"
    exit 2
    ;;
esac

TEMP_DIRECTORY="$(
  mktemp \
    --directory \
    /tmp/rulegate-angular-package-smoke-XXXXXX
)"

cleanup()
{
  rm -rf "$TEMP_DIRECTORY"
}

trap cleanup EXIT

cd "$REPOSITORY_ROOT"

if [[ "$PACKAGE_READY" == "false" ]]
then
  printf '\n== Build Angular package ==\n'

  pnpm angular:build
fi

printf '\n== Verify Angular package build ==\n'

test -f "$PACKAGE_BUILD_DIRECTORY/package.json"
test -f "$PACKAGE_BUILD_DIRECTORY/README.md"
test -f "$PACKAGE_BUILD_DIRECTORY/LICENSE"
test -f "$PACKAGE_BUILD_DIRECTORY/fesm2022/fotbiler-rulegate-angular.mjs"
test -f "$PACKAGE_BUILD_DIRECTORY/fesm2022/fotbiler-rulegate-angular-keycloak.mjs"
test -f "$PACKAGE_BUILD_DIRECTORY/index.d.ts"
test -f "$PACKAGE_BUILD_DIRECTORY/keycloak/index.d.ts"
test -f "$CLIENT_PACKAGE_DIRECTORY/dist/index.js"
test -f "$CLIENT_PACKAGE_DIRECTORY/dist/index.d.ts"
test -f "$LEGACY_PACKAGE_BUILD_DIRECTORY/fesm2015/fotbiler-rulegate-angular-legacy.js"
test -f "$LEGACY_PACKAGE_BUILD_DIRECTORY/fotbiler-rulegate-angular-legacy.d.ts"

rm -rf "$PACKAGE_ARTIFACT_DIRECTORY"
mkdir -p "$PACKAGE_ARTIFACT_DIRECTORY"

printf '\n== Pack Angular package ==\n'

pnpm \
  --dir "$PACKAGE_BUILD_DIRECTORY" \
  pack \
  --pack-destination "$PACKAGE_ARTIFACT_DIRECTORY"

pnpm \
  --dir "$CLIENT_PACKAGE_DIRECTORY" \
  pack \
  --pack-destination "$PACKAGE_ARTIFACT_DIRECTORY"

pnpm \
  --dir "$LEGACY_PACKAGE_BUILD_DIRECTORY" \
  pack \
  --pack-destination "$PACKAGE_ARTIFACT_DIRECTORY"

mapfile -t PACKAGE_PATHS < <(
  find "$PACKAGE_ARTIFACT_DIRECTORY" \
    -maxdepth 1 \
    -type f \
    -name '*.tgz' \
    -print
)

if [[ "${#PACKAGE_PATHS[@]}" -ne 3 ]]
then
  echo "ERROR: Expected exactly three npm package tarballs."
  exit 1
fi

PACKAGE_PATH="$PACKAGE_ARTIFACT_DIRECTORY/fotbiler-rulegate-angular-$PACKAGE_VERSION.tgz"
CLIENT_PACKAGE_PATH="$PACKAGE_ARTIFACT_DIRECTORY/fotbiler-rulegate-client-$PACKAGE_VERSION.tgz"
LEGACY_PACKAGE_PATH="$PACKAGE_ARTIFACT_DIRECTORY/fotbiler-rulegate-angular-legacy-$PACKAGE_VERSION.tgz"

test -f "$PACKAGE_PATH"
test -f "$CLIENT_PACKAGE_PATH"
test -f "$LEGACY_PACKAGE_PATH"

PACKAGE_FILES="$(
  tar \
    --list \
    --gzip \
    --file "$PACKAGE_PATH"
)"

for expected_file in \
  package/package.json \
  package/README.md \
  package/LICENSE \
  package/bin/rulegate-angular.mjs \
  package/fesm2022/fotbiler-rulegate-angular.mjs \
  package/fesm2022/fotbiler-rulegate-angular-keycloak.mjs \
  package/index.d.ts \
  package/keycloak/index.d.ts
do
  if ! grep -Fx "$expected_file" \
    <<<"$PACKAGE_FILES" \
    >/dev/null
  then
    echo "ERROR: npm package does not contain $expected_file."
    exit 1
  fi
done

tar \
  --extract \
  --gzip \
  --file "$PACKAGE_PATH" \
  --directory "$TEMP_DIRECTORY" \
  package/package.json

node \
  --input-type=module \
  - \
  "$TEMP_DIRECTORY/package/package.json" \
  "$PACKAGE_NAME" \
  "$PACKAGE_VERSION" <<'JS'
import { readFile } from 'node:fs/promises';

const [, , manifestPath, expectedName, expectedVersion] = process.argv;
const manifest = JSON.parse(await readFile(manifestPath, 'utf8'));

if (manifest.name !== expectedName) {
  throw new Error(`Unexpected package name: ${manifest.name}`);
}

if (manifest.version !== expectedVersion) {
  throw new Error(`Unexpected package version: ${manifest.version}`);
}

if (manifest.license !== 'MIT') {
  throw new Error(`Unexpected package license: ${manifest.license}`);
}

if (manifest.peerDependencies?.['@angular/core'] !== '^20.0.0 || ^21.0.0 || ^22.0.0') {
  throw new Error('Unexpected @angular/core peer dependency.');
}

if (manifest.peerDependencies?.['@angular/router'] !== '^20.0.0 || ^21.0.0 || ^22.0.0') {
  throw new Error('Unexpected @angular/router peer dependency.');
}

if (manifest.peerDependencies?.['@fotbiler/rulegate-client'] !== expectedVersion) {
  throw new Error('Unexpected RuleGate client peer dependency.');
}

if (manifest.publishConfig?.access !== 'public') {
  throw new Error('The scoped npm package is not configured for public access.');
}

if (manifest.bin?.['rulegate-angular'] !== 'bin/rulegate-angular.mjs') {
  throw new Error('The RuleGate Angular generator binary is missing.');
}

if (manifest.dependencies?.yaml !== '2.9.0') {
  throw new Error('The generator YAML dependency is missing.');
}

if (!manifest.exports?.['./keycloak']) {
  throw new Error('The optional Keycloak secondary entry point is missing.');
}

if (manifest.dependencies?.['keycloak-js'] || manifest.peerDependencies?.['keycloak-js']) {
  throw new Error('The RuleGate package must not require keycloak-js.');
}
JS

printf 'Package: %s\n' "$(basename "$PACKAGE_PATH")"
printf 'Package: %s\n' "$(basename "$CLIENT_PACKAGE_PATH")"
printf 'Package: %s\n' "$(basename "$LEGACY_PACKAGE_PATH")"

CONSUMER_DIRECTORY="$TEMP_DIRECTORY/consumer"
mkdir -p "$CONSUMER_DIRECTORY/src"

cat >"$CONSUMER_DIRECTORY/package.json" <<EOF_PACKAGE
{
  "name": "rulegate-angular-package-consumer",
  "version": "0.0.0",
  "private": true,
  "type": "module",
  "scripts": {
    "build": "ng build"
  },
  "dependencies": {
    "@angular/common": "22.0.8",
    "@angular/compiler": "22.0.8",
    "@angular/core": "22.0.8",
    "@angular/platform-browser": "22.0.8",
    "@angular/router": "22.0.8",
    "@fotbiler/rulegate-angular": "file:$PACKAGE_PATH",
    "@fotbiler/rulegate-client": "file:$CLIENT_PACKAGE_PATH",
    "keycloak-js": "26.2.4",
    "rxjs": "7.8.2",
    "tslib": "2.8.1"
  },
  "devDependencies": {
    "@angular/build": "22.0.8",
    "@angular/cli": "22.0.8",
    "@angular/compiler-cli": "22.0.8",
    "typescript": "6.0.2"
  }
}
EOF_PACKAGE

cat >"$CONSUMER_DIRECTORY/angular.json" <<'EOF_ANGULAR'
{
  "$schema": "./node_modules/@angular/cli/lib/config/schema.json",
  "version": 1,
  "cli": {
    "packageManager": "pnpm"
  },
  "projects": {
    "consumer": {
      "projectType": "application",
      "root": "",
      "sourceRoot": "src",
      "prefix": "app",
      "architect": {
        "build": {
          "builder": "@angular/build:application",
          "options": {
            "browser": "src/main.ts",
            "index": "src/index.html",
            "outputPath": "dist",
            "tsConfig": "tsconfig.app.json"
          }
        }
      }
    }
  }
}
EOF_ANGULAR

cat >"$CONSUMER_DIRECTORY/tsconfig.json" <<'EOF_TSCONFIG'
{
  "compilerOptions": {
    "experimentalDecorators": true,
    "isolatedModules": true,
    "module": "preserve",
    "noFallthroughCasesInSwitch": true,
    "noImplicitOverride": true,
    "noImplicitReturns": true,
    "noPropertyAccessFromIndexSignature": true,
    "skipLibCheck": true,
    "strict": true,
    "target": "ES2022"
  },
  "angularCompilerOptions": {
    "strictInjectionParameters": true,
    "strictInputAccessModifiers": true,
    "strictTemplates": true
  },
  "files": []
}
EOF_TSCONFIG

cat >"$CONSUMER_DIRECTORY/tsconfig.app.json" <<'EOF_APP_TSCONFIG'
{
  "extends": "./tsconfig.json",
  "compilerOptions": {
    "outDir": "./out-tsc/app",
    "types": []
  },
  "files": ["src/main.ts"]
}
EOF_APP_TSCONFIG

cat >"$CONSUMER_DIRECTORY/src/index.html" <<'EOF_HTML'
<!doctype html>
<html lang="en">
  <head>
    <meta charset="utf-8">
    <title>RuleGate Angular package consumer</title>
    <base href="/">
  </head>
  <body>
    <app-root></app-root>
  </body>
</html>
EOF_HTML

cat >"$CONSUMER_DIRECTORY/rulegate.yaml" <<'EOF_MANIFEST'
schemaVersion: 1

application:
  id: angular-package-consumer
  name: Angular Package Consumer

policies:
  - id: documents-read
    resourceType: document
    action: read
    requirement:
      permission: documents.read
  - id: documents-by-role
    resourceType: document
    action: list
    requirement:
      role: keycloak:realm:documents.reader
  - id: secure-context-access
    resourceType: portal
    action: access
    requirement:
      all:
        - attributeComparison:
            left: { source: resource, name: ownerId }
            operator: equal
            right: { source: subject, name: id }
        - timeWindow:
            days: [monday]
            start: "08:00"
            end: "18:00"
            timeZone: Europe/Istanbul
        - dateTimeWindow:
            endsAt: "2026-08-01T00:00:00Z"
        - contextAge:
            timestamp: mfa
            maximumAge: "00:15:00"
        - context:
            property: trustedDevice
            operator: equal
            valueType: boolean
            value: true
EOF_MANIFEST

cat >"$CONSUMER_DIRECTORY/src/main.ts" <<'EOF_TYPESCRIPT'
import { Component, inject } from '@angular/core';
import { bootstrapApplication } from '@angular/platform-browser';
import { provideRouter, RedirectCommand, Router, Routes } from '@angular/router';
import {
  RuleGateAuthorizationClient,
  RuleGateCanDirective,
  RuleGateDisableDirective,
  provideRuleGateDeniedNavigation,
  ruleGateGuard,
  ruleGateRouteData,
} from '@fotbiler/rulegate-angular';
import { RuleGateKeycloakAdapter } from '@fotbiler/rulegate-angular/keycloak';
import Keycloak from 'keycloak-js';

import { RuleGateIdentifiers } from './generated/rulegate';

@Component({
  selector: 'app-root',
  imports: [RuleGateCanDirective, RuleGateDisableDirective],
  template: `
    <span *ruleGateCan="{ permission: permissions.documentsRead }; else denied">
      package consumer
    </span>
    <button [ruleGateDisable]="{ permission: permissions.documentsRead }">Open</button>
    <ng-template #denied>denied</ng-template>
  `,
})
class PackageConsumerComponent {
  private readonly authorization = inject(RuleGateAuthorizationClient);

  readonly permissions = RuleGateIdentifiers.permissions;

  constructor() {
    this.authorization.replaceSnapshot({
      permissions: [RuleGateIdentifiers.permissions.documentsRead],
      policies: [RuleGateIdentifiers.policies.documentsRead],
    });
  }
}

function synchronizeKeycloak(
  adapter: RuleGateKeycloakAdapter,
  keycloak: Keycloak,
): boolean {
  return adapter.synchronize(keycloak, {
    clientIds: ['rulegate-angular-consumer'],
  });
}

void synchronizeKeycloak;

const routes: Routes = [
  {
    path: 'permission',
    canActivate: [ruleGateGuard],
    data: ruleGateRouteData({
      permission: RuleGateIdentifiers.permissions.documentsRead,
    }),
    component: PackageConsumerComponent,
  },
  {
    path: 'policy',
    canActivate: [ruleGateGuard],
    data: ruleGateRouteData({ policy: RuleGateIdentifiers.policies.documentsRead }),
    component: PackageConsumerComponent,
  },
];

bootstrapApplication(PackageConsumerComponent, {
  providers: [
    provideRouter(routes),
    provideRuleGateDeniedNavigation(({ state }) =>
      new RedirectCommand(
        inject(Router).createUrlTree(['/denied'], {
          queryParams: { returnUrl: state.url },
        }),
      ),
    ),
  ],
}).catch((error: unknown) => {
  console.error(error);
});
EOF_TYPESCRIPT

printf '\n== Install package-only Angular consumer ==\n'

pnpm \
  --dir "$CONSUMER_DIRECTORY" \
  install \
  --prefer-offline \
  --ignore-scripts \
  --ignore-workspace \
  --frozen-lockfile=false

printf '\n== Generate package-only Angular identifiers ==\n'

pnpm \
  --dir "$CONSUMER_DIRECTORY" \
  exec \
  rulegate-angular \
  generate \
  rulegate.yaml \
  --output \
  src/generated/rulegate.ts

pnpm \
  --dir "$CONSUMER_DIRECTORY" \
  exec \
  rulegate-angular \
  generate \
  rulegate.yaml \
  --output \
  src/generated/rulegate.ts \
  --check

printf '\n== Build package-only Angular consumer ==\n'

pnpm \
  --dir "$CONSUMER_DIRECTORY" \
  build

test -f "$CONSUMER_DIRECTORY/dist/browser/index.html"

printf '\nAngular npm package smoke test passed.\n'
