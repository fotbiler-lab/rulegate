# Reference Applications

RuleGate includes package-consuming applications that can be built and run
without referencing framework source projects.

## Minimal ASP.NET Core

[`samples/aspnetcore-minimal`](../samples/aspnetcore-minimal/README.md) is the
smallest complete HTTP example. It compiles a YAML manifest at startup,
registers RuleGate, protects a Minimal API endpoint, and demonstrates allowed
and denied requests. Its detailed manifest also provides copyable examples of
every requirement family without adding full-stack infrastructure to the host.
Its committed `authorization.tests.yaml` evaluates representative allow, deny,
indeterminate, default-deny, resource, context, and null/empty behavior directly
against that manifest without starting the HTTP application.

Its header authentication handler is deliberately local to the sample. It is
not a production authentication pattern.

## Document approval

[`samples/document-approval`](../samples/document-approval/README.md) is the
full-stack reference application and the modern Angular reference.
Its [manual verification guide](../samples/document-approval/verification.md)
provides the reproducible Keycloak and five-user authorization test sequence.

| Boundary         | Technology             | Responsibility                                                  |
| ---------------- | ---------------------- | --------------------------------------------------------------- |
| Identity         | Keycloak               | Login, tokens, effective roles, and explicit permission claims  |
| Frontend         | Angular 22 and PrimeNG | Responsive shell, routes, and authorization-aware controls      |
| API              | ASP.NET Core 10        | Token validation and protected document operations              |
| Authorization    | RuleGate               | Local YAML policy evaluation and fail-closed decisions          |
| Application data | EF Core and SQLite     | Profiles, organization schedules, ownership, and workflow state |

The host owns Keycloak initialization, token refresh, logout, and bearer-token
attachment. The optional RuleGate adapters only normalize the validated
identity into provider-independent roles and permissions.

Subject enrichment reads the current username's organization and clearance
from SQLite. Resource enrichment reads document ownership, organization,
classification, and state. The API never accepts these values from request
headers as trusted authorization facts.

The sample combines permission and effective-role checks with ownership,
organization, workflow state, and ordered clearance-to-classification
comparisons. Collection results are filtered by the same per-resource read
policy used by direct requests. Confidential reads combine a manifest-defined
global time envelope with SQLite-backed per-organization business hours emitted
by a scoped context provider. Create operations require the trusted API request
channel.

Angular consumes generated manifest identifiers. Route guards, structural
visibility, and disabled-state directives improve the experience, but each
write operation is protected again by a resource-aware backend policy.

### Prerequisites and Compose scope

The document-approval sample requires:

- an accessible Keycloak instance;
- the manually configured realm, clients, roles, mappers, and test users from
  the [Keycloak setup guide](../samples/document-approval/keycloak/README.md);
- a local PrimeUI license for the PrimeNG 22 application.

The checked-in Docker Compose file builds and starts the API and web
application and persists the SQLite database. It does not start Keycloak,
import a realm, create identities, or inject the local UI license. Use an
untracked deployment-specific configuration or run the licensed web
application locally.

## Package-only verification

The .NET projects reference published NuGet versions. The Angular workspace
sets `linkWorkspacePackages: false`, so the application installs the published
npm package even though the SDK source is present in the same repository.

CI verifies:

- .NET restore, formatting, and build through the solution;
- the generated TypeScript file is byte-exact and current;
- the production Angular application build;
- minimal allowed and denied HTTP decisions;
- deterministic allow and deny decisions for permission-, role-, attribute-,
  context-, resource-, and time-based rules;
- packaged CLI evaluation of allow, deny, indeterminate, and exact failure-code
  fixture expectations;
- database-backed organization schedule calculation and context enrichment;
- SQLite database creation and API startup.

Docker Compose builds the API and web images from the same package-only
projects. Keycloak configuration remains an explicit external prerequisite.

## Framework-independent TypeScript client

The compatibility track extracted the sample's portable snapshot and exact
permission, policy, and role checks into `@fotbiler/rulegate-client`. The
modern Angular adapter adds signals, dependency injection, functional router
guards, and standalone directives; the legacy adapter adds observable,
NgModule, class-guard, and classic-directive APIs. Keycloak lifecycle and
backend authorization remain outside all frontend packages.

See [Frontend compatibility](frontend-compatibility.md) for package selection
across Angular 9–22.

## Security notes

- Do not copy the minimal sample's header authentication into an application.
- Validate issuer, signature, lifetime, and audience before RuleGate mapping.
- Keep Keycloak Admin API credentials out of sample and application runtimes.
- Filter collection queries by the same trusted scope used for single-resource
  authorization.
- Treat frontend authorization state as user-controlled.
- Keep realm exports, test passwords, tokens, and local SQLite files out of the
  repository.
