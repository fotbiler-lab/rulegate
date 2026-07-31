# Frontend Compatibility

RuleGate separates its framework-independent frontend authorization state from
Angular-specific integration. This keeps the backend as the security boundary
while allowing applications on different Angular generations to use an
adapter appropriate to their framework APIs and package format.

## Support matrix

| Angular version | RuleGate package                    | Integration model                                           | Support level |
| --------------- | ----------------------------------- | ----------------------------------------------------------- | ------------- |
| 20–22           | `@fotbiler/rulegate-angular`        | Signals, standalone directives, functional guards           | Current       |
| 12–19           | `@fotbiler/rulegate-angular-legacy` | Observables, NgModule, classic directives, class guard      | Legacy-tested |
| 9–11            | `@fotbiler/rulegate-client`         | Framework-independent store in a host-owned Angular service | Legacy-tested |

[Angular 20–22 are supported by Angular](https://angular.dev/reference/releases)
as of July 2026. Angular 9–19 are end-of-life; RuleGate package-only builds
verify compatibility but cannot provide framework security maintenance.
Applications should upgrade to a vendor-supported Angular release whenever
possible.

The compatibility matrix installs the packed `.tgz` files into real production
consumer builds for Angular 9, 11, 12, 15, 16, 19, 20, 21, and 22. It does not
infer compatibility from source compilation alone.

## Angular 20–22

```bash
pnpm add @fotbiler/rulegate-angular @fotbiler/rulegate-client
```

Use `RuleGateAuthorizationClient`, standalone `RuleGateCanDirective` and
`RuleGateDisableDirective`, `ruleGateGuard`, and the functional guard helpers.
The complete API is documented in the [Angular SDK guide](angular.md).

## Angular 12–19

```bash
pnpm add @fotbiler/rulegate-angular-legacy @fotbiler/rulegate-client
```

Import `RuleGateLegacyModule` in the application module. Supply a complete
snapshot through `RuleGateLegacyAuthorizationClient`; route checks use
`RuleGateLegacyGuard` with `ruleGateLegacyRouteData`.

```ts
authorization.replaceSnapshot({
  permissions: ['documents.read'],
  policies: ['documents-read'],
  roles: ['documents.reader'],
});
```

Classic templates use `*ruleGateLegacyCan` and
`[ruleGateLegacyDisable]`. The adapter exposes `snapshot$` for observable
composition and clears every grant when a snapshot is malformed.

## Angular 9–11

```bash
pnpm add @fotbiler/rulegate-client
```

Create a host-owned Angular service around `RuleGateAuthorizationStore` and
expose only the state shape needed by the application. Angular 9–11 predate the
stable partial-Ivy package format used by the legacy adapter, so RuleGate does
not publish a compiled Angular library for these versions.

```ts
import { Injectable } from '@angular/core';
import {
  RuleGateAuthorizationSnapshot,
  RuleGateAuthorizationStore,
} from '@fotbiler/rulegate-client';

@Injectable({ providedIn: 'root' })
export class AuthorizationService {
  private readonly store = new RuleGateAuthorizationStore();

  replaceSnapshot(snapshot: RuleGateAuthorizationSnapshot): boolean {
    return this.store.replaceSnapshot(snapshot);
  }

  canReadDocuments(): boolean {
    return this.store.hasPermission('documents.read');
  }

  clear(): void {
    this.store.clear();
  }
}
```

## Security boundary

All three packages consume a browser-side projection that users can modify.
They control navigation, visibility, and enabled state only. ASP.NET Core must
load trusted subject, resource, and context data and make the authoritative
RuleGate decision for every protected operation.
