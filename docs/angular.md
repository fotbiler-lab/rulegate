# Angular SDK

The RuleGate Angular SDK provides fail-closed user-interface helpers for a
frontend authorization projection.

Install the current preview from npm:

```bash
pnpm add @fotbiler/rulegate-angular@0.7.0-preview.1 @fotbiler/rulegate-client@0.7.0-preview.1
```

> [!IMPORTANT]
> Route guards and template visibility are user-experience controls. Browser
> state is not trusted, and every protected backend operation must perform its
> own authorization check.

## Package scope

The package provides:

- `RuleGateAuthorizationClient` for holding the current frontend projection
- Declarative route metadata and a shared route guard
- Application-defined denied-navigation handling
- Permission, policy, and role route-guard factories for direct checks
- Standalone visibility and disabled-state directives
- Deterministic TypeScript constants generated from `rulegate.yaml`
- Public TypeScript models for snapshots and requirements

The modern package supports Angular 20–22. Angular 12–19 applications use
`@fotbiler/rulegate-angular-legacy`; Angular 9–11 applications consume the
framework-independent `@fotbiler/rulegate-client` through a small host-owned
service. See [Frontend compatibility](frontend-compatibility.md) for the full
matrix, installation, and legacy support policy.

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
  roles: ['documents.reader'],
});
```

Call `clear()` during sign-out and before changing identities. The client
copies and deduplicates accepted identifiers. An invalid identifier rejects
the complete snapshot and clears all grants.

## Protect routes

Use `ruleGateRouteData` with the shared `ruleGateGuard` to keep authorization
requirements visible in route configuration:

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

Missing or malformed route metadata denies directly. A valid denied
requirement uses the application's denied-navigation handler. Without a
configured handler, navigation is cancelled.

Register a handler when denied navigation should redirect:

```ts
import { ApplicationConfig, inject } from '@angular/core';
import { RedirectCommand, Router } from '@angular/router';
import { provideRuleGateDeniedNavigation } from '@fotbiler/rulegate-angular';

export const appConfig: ApplicationConfig = {
  providers: [
    provideRuleGateDeniedNavigation(() => {
      const router = inject(Router);
      return new RedirectCommand(router.parseUrl('/forbidden'));
    }),
  ],
};
```

The handler may return any Angular guard result, including `false`, a
`UrlTree`, or a `RedirectCommand`. The permission, policy, and role guard factories
remain available for routes that do not use declarative metadata.

## Control template visibility

Import the standalone directive and pass one permission, policy, or role:

```ts
import { Component } from '@angular/core';
import { RuleGateCanDirective } from '@fotbiler/rulegate-angular';

import { RuleGateIdentifiers } from './generated/rulegate';

@Component({
  selector: 'app-document-actions',
  imports: [RuleGateCanDirective],
  template: `
    <button *ruleGateCan="{ permission: permissions.documentsWrite }; else unavailable">
      Edit document
    </button>

    <ng-template #unavailable>Editing is unavailable.</ng-template>
  `,
})
export class DocumentActionsComponent {
  readonly permissions = RuleGateIdentifiers.permissions;
}
```

The directive renders the protected view only while the requirement is
granted. Its optional `else` template is rendered while state is uninitialized,
cleared, malformed, or denied.

## Disable interactions

Use `RuleGateDisableDirective` when an action should remain visible but disabled:

```ts
import { Component } from '@angular/core';
import { RuleGateDisableDirective } from '@fotbiler/rulegate-angular';

import { RuleGateIdentifiers } from './generated/rulegate';

@Component({
  selector: 'app-document-delete',
  imports: [RuleGateDisableDirective],
  template: `
    <button type="button" [ruleGateDisable]="{ permission: permissions.documentsDelete }">
      Delete document
    </button>
  `,
})
export class DocumentDeleteComponent {
  readonly permissions = RuleGateIdentifiers.permissions;
}
```

Native controls receive their `disabled` property. Other interactive hosts
receive `aria-disabled`, expose `data-rulegate-disabled`, and have denied click
activation blocked. Keyboard and focus behavior for custom controls remains an
application responsibility.

## Generate TypeScript identifiers

The npm package includes `rulegate-angular`, which generates deterministic
TypeScript constants from the manifest's policies, permissions, roles,
resource types, and actions:

```bash
pnpm exec rulegate-angular generate \
  ./rulegate.yaml \
  --output ./src/app/generated/rulegate.ts
```

Verify committed output in CI without modifying it:

```bash
pnpm exec rulegate-angular generate \
  ./rulegate.yaml \
  --output ./src/app/generated/rulegate.ts \
  --check
```

Generation sorts identifiers ordinally, rejects generated-name collisions,
writes files atomically, and uses byte-exact stale-output detection. Run the
backend RuleGate CLI validation as the authoritative full-manifest check. The
TypeScript generator validates the identifier-bearing manifest shape needed
for generation; generated constants reduce drift but do not prove that the
current user is authorized.

Backend-only requirement kinds such as attribute comparisons, time and
date-time windows, context age, and canonical context policies are recognized
without generating frontend grants from them. This lets the same manifest
drive backend authorization and frontend identifiers while preserving the
backend as the authorization boundary.

## Fail-closed behavior

- Uninitialized and cleared state denies every check.
- Empty, non-string, or whitespace-padded identifiers invalidate a snapshot.
- Matching is exact and case-sensitive.
- Replacing a snapshot replaces every prior grant.
- Missing or invalid route metadata denies navigation.
- Invalid directive requirements render no protected content and keep disabled
  hosts denied.
- Client-side checks never replace backend authorization.

Read the [security model](security.md) for the complete trust boundary.

## Optional Keycloak adapter

Applications using Keycloak can import the optional
`@fotbiler/rulegate-angular/keycloak` secondary entrypoint. The primary Angular
entrypoint has no `keycloak-js` dependency and remains provider-independent.

See the [Keycloak integration guide](keycloak.md) for session synchronization,
canonical realm and client role names, and the matching ASP.NET Core package.
