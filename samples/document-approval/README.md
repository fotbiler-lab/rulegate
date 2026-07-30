# Document Approval Reference Application

This full-stack sample adapts the clean Angular, PrimeNG, PrimeFlex, and
Keycloak patterns used by a production-style application while keeping the
infrastructure small. Its responsive application shell is adapted from the
MIT-licensed [Sakai](https://github.com/primefaces/sakai-ng) template and
updated for Angular 22 and PrimeNG 22.

It demonstrates:

- Angular 22 with PrimeNG 22, PrimeFlex, Aura, and a Sakai-based responsive
  application shell;
- Keycloak login, PKCE, refresh, logout, and bearer-token attachment owned by
  the host application;
- RuleGate generated identifiers, route guards, visibility, and disabled-state
  directives;
- ASP.NET Core bearer-token validation and optional Keycloak subject mapping;
- local YAML policies with subject and resource attribute enrichment;
- layered permission-, role-, attribute-, and context-based decisions;
- a SQLite document, user-profile, and organization-schedule store;
- backend enforcement for every protected operation.

The sample does not use Keycloak Admin API, remote policy evaluation, Redis,
Hangfire, object storage, a gateway, or a background worker.

## Prerequisites

- .NET 10 SDK, Node.js, and pnpm 11;
- an accessible Keycloak instance;
- the dedicated realm, clients, roles, mappers, and test users described in
  [Dedicated Keycloak setup](keycloak/README.md);
- a valid PrimeUI license stored only in the ignored local configuration file.

The checked-in `compose.yaml` builds the API and web application and persists
SQLite data. It does not start Keycloak, import the realm, create test users,
or inject the local PrimeUI license.

## Run

The checked-in configuration targets the dedicated `rulegate-samples` realm
and the `rulegate-document-approval-web` and
`rulegate-document-approval-api` clients.

```bash
dotnet run --project samples/document-approval/api
pnpm --dir samples/document-approval/web install --frozen-lockfile
pnpm --dir samples/document-approval/web start
```

PrimeNG 22 requires a valid PrimeUI license. Before starting the web application,
copy `web/public/app-config.local.example.json` to
`web/public/app-config.local.json` and add your license key. The local file is
ignored by Git and is merged with the checked-in application configuration at
runtime.

Open `http://localhost:4200`. The API listens on `http://localhost:5088` and
creates `rulegate-sample.db` in its content root.

## Authorization scenarios

| Operation         | RuleGate decision inputs                                                                                                                                                 |
| ----------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| List and read     | `DOC.READ`, same organization, clearance at or above classification, a global weekday time envelope, and SQLite-backed organization hours for confidential documents     |
| Create            | `DOC.CREATE`, trusted API request channel, and clearance at or above the requested classification                                                                        |
| Update            | `DOC.UPDATE`, draft state, ownership, and clearance at or above both the existing and requested classification                                                           |
| Submit            | `WFL.START`, draft state, ownership, and clearance at or above classification                                                                                            |
| Approve or reject | Effective `APPROVER` realm role, `WFL.APPROVE` or `WFL.REJECT`, submitted state, same organization, a different owner, and clearance at or above document classification |

The list endpoint first applies the collection permission and organization
scope, then evaluates the `document-read` policy for every candidate. A record
that fails its resource decision is omitted rather than disclosed. Direct
single-document requests use the same resource policy and return a generic
authorization denial.

Classification levels are ordered as `public` (1), `internal` (2), and
`confidential` (3). They are loaded from trusted local profiles and persisted
documents; request headers cannot raise either value. Confidential reads must
fit both the global weekday 05:00–22:00 `Europe/Istanbul` policy envelope and
the current user's SQLite-backed organization schedule: `records` uses
08:00–18:00 and `legal` uses 06:00–20:00. The scoped context provider resolves
the authenticated user's organization, reads its schedule, and evaluates it
against RuleGate's trusted evaluation time.

## Verification matrix

The integration suite compiles this sample's checked-in `rulegate.yaml` and
verifies both allow and deny outcomes for:

- missing permissions and roles;
- cross-organization and non-owner access;
- draft, submitted, and completed workflow states;
- clearance-to-classification comparisons;
- confidential reads inside and outside business hours;
- different `records` and `legal` organization schedules;
- missing or invalid organization schedules;
- trusted, incorrect, and missing request-channel context;
- missing attributes and fail-closed evaluation.

Follow the [manual verification guide](verification.md) to reproduce the
dedicated Keycloak configuration and exercise every user, workflow, resource,
classification, and time-window scenario in a stable order.

Run the exact policy matrix with:

```bash
dotnet test tests/Fotbiler.RuleGate.Integration.Tests \
  --framework net10.0 \
  --filter FullyQualifiedName~DocumentApprovalPolicyTests
```

The database seeds profiles for the five documented test usernames. The realm,
clients, roles, composites, protocol mappers, redirect URI, and web origin are
documented in [Dedicated Keycloak setup](keycloak/README.md); those settings are
intentionally not automated by the application.
