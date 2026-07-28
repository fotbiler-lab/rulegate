# RuleGate Angular

`@fotbiler/rulegate-angular` provides fail-closed Angular helpers for consuming
a frontend authorization projection in RuleGate applications.

> [!IMPORTANT]
> Browser-side guards and directives are user-experience controls, not security
> boundaries. Always enforce authorization on the backend with RuleGate.

## Install

```bash
pnpm add @fotbiler/rulegate-angular@0.4.0-preview.2
```

The package requires Angular 22.

## Supply authorization state

Load the current user's frontend authorization projection through application
code, then replace the complete SDK snapshot:

```ts
import { inject } from '@angular/core';
import { RuleGateAuthorizationClient } from '@fotbiler/rulegate-angular';

const authorization = inject(RuleGateAuthorizationClient);

authorization.replaceSnapshot({
  permissions: ['documents.read'],
  policies: ['documents-read'],
});
```

Call `clear()` during sign-out and before replacing identity state. Missing or
malformed state denies every check.

## Protect routes

Declarative route metadata keeps requirements visible in route configuration:

```ts
import { Routes } from '@angular/router';
import { ruleGateGuard, ruleGateRouteData } from '@fotbiler/rulegate-angular';

import { RuleGateIdentifiers } from './generated/rulegate';

export const routes: Routes = [
  {
    path: 'documents',
    loadComponent: () => import('./documents/documents.component'),
    canActivate: [ruleGateGuard],
    data: ruleGateRouteData({
      permission: RuleGateIdentifiers.permissions.documentsRead,
    }),
  },
];
```

Missing or malformed metadata denies. Configure
`provideRuleGateDeniedNavigation` to return an Angular `UrlTree` or
`RedirectCommand` for valid denied requirements. The direct permission and
policy guard factories remain available.

## Control template visibility

Import the standalone directive and pass exactly one permission or policy
requirement:

```ts
import { Component } from '@angular/core';
import { RuleGateCanDirective, RuleGateDisableDirective } from '@fotbiler/rulegate-angular';

import { RuleGateIdentifiers } from './generated/rulegate';

@Component({
  selector: 'app-document-actions',
  imports: [RuleGateCanDirective, RuleGateDisableDirective],
  template: `
    <button *ruleGateCan="{ permission: permissions.documentsWrite }; else unavailable">
      Edit document
    </button>
    <ng-template #unavailable>Editing is unavailable.</ng-template>

    <button [ruleGateDisable]="{ permission: permissions.documentsDelete }">Delete document</button>
  `,
})
export class DocumentActionsComponent {
  readonly permissions = RuleGateIdentifiers.permissions;
}
```

The structural directive supports an `else` template. The disabled-state
directive owns the native `disabled` property where available and blocks denied
click activation on other hosts.

## Generate TypeScript identifiers

Generate deterministic constants directly from `rulegate.yaml`:

```bash
pnpm exec rulegate-angular generate rulegate.yaml --output src/app/generated/rulegate.ts
pnpm exec rulegate-angular generate rulegate.yaml --output src/app/generated/rulegate.ts --check
```

The generator fails on malformed identifier-bearing manifest shapes and name
collisions, writes atomically, and checks output byte-for-byte. Use the backend
RuleGate CLI for authoritative full-manifest validation.

## Security behavior

- Uninitialized state denies every check.
- Invalid or whitespace-padded identifiers invalidate the complete snapshot.
- Permission and policy matching is exact and case-sensitive.
- A directive requirement containing both or neither identifier kind denies.
- Missing or malformed declarative route metadata denies navigation.
- Browser state can be modified by the user and never replaces backend
  authorization.

See the full
[RuleGate Angular guide](https://github.com/fotbiler-lab/rulegate/blob/main/docs/angular.md)
and [security model](https://github.com/fotbiler-lab/rulegate/blob/main/docs/security.md).

## License

Licensed under the
[Apache License 2.0](https://github.com/fotbiler-lab/rulegate/blob/main/LICENSE).
