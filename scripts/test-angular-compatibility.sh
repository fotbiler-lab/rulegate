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

PACKAGE_VERSION="0.9.0-preview.4"
PACKAGE_DIRECTORY="$REPOSITORY_ROOT/artifacts/npm"
CLIENT_PACKAGE_PATH="$PACKAGE_DIRECTORY/fotbiler-rulegate-client-$PACKAGE_VERSION.tgz"
MODERN_PACKAGE_PATH="$PACKAGE_DIRECTORY/fotbiler-rulegate-angular-$PACKAGE_VERSION.tgz"
LEGACY_PACKAGE_PATH="$PACKAGE_DIRECTORY/fotbiler-rulegate-angular-legacy-$PACKAGE_VERSION.tgz"

ANGULAR_MAJOR="${1:-}"
TSLIB_VERSION="2.8.1"
TYPESCRIPT_MODULE="es2020"
TYPESCRIPT_MODULE_RESOLUTION="node"

case "$ANGULAR_MAJOR" in
  9)
    ANGULAR_VERSION="9.1.13"
    CLI_VERSION="9.1.15"
    BUILD_VERSION="0.901.15"
    TYPESCRIPT_VERSION="3.8.3"
    RXJS_VERSION="6.5.5"
    ZONE_VERSION="0.10.3"
    NODE_IMAGE="node:12.22.12"
    ADAPTER="client"
    TSLIB_VERSION="1.14.1"
    ;;

  11)
    ANGULAR_VERSION="11.2.14"
    CLI_VERSION="11.2.19"
    BUILD_VERSION="0.1102.19"
    TYPESCRIPT_VERSION="4.1.6"
    RXJS_VERSION="6.6.7"
    ZONE_VERSION="0.11.4"
    NODE_IMAGE="node:14.21.3"
    ADAPTER="client"
    TSLIB_VERSION="1.14.1"
    ;;

  12)
    ANGULAR_VERSION="12.2.17"
    CLI_VERSION="12.2.18"
    BUILD_VERSION="12.2.18"
    TYPESCRIPT_VERSION="4.3.5"
    RXJS_VERSION="6.6.7"
    ZONE_VERSION="0.11.8"
    NODE_IMAGE="node:14.21.3"
    ADAPTER="legacy"
    ;;

  15)
    ANGULAR_VERSION="15.2.10"
    CLI_VERSION="15.2.11"
    BUILD_VERSION="15.2.11"
    TYPESCRIPT_VERSION="4.9.5"
    RXJS_VERSION="7.8.1"
    ZONE_VERSION="0.12.0"
    NODE_IMAGE="node:18.20.8"
    ADAPTER="legacy"
    ;;

  16)
    ANGULAR_VERSION="16.2.12"
    CLI_VERSION="16.2.16"
    BUILD_VERSION="16.2.16"
    TYPESCRIPT_VERSION="5.1.6"
    RXJS_VERSION="7.8.1"
    ZONE_VERSION="0.13.3"
    NODE_IMAGE="node:18.20.8"
    ADAPTER="legacy"
    ;;

  19)
    ANGULAR_VERSION="19.2.25"
    CLI_VERSION="19.2.27"
    BUILD_VERSION="19.2.27"
    TYPESCRIPT_VERSION="5.8.3"
    RXJS_VERSION="7.8.2"
    ZONE_VERSION="0.15.1"
    NODE_IMAGE="node:20.19.5"
    ADAPTER="legacy"
    ;;

  20)
    ANGULAR_VERSION="20.3.27"
    CLI_VERSION="20.3.32"
    BUILD_VERSION="20.3.32"
    TYPESCRIPT_VERSION="5.9.3"
    RXJS_VERSION="7.8.2"
    ZONE_VERSION="0.15.1"
    NODE_IMAGE="node:20.19.5"
    ADAPTER="modern"
    TYPESCRIPT_MODULE="es2022"
    TYPESCRIPT_MODULE_RESOLUTION="bundler"
    ;;

  21)
    ANGULAR_VERSION="21.2.19"
    CLI_VERSION="21.2.19"
    BUILD_VERSION="21.2.19"
    TYPESCRIPT_VERSION="5.9.3"
    RXJS_VERSION="7.8.2"
    ZONE_VERSION="0.16.0"
    NODE_IMAGE="node:20.19.5"
    ADAPTER="modern"
    TYPESCRIPT_MODULE="es2022"
    TYPESCRIPT_MODULE_RESOLUTION="bundler"
    ;;

  22)
    ANGULAR_VERSION="22.1.0"
    CLI_VERSION="22.1.2"
    BUILD_VERSION="22.1.2"
    TYPESCRIPT_VERSION="6.0.2"
    RXJS_VERSION="7.8.2"
    ZONE_VERSION="0.16.0"
    NODE_IMAGE="node:22.22.3"
    ADAPTER="modern"
    TYPESCRIPT_MODULE="es2022"
    TYPESCRIPT_MODULE_RESOLUTION="bundler"
    ;;

  *)
    echo "Usage: $0 {9|11|12|15|16|19|20|21|22}"
    exit 2
    ;;
esac

test -f "$CLIENT_PACKAGE_PATH"

if [[ "$ADAPTER" == "legacy" ]]
then
  test -f "$LEGACY_PACKAGE_PATH"
elif [[ "$ADAPTER" == "modern" ]]
then
  test -f "$MODERN_PACKAGE_PATH"
fi

TEMP_DIRECTORY="$(
  mktemp \
    --directory \
    "/tmp/rulegate-angular-$ANGULAR_MAJOR-compatibility-XXXXXX"
)"

cleanup()
{
  rm -rf "$TEMP_DIRECTORY"
}

trap cleanup EXIT

mkdir -p "$TEMP_DIRECTORY/src"

cat >"$TEMP_DIRECTORY/package.json" <<EOF_PACKAGE
{
  "name": "rulegate-angular-$ANGULAR_MAJOR-compatibility-consumer",
  "version": "0.0.0",
  "private": true,
  "dependencies": {
    "@angular/animations": "$ANGULAR_VERSION",
    "@angular/common": "$ANGULAR_VERSION",
    "@angular/compiler": "$ANGULAR_VERSION",
    "@angular/core": "$ANGULAR_VERSION",
    "@angular/platform-browser": "$ANGULAR_VERSION",
    "@angular/platform-browser-dynamic": "$ANGULAR_VERSION",
    "@angular/router": "$ANGULAR_VERSION",
    "@fotbiler/rulegate-client": "file:/workspace/artifacts/npm/$(basename "$CLIENT_PACKAGE_PATH")",
    "rxjs": "$RXJS_VERSION",
    "tslib": "$TSLIB_VERSION",
    "zone.js": "$ZONE_VERSION"
  },
  "devDependencies": {
    "@angular-devkit/build-angular": "$BUILD_VERSION",
    "@angular/cli": "$CLI_VERSION",
    "@angular/compiler-cli": "$ANGULAR_VERSION",
    "typescript": "$TYPESCRIPT_VERSION"
  }
}
EOF_PACKAGE

if [[ "$ADAPTER" == "legacy" ]]
then
  node -e \
    "const fs=require('fs');const p='$TEMP_DIRECTORY/package.json';const m=JSON.parse(fs.readFileSync(p));m.dependencies['@fotbiler/rulegate-angular-legacy']='file:/workspace/artifacts/npm/$(basename "$LEGACY_PACKAGE_PATH")';fs.writeFileSync(p,JSON.stringify(m,null,2)+'\\n')"
elif [[ "$ADAPTER" == "modern" ]]
then
  node -e \
    "const fs=require('fs');const p='$TEMP_DIRECTORY/package.json';const m=JSON.parse(fs.readFileSync(p));m.dependencies['@fotbiler/rulegate-angular']='file:/workspace/artifacts/npm/$(basename "$MODERN_PACKAGE_PATH")';fs.writeFileSync(p,JSON.stringify(m,null,2)+'\\n')"
fi

cat >"$TEMP_DIRECTORY/angular.json" <<'EOF_ANGULAR'
{
  "$schema": "./node_modules/@angular/cli/lib/config/schema.json",
  "version": 1,
  "projects": {
    "consumer": {
      "projectType": "application",
      "root": "",
      "sourceRoot": "src",
      "architect": {
        "build": {
          "builder": "@angular-devkit/build-angular:browser",
          "options": {
            "outputPath": "dist",
            "index": "src/index.html",
            "main": "src/main.ts",
            "tsConfig": "tsconfig.app.json",
            "aot": true
          },
          "configurations": {
            "production": {
              "optimization": true,
              "outputHashing": "all",
              "sourceMap": false,
              "extractLicenses": true,
              "namedChunks": false
            }
          }
        }
      }
    }
  }
}
EOF_ANGULAR

cat >"$TEMP_DIRECTORY/tsconfig.json" <<EOF_TSCONFIG
{
  "compilerOptions": {
    "experimentalDecorators": true,
    "importHelpers": true,
    "module": "$TYPESCRIPT_MODULE",
    "moduleResolution": "$TYPESCRIPT_MODULE_RESOLUTION",
    "noImplicitAny": true,
    "noImplicitReturns": true,
    "skipLibCheck": false,
    "strict": true,
    "target": "es2015"
  },
  "angularCompilerOptions": {
    "enableIvy": true,
    "strictInjectionParameters": true,
    "strictTemplates": true
  }
}
EOF_TSCONFIG

cat >"$TEMP_DIRECTORY/tsconfig.app.json" <<'EOF_APP_TSCONFIG'
{
  "extends": "./tsconfig.json",
  "compilerOptions": {
    "outDir": "./out-tsc/app",
    "types": []
  },
  "files": ["src/main.ts"]
}
EOF_APP_TSCONFIG

cat >"$TEMP_DIRECTORY/src/index.html" <<'EOF_HTML'
<!doctype html>
<html lang="en">
  <head>
    <meta charset="utf-8">
    <title>RuleGate compatibility consumer</title>
    <base href="/">
  </head>
  <body>
    <app-root></app-root>
  </body>
</html>
EOF_HTML

if [[ "$ADAPTER" == "client" ]]
then
  cat >"$TEMP_DIRECTORY/src/main.ts" <<'EOF_CLIENT'
import { Component, NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { platformBrowserDynamic } from '@angular/platform-browser-dynamic';
import { RuleGateAuthorizationStore } from '@fotbiler/rulegate-client';

@Component({
  selector: 'app-root',
  template: '<span>{{ allowed ? "allowed" : "denied" }}</span>',
})
class AppComponent {
  readonly allowed: boolean;

  constructor() {
    const authorization = new RuleGateAuthorizationStore();
    authorization.replaceSnapshot({ permissions: ['documents.read'] });
    this.allowed = authorization.hasPermission('documents.read');
  }
}

@NgModule({ declarations: [AppComponent], imports: [BrowserModule], bootstrap: [AppComponent] })
class AppModule {}

platformBrowserDynamic().bootstrapModule(AppModule);
EOF_CLIENT
elif [[ "$ADAPTER" == "legacy" ]]
then
  cat >"$TEMP_DIRECTORY/src/main.ts" <<'EOF_LEGACY'
import { Component, NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { platformBrowserDynamic } from '@angular/platform-browser-dynamic';
import { RouterModule, Routes } from '@angular/router';
import {
  RuleGateLegacyAuthorizationClient,
  RuleGateLegacyGuard,
  RuleGateLegacyModule,
  ruleGateLegacyRouteData,
} from '@fotbiler/rulegate-angular-legacy';

@Component({ selector: 'app-protected', template: 'protected' })
class ProtectedComponent {}

@Component({
  selector: 'app-root',
  template: '<span *ruleGateLegacyCan="requirement">allowed</span>',
})
class AppComponent {
  readonly requirement = { permission: 'documents.read' } as const;

  constructor(authorization: RuleGateLegacyAuthorizationClient) {
    authorization.replaceSnapshot({ permissions: ['documents.read'] });
  }
}

const routes: Routes = [
  {
    path: 'protected',
    component: ProtectedComponent,
    canActivate: [RuleGateLegacyGuard],
    data: ruleGateLegacyRouteData({ permission: 'documents.read' }),
  },
];

@NgModule({
  declarations: [AppComponent, ProtectedComponent],
  imports: [BrowserModule, RuleGateLegacyModule, RouterModule.forRoot(routes)],
  bootstrap: [AppComponent],
})
class AppModule {}

platformBrowserDynamic().bootstrapModule(AppModule);
EOF_LEGACY

  if [[ "$ANGULAR_MAJOR" == "19" ]]
  then
    node -e \
      "const fs=require('fs');const p='$TEMP_DIRECTORY/src/main.ts';const s=fs.readFileSync(p,'utf8').replaceAll('@Component({','@Component({ standalone: false,');fs.writeFileSync(p,s)"
  fi
else
  cat >"$TEMP_DIRECTORY/src/main.ts" <<'EOF_MODERN'
import { Component, NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { platformBrowserDynamic } from '@angular/platform-browser-dynamic';
import { RouterModule, Routes } from '@angular/router';
import {
  RuleGateAuthorizationClient,
  RuleGateCanDirective,
  ruleGateGuard,
  ruleGateRouteData,
} from '@fotbiler/rulegate-angular';

@Component({ selector: 'app-protected', standalone: false, template: 'protected' })
class ProtectedComponent {}

@Component({
  selector: 'app-root',
  standalone: false,
  template: '<span *ruleGateCan="requirement">allowed</span>',
})
class AppComponent {
  readonly requirement = { permission: 'documents.read' } as const;

  constructor(authorization: RuleGateAuthorizationClient) {
    authorization.replaceSnapshot({ permissions: ['documents.read'] });
  }
}

const routes: Routes = [
  {
    path: 'protected',
    component: ProtectedComponent,
    canActivate: [ruleGateGuard],
    data: ruleGateRouteData({ permission: 'documents.read' }),
  },
];

@NgModule({
  declarations: [AppComponent, ProtectedComponent],
  imports: [BrowserModule, RuleGateCanDirective, RouterModule.forRoot(routes)],
  bootstrap: [AppComponent],
})
class AppModule {}

platformBrowserDynamic().bootstrapModule(AppModule);
EOF_MODERN
fi

printf '\n== Angular %s package-only compatibility consumer ==\n' "$ANGULAR_MAJOR"

docker run \
  --rm \
  --user "$(id -u):$(id -g)" \
  --env NPM_CONFIG_CACHE=/tmp/npm-cache \
  --tmpfs /tmp \
  --volume "$REPOSITORY_ROOT:/workspace:ro" \
  --volume "$TEMP_DIRECTORY:/consumer" \
  --workdir /consumer \
  "$NODE_IMAGE" \
  sh -c 'npm install --legacy-peer-deps --no-audit --no-fund && ./node_modules/.bin/ng build consumer --configuration production'

test -f "$TEMP_DIRECTORY/dist/index.html"

printf 'Angular %s compatibility consumer passed with the %s adapter.\n' \
  "$ANGULAR_MAJOR" \
  "$ADAPTER"
