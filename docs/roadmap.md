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
| Policy Testing CLI                           | ⏳ Planned   |
| Explain and Lint                             | ⏳ Planned   |
| Policy Sources and Atomic Reload             | ⏳ Planned   |
| OpenTelemetry, Benchmarks, and Concurrency   | ⏳ Planned   |
| .NET and Angular Compatibility Track         | ⏳ Planned   |
| API Freeze and Security Hardening            | ⏳ Planned   |
| Stable Release                               | ⏳ Planned   |

The latest RuleGate NuGet preview is `0.7.0-preview.2`. The independently
versioned Angular npm package remains at `0.7.0-preview.1`. Official Reference
Applications are available in the repository. The next feature milestone is
the Policy Testing CLI in `0.8.0-preview.2`.

All NuGet packages share one version and are published together for every
NuGet release, including packages without code changes. npm packages are
versioned independently from NuGet and remain aligned within the npm package
family.

## Current platform support

The packages currently published and verified support:

| Package family       | Current tested platform     |
| -------------------- | --------------------------- |
| RuleGate NuGet       | .NET 8, .NET 9, and .NET 10 |
| RuleGate Angular SDK | Angular 22                  |

The wider .NET Core 3.1+ and Angular 9+ matrix in the compatibility track is a
1.0 goal, not a claim about the current packages. A platform becomes
`legacy-tested` only after a package-only consumer installs the real release
artifact and passes the defined build and authorization tests.

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

## Available in the repository

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

## Planned previews

The compatibility track runs alongside these feature milestones and becomes a
release criterion at `1.0.0-rc.1`.

### `0.8.0-preview.2` — Policy Testing CLI

- `rulegate test [authorization.tests.yaml]`
- Human-readable and JSON output
- Allow, deny, and indeterminate expectations
- Expected failure codes, filtering, summaries, and stable CI exit codes
- Manifest and test-fixture validation
- Deterministic execution without starting the host application

### `0.9.0-preview.1` — Explain and Lint

- `rulegate explain` for safe structural decision explanations
- Redaction of attribute values and identity-specific data by default
- `rulegate lint` for duplicate, contradictory, unreachable, overly deep, or
  unnecessarily complex requirements
- Detection of unused definitions, identifier collisions, and risky operator
  configurations
- Stable human-readable and machine-readable output

### `0.9.0-preview.2` — Policy Sources and Atomic Reload

- In-memory, YAML file, embedded-resource, configuration, and
  application-defined policy sources
- Complete parse and validation before activation
- Immutable policy snapshots and atomic replacement
- Preservation of the last valid snapshot when a reload fails
- Deterministic reload diagnostics and concurrency tests

### `0.9.0-preview.3` — OpenTelemetry, Benchmarks, and Concurrency

- Low-cardinality authorization metrics, activities, and traces
- Decision, failure, latency, policy-load, and reload instrumentation
- No subject IDs, resource IDs, raw roles, permissions, claims, or attribute
  values in telemetry by default
- Benchmarks for scalar, collection, attribute-to-attribute, logical, time, and
  context requirements
- Policy lookup benchmarks at representative policy-set sizes
- Immutable-snapshot, parallel-evaluation, cancellation, provider, and reload
  race-condition tests
- Long-running stress tests and documented thread-safety contracts

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

## Compatibility track

Compatibility work proceeds in parallel so it does not hide or delay feature
scope until the release-candidate gate.

### .NET goals

- Evaluate `netstandard2.0` for abstractions and the provider-independent core.
- Validate manifest-package requirements separately where dependencies or APIs
  prevent the same target set.
- Multi-target ASP.NET Core integration from ASP.NET Core 3.1 through current
  supported versions where a secure implementation is maintainable.
- Keep the CLI on modern .NET when necessary; generated source must remain
  consumable by validated legacy applications.
- Add package-only consumers for .NET Core 3.1, .NET 5, .NET 6, .NET 7, current
  LTS, and current STS releases.

### Frontend goals

- Extract a framework-independent `@fotbiler/rulegate-client` TypeScript core.
- Keep `@fotbiler/rulegate-angular` focused on the current Angular Package
  Format, standalone APIs, signals, functional guards, and modern directives.
- Provide a separately maintained legacy Angular adapter when required for
  NgModule, observable, class-based guard, and classic directive patterns.
- Verify representative package consumers for Angular 9–11, 12–15, 16–19,
  and 20+ rather than assuming one modern package works unchanged everywhere.

Compatibility is accepted only when consumers install the packed `.nupkg` or
`.tgz`, build production code, and run authorization smoke tests. Source-only
compatibility does not satisfy the roadmap.

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
