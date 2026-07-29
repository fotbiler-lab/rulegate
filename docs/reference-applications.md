# Reference Applications

RuleGate includes package-consuming applications that can be built and run
without referencing framework source projects.

## Minimal ASP.NET Core

[`samples/aspnetcore-minimal`](../samples/aspnetcore-minimal/README.md) is the
smallest complete HTTP example. It compiles a YAML manifest at startup,
registers RuleGate, protects a Minimal API endpoint, and demonstrates allowed
and denied requests.

Its header authentication handler is deliberately local to the sample. It is
not a production authentication pattern.

## Document approval

[`samples/document-approval`](../samples/document-approval/README.md) is the
full-stack reference application and the modern Angular reference.

| Boundary         | Technology             | Responsibility                                                   |
| ---------------- | ---------------------- | ---------------------------------------------------------------- |
| Identity         | Keycloak               | Login, tokens, effective roles, and explicit permission claims   |
| Frontend         | Angular 22 and PrimeNG | Responsive shell, routes, and authorization-aware controls       |
| API              | ASP.NET Core 10        | Token validation and protected document operations               |
| Authorization    | RuleGate               | Local YAML policy evaluation and fail-closed decisions           |
| Application data | EF Core and SQLite     | User profiles, organization scope, ownership, and workflow state |

The host owns Keycloak initialization, token refresh, logout, and bearer-token
attachment. The optional RuleGate adapters only normalize the validated
identity into provider-independent roles and permissions.

Subject enrichment reads the current username's organization and clearance
from SQLite. Resource enrichment reads document ownership, organization,
classification, and state. The API never accepts these values from request
headers as trusted authorization facts.

Angular consumes generated manifest identifiers. Route guards, structural
visibility, and disabled-state directives improve the experience, but each
write operation is protected again by a resource-aware backend policy.

## Package-only verification

The .NET projects reference published NuGet versions. The Angular workspace
sets `linkWorkspacePackages: false`, so the application installs the published
npm package even though the SDK source is present in the same repository.

CI verifies:

- .NET restore, formatting, and build through the solution;
- the generated TypeScript file is byte-exact and current;
- the production Angular application build;
- minimal allowed and denied HTTP decisions;
- SQLite database creation and API startup.

Docker Compose builds the API and web images from the same package-only
projects. It does not provision or mutate Keycloak.

## Framework-independent TypeScript feasibility

The sample confirms a narrow framework-independent client is feasible: the
portable part is an immutable snapshot plus exact permission, policy, and role
membership checks. Angular-specific behavior is limited to signals, dependency
injection, router guards, and directives.

No additional TypeScript package is introduced in this milestone. Extracting a
portable client now would duplicate a very small API and create a compatibility
promise before the Angular and legacy-adapter matrices are defined. A later
compatibility milestone can extract that snapshot contract without moving
Keycloak lifecycle or backend authorization into the browser.

## Security notes

- Do not copy the minimal sample's header authentication into an application.
- Validate issuer, signature, lifetime, and audience before RuleGate mapping.
- Keep Keycloak Admin API credentials out of sample and application runtimes.
- Filter collection queries by the same trusted scope used for single-resource
  authorization.
- Treat frontend authorization state as user-controlled.
- Keep realm exports, test passwords, tokens, and local SQLite files out of the
  repository.
