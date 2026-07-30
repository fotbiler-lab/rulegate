# Document Approval Verification Guide

This guide reproduces the complete document-approval reference application
setup and verifies its permission, role, attribute, context, time, and
resource-based authorization behavior.

The application deliberately separates responsibilities:

| Boundary       | Source of truth                                                                     |
| -------------- | ----------------------------------------------------------------------------------- |
| Authentication | Keycloak validates the user session and issues the access token                     |
| Permissions    | Effective API client roles are emitted in the token's `permission` claim            |
| Roles          | Effective Keycloak realm roles are normalized into RuleGate role names              |
| Subject data   | SQLite supplies the user's organization and clearance                               |
| Resource data  | SQLite supplies document ownership, organization, classification, and state         |
| Context        | The API supplies request channel; SQLite supplies schedules; RuleGate supplies time |
| Decisions      | The local `rulegate.yaml` policy combines every trusted input and fails closed      |

Angular route guards and directives project coarse permissions into the user
interface. They are not the security boundary. ASP.NET Core evaluates the
resource-aware policy again for every protected API operation.

## Prerequisites

- .NET 10 SDK
- Node.js and pnpm 11
- A running Keycloak instance
- The dedicated `rulegate-samples` realm described in
  [Dedicated Keycloak setup](keycloak/README.md)
- A local PrimeUI license in the ignored
  `web/public/app-config.local.json` file

Do not commit test passwords, tokens, realm exports containing credentials, or
the local PrimeUI license.

## Start the applications

Start the API from the repository root:

```bash
dotnet run --project samples/document-approval/api
```

Start Angular in a second terminal:

```bash
pnpm --dir samples/document-approval/web install --frozen-lockfile
pnpm --dir samples/document-approval/web start
```

Open `http://localhost:4200`. Restart the API whenever `rulegate.yaml` changes;
the manifest is compiled once during application startup.

The sample creates `OrganizationSchedules` idempotently so an existing local
sample database can be upgraded without deleting previously tested documents.
Production applications should use their normal reviewed database-migration
workflow instead of copying this sample compatibility step.

## Keycloak verification

The API client has these normal, non-composite client roles:

```text
DOC.READ
DOC.CREATE
DOC.UPDATE
WFL.START
WFL.APPROVE
WFL.REJECT
```

The realm roles are composite roles:

| Realm role         | Associated roles                                                     |
| ------------------ | -------------------------------------------------------------------- |
| `VIEWER`           | API client role `DOC.READ`                                           |
| `DOCUMENT_MANAGER` | API client roles `DOC.READ`, `DOC.CREATE`, `DOC.UPDATE`, `WFL.START` |
| `APPROVER`         | API client roles `DOC.READ`, `WFL.APPROVE`, `WFL.REJECT`             |
| `ADMIN`            | Realm roles `VIEWER`, `DOCUMENT_MANAGER`, and `APPROVER`             |

The `rulegate-api-access` client scope must emit effective API client roles in
the multivalued `permission` claim and include
`rulegate-document-approval-api` in the access-token audience.

If a composite role changes, sign out and sign in again. An existing access
token does not acquire the new effective roles. In particular, a correctly
configured `sample-admin` sees both New document and Approvals in the sidebar.

## Authorization model

Classification levels are ordered:

| Classification | Level |
| -------------- | ----: |
| `public`       |     1 |
| `internal`     |     2 |
| `confidential` |     3 |

A subject may create, update, read, submit, or approve a document only when
their clearance is at least the document classification required by the
operation. Confidential reads have two temporal controls:

1. The manifest defines a global weekday 05:00–22:00 `Europe/Istanbul`
   security envelope.
2. `ApiRequestContextProvider` resolves the authenticated user's organization,
   reads its schedule from SQLite, evaluates it against the RuleGate evaluation
   time, and emits `organizationBusinessHoursOpen`.

The seeded schedules are:

| Organization | Working days  | Time zone       | Half-open window |
| ------------ | ------------- | --------------- | ---------------- |
| `records`    | Monday–Friday | Europe/Istanbul | 08:00–18:00      |
| `legal`      | Monday–Friday | Europe/Istanbul | 06:00–20:00      |

An absent organization schedule, invalid time zone, invalid boundary, or
missing authenticated profile fails closed. Organization and schedule data are
never accepted from headers or `appsettings.json`.

The five deterministic users are:

| Username                | Organization | Clearance      | Realm role         |
| ----------------------- | ------------ | -------------- | ------------------ |
| `sample-viewer`         | `records`    | `public`       | `VIEWER`           |
| `sample-manager`        | `records`    | `internal`     | `DOCUMENT_MANAGER` |
| `sample-approver`       | `records`    | `confidential` | `APPROVER`         |
| `sample-legal-approver` | `legal`      | `confidential` | `APPROVER`         |
| `sample-admin`          | `records`    | `confidential` | `ADMIN`            |

## Manual verification sequence

Run the users in the following order. Use the specified document names so the
workflow states remain unambiguous even when the database contains earlier
manual-test data.

### 1. Document manager

Sign in as `sample-manager`.

Expected projection:

- Documents and New document are visible.
- Approvals is hidden.
- Public and internal records documents are visible.
- Confidential documents are not visible because the clearance is internal.

Create these documents:

| Title              | Classification | Final manager action |
| ------------------ | -------------- | -------------------- |
| `RG-PUBLIC-DRAFT`  | Public         | Leave as draft       |
| `RG-APPROVE`       | Internal       | Submit               |
| `RG-REJECT`        | Internal       | Submit               |
| `RG-ADMIN-APPROVE` | Internal       | Submit               |

Attempt to create `RG-CONFIDENTIAL-DENY` as Confidential. The request must
return a generic 403 response, the UI must show the clearance message, and the
document must not be stored. Navigating directly to `/approvals` must show the
access-denied page.

### 2. Viewer

Sign in as `sample-viewer`.

- `RG-PUBLIC-DRAFT` is visible.
- The three internal workflow documents are omitted from the list.
- New document and Approvals are hidden.
- Submit is disabled on the public draft.
- Direct navigation to `/approvals` is denied.

This verifies that collection authorization does not disclose resources above
the subject's clearance.

### 3. Records approver

Sign in as `sample-approver`.

- Approvals is visible and New document is hidden.
- Approve `RG-APPROVE`; its final state is `approved`.
- Reject `RG-REJECT`; its final state is `rejected`.
- Leave `RG-ADMIN-APPROVE` submitted for the administrator.
- Submit remains disabled on draft documents.

During the `records` 08:00–18:00 organization window, `Confidential board
minutes` is visible. Outside the window, the same document is omitted even
though the subject has confidential clearance.

### 4. Legal approver

Sign in as `sample-legal-approver`.

- Only documents in the `legal` organization are visible.
- Records documents, including every `RG-*` document above, are omitted.
- `Legal review checklist` appears in Approvals.
- `Confidential legal opinion` is visible during the `legal` 06:00–20:00
  organization window and omitted outside it.
- Approve and Reject both fail because the current user owns that document.
- The document remains submitted.

This verifies organization isolation and the self-approval prohibition.

### 5. Administrator

Sign in as `sample-admin`.

- Documents, New document, and Approvals are visible.
- Approve `RG-ADMIN-APPROVE`; its final state is `approved`.
- Create `RG-ADMIN-SELF` as Internal and submit it.
- Approve and Reject both fail for `RG-ADMIN-SELF`; administrator status does
  not bypass the self-approval policy.

Create `RG-ADMIN-CONFIDENTIAL` as Confidential. Inside the configured time
window it remains visible. Outside the window creation still succeeds, but the
subsequent resource-filtered list omits it until the window opens.

## Expected final states

| Document                | Expected state or result                              |
| ----------------------- | ----------------------------------------------------- |
| `RG-PUBLIC-DRAFT`       | `draft`                                               |
| `RG-APPROVE`            | `approved`                                            |
| `RG-REJECT`             | `rejected`                                            |
| `RG-ADMIN-APPROVE`      | `approved`                                            |
| `RG-CONFIDENTIAL-DENY`  | Not created                                           |
| `RG-ADMIN-SELF`         | `submitted`; self-approval and self-rejection denied  |
| `RG-ADMIN-CONFIDENTIAL` | Visible only inside the confidential read time window |

## Automated verification

Validate the exact sample manifest:

```bash
dotnet run --project src/Fotbiler.RuleGate.Cli \
  --framework net10.0 -- \
  validate samples/document-approval/api/rulegate.yaml
```

Run the deterministic policy matrix:

```bash
dotnet test tests/Fotbiler.RuleGate.Integration.Tests \
  --framework net10.0 \
  --filter FullyQualifiedName~DocumentApprovalPolicyTests
```

The policy matrix covers allowed and denied permission, role, ownership,
organization, workflow-state, clearance, classification, request-channel,
time-window, and missing-attribute decisions. The sample-specific test project
also verifies SQLite-backed records and legal schedules, boundary behavior,
overnight windows, invalid schedules, missing schedules, and context enrichment.
Time tests use fixed instants so they do not depend on the machine clock.

```bash
dotnet test tests/RuleGate.DocumentApproval.Sample.Tests
```

Verify package-only generation and the Angular production application:

```bash
pnpm samples:generate:check
pnpm samples:build
pnpm samples:format:check
```

## Security expectations

- Keycloak authenticates; it does not evaluate RuleGate policies.
- The browser improves the experience but never grants backend access.
- Subject and resource attributes come from SQLite rather than request headers.
- Context values are supplied only by trusted application code and SQLite data.
- Missing, malformed, incompatible, or insufficient inputs deny access.
- Collection endpoints evaluate the resource read policy before disclosing
  each document.
- Authorization failures use generic 401 or 403 problem details and do not
  expose policy internals.
