# Document Approval Reference Application

This full-stack sample adapts the clean Angular, PrimeNG, and Keycloak patterns
used by a production-style application while keeping the infrastructure small.

It demonstrates:

- Angular 22 with an Aura-based responsive application shell;
- Keycloak login, PKCE, refresh, logout, and bearer-token attachment owned by
  the host application;
- RuleGate generated identifiers, route guards, visibility, and disabled-state
  directives;
- ASP.NET Core bearer-token validation and optional Keycloak subject mapping;
- local YAML policies with subject and resource attribute enrichment;
- a SQLite document and user-profile store;
- backend enforcement for every protected operation.

The sample does not use Keycloak Admin API, remote policy evaluation, Redis,
Hangfire, object storage, a gateway, or a background worker.

## Run

The checked-in development configuration temporarily targets the existing
`ebys-dys` realm and its `ebys-dys-frontend` and `ebys-dys-api` clients. It is
for local verification only and will be replaced by dedicated RuleGate sample
configuration before this branch is pushed.

```bash
dotnet run --project samples/document-approval/api
pnpm --dir samples/document-approval/web install --frozen-lockfile
pnpm --dir samples/document-approval/web start
```

Open `http://localhost:4200`. The API listens on `http://localhost:5088` and
creates `rulegate-sample.db` in its content root.

## Authorization scenarios

| Operation         | RuleGate decision inputs                                                                                                  |
| ----------------- | ------------------------------------------------------------------------------------------------------------------------- |
| List and read     | `DOC.READ` and organization scope for a single document                                                                   |
| Create            | `DOC.CREATE` and trusted API request channel                                                                              |
| Update            | `DOC.UPDATE`, draft state, and ownership                                                                                  |
| Submit            | `WFL.START`, draft state, and ownership                                                                                   |
| Approve or reject | Effective `APPROVER` realm role, `WFL.APPROVE` or `WFL.REJECT`, submitted state, same organization, and a different owner |

The database seeds profiles for the EBYS/DYS QA usernames. A dedicated realm,
clients, roles, composites, protocol mapper, redirect URI, and web origin are
required before publishing. The exact target configuration is documented in
[Dedicated Keycloak setup](keycloak/README.md); those settings are intentionally
not automated by the application.
