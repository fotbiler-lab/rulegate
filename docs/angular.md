# Angular SDK

The RuleGate Angular SDK provides fail-closed user-interface helpers for a
frontend authorization projection.

Install the current preview from npm:

```bash
pnpm add @fotbiler/rulegate-angular@0.4.0-preview.1
```

> [!IMPORTANT]
> Route guards and template visibility are user-experience controls. Browser
> state is not trusted, and every protected backend operation must perform its
> own authorization check.

## Package scope

The first preview provides:

- `RuleGateAuthorizationClient` for holding the current frontend projection
- Permission and policy route-guard factories
- The standalone `RuleGateCanDirective` structural directive
- Public TypeScript models for snapshots and requirements
- Direct consumption of generated or committed string constants

The package requires Angular 22.

## Supply authorization state

Load the current user's frontend authorization projection through application
code and replace the complete snapshot:

```ts
import { inject } from '@angular/core';
import { RuleGateAuthorizationClient } from '@fotbiler/rulegate-angular';

const authorization = inject(RuleGateAuthorizationClient);

authorization.replaceSnapshot({
  permissions: ['documents.read'],
  policies: ['documents-read'],
});
```

Call `clear()` during sign-out and before changing identities. The client
copies and deduplicates accepted identifiers. An invalid identifier rejects
the complete snapshot and clears all grants.

## Protect routes

Use a guard factory with one exact permission or policy identifier:

```ts
import { Routes } from '@angular/router';
import {
  ruleGatePermissionGuard,
  ruleGatePolicyGuard,
} from '@fotbiler/rulegate-angular';

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

A guard returns `false` before the client is ready, for an invalid identifier,
or when the identifier is not granted.

## Control template visibility

Import the standalone directive and pass either a permission or a policy:

```ts
import { Component } from '@angular/core';
import { RuleGateCanDirective } from '@fotbiler/rulegate-angular';

import { GeneratedAuthorization } from './generated/authorization';

@Component({
  selector: 'app-document-actions',
  imports: [RuleGateCanDirective],
  template: `
    <button *ruleGateCan="{ permission: permissions.documentsWrite }">
      Edit document
    </button>
  `,
})
export class DocumentActionsComponent {
  readonly permissions = GeneratedAuthorization.permissions;
}
```

The directive creates its embedded view only while the requirement is granted.
A requirement containing both a permission and a policy, or neither, denies.

## Generated constants

SDK APIs accept ordinary strings and string-valued `as const` objects. This
keeps generated policy identifiers aligned without coupling the Angular
package to one code generator.

```ts
export const GeneratedAuthorization = {
  permissions: {
    documentsRead: 'documents.read',
  },
  policies: {
    documentsRead: 'documents-read',
  },
} as const;
```

Constants reduce identifier drift; they do not prove that the current user is
authorized.

## Fail-closed behavior

- Uninitialized and cleared state denies every check.
- Empty, non-string, or whitespace-padded identifiers invalidate a snapshot.
- Matching is exact and case-sensitive.
- Replacing a snapshot replaces every prior grant.
- Invalid directive requirements render no protected content.
- Client-side checks never replace backend authorization.

Read the [security model](security.md) for the complete trust boundary.
