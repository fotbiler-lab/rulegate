# Keycloak Integration

RuleGate can consume effective roles and explicit permission claims from
Keycloak without coupling the authorization engine to Keycloak.

The integration is split into two optional surfaces:

- `Fotbiler.RuleGate.Keycloak` maps an authenticated ASP.NET Core
  `ClaimsPrincipal` to a RuleGate subject.
- `@fotbiler/rulegate-angular/keycloak` maps a `keycloak-js` session to the
  provider-independent Angular authorization snapshot.

Applications that do not use Keycloak do not reference either integration.
The RuleGate engine, ASP.NET Core package, and primary Angular entrypoint remain
provider-independent.

## Security boundary

Keycloak performs authentication and issues tokens. ASP.NET Core validates the
token issuer, signature, lifetime, and audience. RuleGate then evaluates local
authorization policies from the validated identity.

The Keycloak integrations do not:

- configure login or bearer-token validation
- store or refresh tokens
- call Keycloak or its Admin API
- resolve a remote composite-role graph
- turn every token claim into a RuleGate attribute
- treat frontend checks as backend security

## ASP.NET Core setup

Install the optional package alongside the normal ASP.NET Core integration:

```bash
dotnet add package Fotbiler.RuleGate.Keycloak --version 0.5.0-preview.1
```

Configure authentication in the host application, preserving Keycloak's claim
names, then select the client roles RuleGate may consume:

```csharp
using Fotbiler.RuleGate.AspNetCore.DependencyInjection;
using Fotbiler.RuleGate.Keycloak.DependencyInjection;

builder.Services
    .AddAuthentication()
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Keycloak:Authority"];
        options.Audience = builder.Configuration["Keycloak:Audience"];
        options.MapInboundClaims = false;
    });

builder.Services
    .AddRuleGate()
    .UseKeycloakSubjectMapping(options =>
    {
        options.ClientIds.Add("rulegate-api");
    });
```

`UseKeycloakSubjectMapping` replaces only `IRuleGateSubjectFactory`. It does
not replace authentication, the RuleGate engine, policies, diagnostics, or
HTTP authorization behavior.

The defaults consume:

| Purpose               | Claim                               |
| --------------------- | ----------------------------------- |
| Subject identifier    | `sub`                               |
| Realm roles           | `realm_access.roles`                |
| Selected client roles | `resource_access.<client-id>.roles` |
| Explicit permissions  | `permission`                        |

Realm-role mapping can be disabled. Client roles are never imported globally;
each client ID must be added to `ClientIds`. Claim names and permission claim
types can be changed through `RuleGateKeycloakSubjectOptions`.

## Canonical role names

Realm and client roles have separate namespaces so equal raw role names cannot
collide:

```text
keycloak:realm:<encoded-role>
keycloak:client:<encoded-client-id>:<encoded-role>
```

Components use UTF-8 RFC 3986 percent encoding. For example:

```text
keycloak:realm:administrator
keycloak:client:rulegate-api:documents.reader
keycloak:client:web%20portal:documents%2Fread
```

Use these canonical identifiers in manifest role requirements:

```yaml
requirement:
  role: keycloak:client:rulegate-api:documents.reader
```

Keycloak access tokens already contain the effective roles assigned to the
subject. Composite descendants present in the token are normalized like every
other effective role; RuleGate does not contact Keycloak to expand them.

## Angular setup

The Keycloak adapter is a secondary entrypoint of the existing Angular package:

```ts
import Keycloak from 'keycloak-js';
import { inject, Injectable } from '@angular/core';
import { RuleGateKeycloakAdapter } from '@fotbiler/rulegate-angular/keycloak';

@Injectable({ providedIn: 'root' })
export class ApplicationIdentityBridge {
  private readonly adapter = inject(RuleGateKeycloakAdapter);

  synchronize(keycloak: Keycloak): boolean {
    return this.adapter.synchronize(keycloak, {
      clientIds: ['rulegate-web'],
    });
  }

  clear(): void {
    this.adapter.clear();
  }
}
```

The host application owns `keycloak.init`, login, refresh, logout, and callback
composition. Call `synchronize` after successful initialization and token
refresh. Call `clear` before identity changes, on logout, and after terminal
authentication errors.

The RuleGate npm package has no `keycloak-js` dependency or peer dependency.
Its structural adapter accepts a `keycloak-js` instance when the application
chooses to install and use that library.

The adapter includes realm roles by default and client roles only for selected
client IDs. It reads the top-level `permission` token claim when present. That
claim may be a string or an array of strings and can be renamed or disabled in
`RuleGateKeycloakSnapshotOptions`.

`createRuleGateSnapshotFromKeycloak` exposes the same conversion as a pure
function. Use it when an application needs to compose token roles with a
separate backend-provided UI policy projection before replacing the generic
`RuleGateAuthorizationClient` snapshot. Reject a `null` conversion and clear
the client instead of retaining grants from a previous identity.

## Fail-closed behavior

- Unauthenticated frontend sessions clear the Angular snapshot.
- Missing subject identifiers reject backend subject creation.
- Multiple distinct subject or structured-role claims are rejected.
- Malformed JSON, role arrays, selected client access, and permission arrays
  are rejected.
- Matching remains exact, ordinal, and case-sensitive.
- Roles or permissions absent from the validated token cannot be inferred.
- Backend RuleGate policies remain the enforcement boundary.
