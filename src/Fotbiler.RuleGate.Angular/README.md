# RuleGate Angular

`@fotbiler/rulegate-angular` provides fail-closed Angular helpers for consuming
a frontend authorization projection in RuleGate applications.

> [!IMPORTANT]
> Browser-side guards and directives are user-experience controls, not security
> boundaries. Always enforce authorization on the backend with RuleGate.

## Install

```bash
pnpm add @fotbiler/rulegate-angular
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

Permission and policy guard factories accept string constants directly:

```ts
import { Routes } from '@angular/router';
import { ruleGatePermissionGuard, ruleGatePolicyGuard } from '@fotbiler/rulegate-angular';

import { GeneratedAuthorization } from './generated/authorization';

export const routes: Routes = [
  {
    path: 'documents',
    loadComponent: () => import('./documents/documents.component'),
    canActivate: [
      ruleGatePermissionGuard(GeneratedAuthorization.permissions.documentsRead),
      ruleGatePolicyGuard(GeneratedAuthorization.policies.documentsRead),
    ],
  },
];
```

Guards deny navigation when the client is not ready or the exact identifier is
not present. Matching is ordinal and case-sensitive.

## Control template visibility

Import the standalone directive and pass exactly one permission or policy
requirement:

```ts
import { Component } from '@angular/core';
import { RuleGateCanDirective } from '@fotbiler/rulegate-angular';

import { GeneratedAuthorization } from './generated/authorization';

@Component({
  selector: 'app-document-actions',
  imports: [RuleGateCanDirective],
  template: `
    <button *ruleGateCan="{ permission: permissions.documentsWrite }">Edit document</button>
  `,
})
export class DocumentActionsComponent {
  readonly permissions = GeneratedAuthorization.permissions;
}
```

The directive removes its embedded view when state is cleared, malformed, or
denied.

## Generated constants

The public APIs accept ordinary string-valued constants, including `as const`
objects produced or committed by application tooling. The SDK does not treat a
constant as proof of authorization; it only compares it with the current
frontend projection.

## Security behavior

- Uninitialized state denies every check.
- Invalid or whitespace-padded identifiers invalidate the complete snapshot.
- Permission and policy matching is exact and case-sensitive.
- A directive requirement containing both or neither identifier kind denies.
- Browser state can be modified by the user and never replaces backend
  authorization.

See the full
[RuleGate Angular guide](https://github.com/fotbiler-lab/rulegate/blob/main/docs/angular.md)
and [security model](https://github.com/fotbiler-lab/rulegate/blob/main/docs/security.md).

## License

Licensed under the
[Apache License 2.0](https://github.com/fotbiler-lab/rulegate/blob/main/LICENSE).
