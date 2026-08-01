# RuleGate Roadmap

This document describes planned development through RuleGate 1.0. Scope may
change as public APIs, security boundaries, package structure, and package-only
consumer feedback evolve.

## Product direction

RuleGate is a local-first, embedded, provider-independent authorization
framework for .NET and Angular applications. It evaluates application-specific
authorization rules from roles, permissions, claims, and attributes supplied by
the host application or its identity provider.

RuleGate does not replace authentication, Keycloak, ASP.NET Core Identity,
IdentityServer, an application user store, or another identity provider.
Provider-specific integrations remain optional adapters around the
provider-independent authorization engine.

Development through 1.0 preserves five product properties:

- local-first evaluation;
- default-deny and fail-closed behavior;
- provider-independent core packages;
- testable policies and deterministic tooling;
- safe diagnostics that explain structure without exposing sensitive values.

## Current status

| Capability                                   | Status       |
| -------------------------------------------- | ------------ |
| Authorization engine                         | ✅ Available |
| Permission- and role-based authorization     | ✅ Available |
| Logical `all`, `any`, and `not` requirements | ✅ Available |
| Typed scalar attribute comparison            | ✅ Available |
| Subject, resource, and context attributes    | ✅ Available |
| YAML manifest compilation                    | ✅ Available |
| ASP.NET Core integration                     | ✅ Available |
| Authorization diagnostics and logging        | ✅ Available |
| CLI validation and deterministic generation  | ✅ Available |
| Angular SDK and TypeScript generation        | ✅ Available |
| Optional Keycloak helpers                    | ✅ Available |
| Advanced Attribute Operators                 | ✅ Available |
| Attribute-to-Attribute Comparison            | ✅ Available |
| First-Class Time and Context Policies        | ✅ Available |
| ASP.NET Core Enrichment Pipeline             | ✅ Available |
| Official Reference Applications              | ✅ Available |
| Policy Testing CLI                           | ✅ Available |
| Explain and Lint                             | ✅ Available |
| Policy Sources and Atomic Reload             | ✅ Available |
| OpenTelemetry, Benchmarks, and Concurrency   | ✅ Available |
| .NET and Angular Compatibility Track         | ✅ Available |
| API Freeze and Security Hardening            | ⏳ Planned   |
| Stable Release                               | ⏳ Planned   |

The latest RuleGate NuGet preview is `0.9.0-preview.4`. The three-package npm
family is also published at `0.9.0-preview.4` for this compatibility release.
Official Reference Applications, Policy Testing, Explain and Lint, Policy
Sources and Atomic Reload, OpenTelemetry, Benchmarks and Concurrency, and the
compatibility track are available. The next milestone is API Freeze and
Security Hardening in `1.0.0-rc.1`.

All NuGet packages share one version and are published together for every
NuGet release, including packages without code changes. npm packages are
versioned independently from NuGet and remain aligned within the npm package
family.

NuGet and npm release numbers may normally differ from each other. The stable
RuleGate 1.0 milestone is intentionally coordinated: all six NuGet packages
and all three npm packages must be published as exactly `1.0.0`.

## Current platform support

The repository package and consumer matrix verifies:

| Package family                        | Verified platform                              |
| ------------------------------------- | ---------------------------------------------- |
| RuleGate portable libraries           | .NET Standard 2.0, .NET 8, .NET 9, and .NET 10 |
| RuleGate ASP.NET Core integrations    | .NET Core 3.1 and .NET 5 through .NET 10       |
| RuleGate CLI                          | .NET 8, .NET 9, and .NET 10                    |
| Modern Angular adapter                | Angular 20 through Angular 22                  |
| Legacy Angular adapter                | Angular 12 through Angular 19                  |
| Framework-independent frontend client | Angular 9 through Angular 22 consumers         |

The `0.9.0-preview.4` NuGet and npm package families publish this expanded
compatibility matrix after package-only consumer and release verification.

Support levels are:

- `current`: supported by the framework vendor and exercised in RuleGate CI;
- `legacy-tested`: no longer vendor-supported, but verified by RuleGate
  package-only consumers;
- `unsupported`: below .NET Core 3.1 or Angular 9, or not covered by either
  verified support level.

RuleGate cannot extend vendor security support for end-of-life runtimes. Legacy
verification describes compatibility only.

## Published previews

### `0.1.0-preview.1` — Authorization Core Foundation

- Public authorization contracts
- Policy and requirement definitions
- Permission and role requirements
- Logical requirements
- Default-deny and fail-closed evaluation
- In-memory policy provider
- YAML manifest foundation

### `0.2.0-preview.1` — ASP.NET Core Integration Foundation

- Dependency injection registration
- `ClaimsPrincipal` subject mapping
- Resource-based authorization
- Dynamic RuleGate policy names
- Minimal API integration
- Controller and action authorization
- Package-only consumer verification

### `0.2.0-preview.2` — Advanced Authorization and Diagnostics

- Subject, resource, and context attribute requirements
- Typed scalar comparison
- Nested manifest attribute requirements
- Authorization diagnostics contracts
- Structured ASP.NET Core logging
- Generic RuleGate HTTP `401` and `403` problem responses
- Multi-targeting for .NET 8, .NET 9, and .NET 10

### `0.3.0-preview.1` — CLI and Manifest Validation

- Installable `Fotbiler.RuleGate.Cli` .NET tool
- Default and explicit manifest-file validation
- Human-readable and JSON output
- Stable process exit codes
- Fail-closed manifest compilation and structured errors
- Package-only CLI installation and execution smoke tests
- [RuleGate CLI guide](cli.md)

### `0.3.0-preview.2` — Code Generation

- Manifest-derived C# policy, resource-type, and action constants
- Deterministic output and atomic generated-file replacement
- Byte-exact stale-output detection through `--check`
- Identifier, namespace, and collision diagnostics
- Generated-code verification on .NET 8, .NET 9, and .NET 10
- [C# code-generation guide](code-generation.md)

### `0.4.0-preview.1` — Angular SDK Foundation

- Signal-backed authorization client and public TypeScript models
- Permission and policy route guards
- Standalone structural authorization directive
- Generated string-constant consumption
- Angular Package Format build for `@fotbiler/rulegate-angular`
- Package-only npm tarball consumer verification
- [Angular SDK guide](angular.md)

### `0.4.0-preview.2` — Angular Developer Experience

- Declarative route authorization metadata
- Disabled-state and template composition helpers
- Framework-aware denied-navigation handling
- TypeScript generation and backend identifier alignment
- Angular examples and integration consumers

### `0.5.0-preview.1` — Keycloak Helpers

- Realm-, client-, and composite-role mapping
- Claim normalization and RuleGate subject creation
- Optional Angular Keycloak secondary entrypoint
- Package-only npm and NuGet consumer verification
- [Keycloak integration guide](keycloak.md)

### `0.5.0-preview.2` — NuGet Version Alignment

- One synchronized version for all RuleGate NuGet packages
- Aligned package-to-package dependency versions
- Complete six-package NuGet publishing and verification
- Standardized NuGet package README product naming

### `0.6.0-preview.1` — Advanced Attribute Operators

- String `contains`, `startsWith`, and `endsWith` operators
- Explicit ordinal case-sensitive and case-insensitive behavior
- Collection membership, set-intersection, and empty-state operators
- Attribute presence and null-state operators
- Homogeneous collection validation with a 256-element limit
- Defined missing-versus-null semantics and fail-closed type handling

### `0.6.0-preview.2` — Attribute-to-Attribute Comparison

- Subject, resource, context, and literal operands
- Ownership and organization-scope comparisons
- Numeric normalization and date/time comparison
- Defined type compatibility and null/missing behavior
- Manifest validation and safe, value-free evaluation traces

### `0.7.0-preview.1` — First-Class Time and Context Policies

- Explicit-time-zone workday and overnight schedules
- Before, after, and bounded date-time policies
- Authentication-age, MFA-age, and reauthentication windows
- Canonical authentication, channel, network, tenant, organization,
  trusted-device, and identity-type context
- Deterministic `TimeProvider` testing and untrusted-by-default request context
- Angular TypeScript generation compatibility with backend requirement kinds

### `0.7.0-preview.2` — ASP.NET Core Enrichment Pipeline

- Subject, resource, and context attribute-provider abstractions
- Ordered asynchronous enrichment with cancellation
- Explicit attribute precedence and collision behavior
- Fail-closed provider exceptions and missing trusted data
- Sensitive-value-safe diagnostics
- Minimal-hosting and `Startup.cs` integration paths
- [ASP.NET Core enrichment guide](enrichment.md)

### `0.8.0-preview.1` — Official Reference Applications

- A minimal ASP.NET Core authorization sample
- A modern Angular sample using generated identifiers, guards, and directives
- A full-stack document-approval sample with ASP.NET Core, Angular, Keycloak,
  YAML policies, and a sample data store
- Docker Compose setup for the API and web application, with documented
  Keycloak configuration, test identities, and authorization scenarios
- Package-consuming examples rather than source-project shortcuts
- Initial framework-independent TypeScript client and legacy-adapter feasibility
  work required by the compatibility track

The full-stack sample uses the current stable .NET, Angular, and Keycloak
versions at implementation time. Smaller compatibility samples cover legacy
hosting and Angular patterns.

### `0.8.0-preview.2` — Policy Testing CLI

- `rulegate test [authorization.tests.yaml]`
- Human-readable and JSON output
- Allow, deny, and indeterminate expectations
- Expected failure codes, filtering, summaries, and stable CI exit codes
- Manifest and test-fixture validation
- Deterministic execution without starting the host application
- [Policy testing guide](policy-testing.md)

### `0.9.0-preview.1` — Explain and Lint

- `rulegate explain [authorization.tests.yaml] --test <id>`
- Value-free structural explanations using the runtime evaluator pipeline
- Redaction of request values and identity-specific data by default
- `rulegate lint [rulegate.yaml]` for duplicate, contradictory, absorbed,
  overly deep, or unnecessarily complex requirements
- Requirement identifier collisions and risky negative-operator detection
- Stable human-readable and machine-readable output
- [Explain and Lint guide](explain-and-lint.md)

### `0.9.0-preview.2` — Policy Sources and Atomic Reload

- In-memory, YAML file, embedded-resource, configuration, and
  application-defined policy sources
- Complete parse and validation before activation
- Immutable policy snapshots and atomic replacement
- Preservation of the last valid snapshot when a reload fails
- Deterministic reload diagnostics and concurrency tests
- [Policy sources and atomic reload guide](policy-sources.md)

### `0.9.0-preview.3` — OpenTelemetry, Benchmarks, and Concurrency

- Low-cardinality authorization metrics, activities, and traces
- Decision, bounded failure-category, latency, policy-lookup, source-load, and
  reload instrumentation
- No subject IDs, resource IDs, policy/source identifiers, raw roles,
  permissions, claims, or attribute names and values in built-in telemetry
- Benchmarks for scalar, collection, attribute-to-attribute, logical, time, and
  context requirements
- Policy lookup benchmarks at 10, 100, 1,000, and 10,000 policies
- Immutable-snapshot, parallel-evaluation, cancellation, provider, and reload
  race-condition tests
- Configurable long-running stress tests and documented thread-safety contracts
- [Telemetry, performance, and concurrency guide](telemetry-performance-concurrency.md)

### `0.9.0-preview.4` — .NET and Angular Compatibility

- .NET Standard 2.0 targets for Abstractions, Core, and Manifest
- ASP.NET Core and Keycloak packages from .NET Core 3.1 through .NET 10
- Framework-independent `@fotbiler/rulegate-client`
- Modern Angular adapter support for Angular 20–22
- Legacy Angular adapter support for Angular 12–19
- Host-owned client integration path for Angular 9–11
- Packed `.nupkg` and `.tgz` consumer verification across the compatibility matrix
- Unified `0.9.0-preview.4` preview numbering across the NuGet and npm package families

## Planned previews

The compatibility track runs alongside these feature milestones and becomes a
release criterion at `1.0.0-rc.1`.

### `1.0.0-rc.1` — API Freeze and Security Hardening

No major feature is added during the release-candidate phase. Exit criteria
include:

- public API, naming, nullability, cancellation, exception, and thread-safety
  review;
- API approval snapshots, binary-compatibility checks, and migration guidance;
- fail-closed integrity, property-based, and manifest fuzz testing;
- requirement-depth, manifest-size, collection-size, and regex limits;
- YAML parser, dependency, and supply-chain hardening;
- reproducible package builds and sensitive-diagnostics review;
- completed current and legacy package-only consumer matrices;
- published compatibility and support policy.

### `1.0.0` — Stable Release

RuleGate 1.0 is intended to provide:

- RBAC, permission-based authorization, ABAC, CBAC, resource rules, and logical
  composition;
- scalar, string, collection, presence, null, attribute-to-attribute, time, and
  context requirements;
- ASP.NET Core integration and attribute enrichment;
- modern Angular integration and the validated compatibility adapters;
- optional Keycloak adapters without provider coupling;
- YAML manifests, validation, C# and TypeScript generation, policy tests,
  explanations, and linting;
- local policy sources with atomic reload;
- OpenTelemetry integration and documented performance/concurrency behavior;
- official backend, frontend, full-stack, and compatibility samples;
- stable public APIs and security documentation.

## Post-stable product roadmap

The following work begins only after the stable `1.0.0` release is published
and its release verification is closed. These phases intentionally have no
assigned version numbers yet. Each phase will receive its own scoped milestone
before implementation begins.

### Focused reference applications and case studies

The reference-application portfolio will demonstrate RuleGate across modern
and legacy stacks without turning the repository into a collection of large
sample products.

Each application will:

- implement one small, understandable domain case instead of an entire
  business application;
- consume stable RuleGate packages rather than preview or release-candidate
  packages;
- emphasize a detailed, realistic `rulegate.yaml`;
- include only enough backend, frontend, authentication, tests, and generated
  code to explain the authorization case;
- demonstrate that UI framework, identity provider, application generation,
  and domain do not change RuleGate policy semantics.

Planned domains include:

- document approval and EBYS/DYS workflows;
- HBYS and patient-record access;
- student information and grade publication;
- fintech payment or transaction approval;
- B2B partner-specific resource access;
- ERP purchase-order approval;
- e-commerce merchant and order ownership;
- CRM lead and customer ownership, sales hierarchy, region scope, and
  restricted-field updates.

The portfolio will include both modern and legacy consumers, including:

- the existing modern ASP.NET Core and Angular document-approval application;
- an ASP.NET Core MVC 3.1 application;
- a legacy Angular and legacy .NET application;
- current and older compatible PrimeNG generations where appropriate.

UI technology will deliberately vary between applications. Candidate
open-source and freely usable stacks include:

- Bootstrap;
- current and legacy PrimeNG;
- Angular Material;
- Tailwind CSS;
- NG-ZORRO or another established free component library.

Commercial or licensing-sensitive UI packages will not be required by public
samples unless their terms are explicitly suitable for an open-source
reference application.

Authentication and identity approaches will also vary. The complete portfolio
will not depend only on Keycloak. Cases may use:

- Keycloak;
- ASP.NET Core Identity;
- legacy IdentityServer integration;
- OpenIddict or another standards-based OIDC provider;
- an application-owned or custom JWT authentication implementation.

Authentication remains host-owned in every sample. RuleGate receives trusted
subject, resource, action, and context data and remains independent from the
selected identity product.

### Book-style documentation and GitHub Wiki

After the case-study portfolio is available, the documentation will be
reorganized into a beginner-to-advanced learning path.

A reader with no previous RuleGate experience should be able to:

- understand authentication and authorization boundaries;
- install the correct packages;
- write and validate `rulegate.yaml`;
- integrate ASP.NET Core and a supported frontend;
- connect an existing identity system;
- diagnose denied or indeterminate decisions;
- implement custom providers, requirements, evaluators, and adapters;
- apply RuleGate safely in a production application.

Repository Markdown under `docs/` will remain the canonical source of truth.
The same documentation will be published to the GitHub Wiki through a
repeatable synchronization process rather than maintained manually in two
places.

The documentation publishing workflow will:

- generate the Wiki `Home` page and `_Sidebar`;
- preserve usable navigation in both repository Markdown and the Wiki;
- validate local links;
- validate commands and important code examples where practical;
- prevent repository and Wiki documentation from drifting.

The documentation set will include:

- getting started and the first working policy;
- authentication versus authorization;
- RBAC, permission, ABAC, CBAC, resource, and context concepts;
- a complete `rulegate.yaml` guide;
- ASP.NET Core, MVC, Minimal API, modern Angular, and legacy Angular
  integration;
- identity-provider integration patterns;
- recipes for common authorization problems;
- domain case studies based on the reference applications;
- security boundaries and fail-closed behavior;
- troubleshooting and diagnostics;
- migration and versioning;
- advanced extensibility.

Case-study and recipe chapters should normally follow this teaching order:

1. problem;
2. authorization reasoning;
3. `rulegate.yaml`;
4. backend integration;
5. frontend integration when applicable;
6. tests;
7. common mistakes and security considerations.

### Java and PHP package families

After the stable samples and documentation foundation, RuleGate will expand to
native Java and PHP packages.

The ports must preserve:

- the same `rulegate.yaml` schema;
- equivalent authorization outcomes and fail-closed semantics;
- the same subject, resource, action, context, policy, and requirement model;
- provider independence;
- compatibility and package-consumer verification appropriate to each
  ecosystem.

The Java plan includes:

- a framework-independent Java core;
- an idiomatic Spring Boot starter and integration layer;
- native Java extension points;
- publication through Maven Central;
- Java-specific samples and documentation.

The PHP plan includes:

- a framework-independent Composer package;
- optional Laravel and Symfony adapters;
- native PHP extension points;
- publication through Packagist;
- PHP-specific samples and documentation.

Java and PHP implementations must not require an application to call a hidden
.NET service. They are native ecosystem implementations of the shared RuleGate
authorization model.

### React and Vue integrations

After the Java and PHP foundations, the frontend family will expand beyond
Angular.

Both integrations should reuse the framework-independent RuleGate client where
possible while remaining idiomatic to their own frameworks.

The React integration is expected to include:

- provider and context integration;
- authorization hooks;
- conditional-rendering components;
- route and navigation patterns;
- generated policy-constant consumption.

The Vue integration is expected to include:

- an installable Vue plugin;
- composables;
- directives;
- router-guard patterns;
- generated policy-constant consumption.

Angular, React, and Vue integrations remain user-experience projections.
Backend authorization is always the security boundary, and every frontend
adapter must preserve fail-closed client-state behavior.

## Compatibility track

The delivered .NET compatibility model is:

- Abstractions, Core, and Manifest target .NET Standard 2.0 plus .NET 8–10.
- ASP.NET Core and Keycloak integration packages target .NET Core 3.1 and
  every .NET release from 5 through 10.
- The CLI remains on .NET 8–10; generated source is verified independently.
- Packed NuGet consumers build every target and execute inside isolated .NET
  Core 3.1, .NET 5, .NET 6, and .NET 7 runtime containers as well as installed
  .NET 8–10 runtimes.

The delivered frontend compatibility model is:

- `@fotbiler/rulegate-client` owns framework-independent fail-closed state.
- `@fotbiler/rulegate-angular` targets Angular 20–22 with signals, standalone
  APIs, functional guards, and modern directives.
- `@fotbiler/rulegate-angular-legacy` targets Angular 12–19 with observables,
  NgModule, class guards, and classic directives.
- Angular 9–11 use the framework-independent client through a host-owned
  service because those versions predate the stable partial-Ivy library format.
- Packed `.tgz` consumers build real production applications on representative
  Angular 9–22 versions.

Compatibility is accepted only when consumers install packed `.nupkg` or
`.tgz` artifacts and pass the defined build and authorization tests.
Source-only compatibility does not satisfy the roadmap. See the
[frontend compatibility guide](frontend-compatibility.md) for package selection.
The complete current-versus-legacy policy is documented in
[Platform compatibility](platform-compatibility.md).

## Roadmap principles

All roadmap work must preserve these principles:

- Backend authorization remains the security boundary.
- Authorization defaults to deny.
- Missing and malformed policies fail closed.
- Provider-specific integrations remain optional.
- Core authorization remains local-first.
- Public APIs are tested through package-only consumers.
- Diagnostics and telemetry do not expose sensitive inputs by default.
- Generated output is deterministic.
- Legacy compatibility never weakens current-runtime security guarantees.
