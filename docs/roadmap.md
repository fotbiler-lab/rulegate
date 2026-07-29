# RuleGate Roadmap

This document describes the planned development direction of RuleGate.

The roadmap is outcome-oriented. Planned scope may change as public APIs,
security boundaries, package structure, and consumer feedback evolve during
the preview releases.

## Current status

| Capability                                   | Status       |
| -------------------------------------------- | ------------ |
| Authorization engine                         | ✅ Available |
| Permission-based authorization               | ✅ Available |
| Role-based authorization                     | ✅ Available |
| Logical `all`, `any`, and `not` requirements | ✅ Available |
| Subject, resource, and context attributes    | ✅ Available |
| YAML manifest compilation                    | ✅ Available |
| ASP.NET Core integration                     | ✅ Available |
| Dynamic authorization policies               | ✅ Available |
| Minimal API endpoint integration             | ✅ Available |
| Controller and action attributes             | ✅ Available |
| Authorization diagnostics and logging        | ✅ Available |
| HTTP authorization-result mapping            | ✅ Available |
| CLI manifest validation                      | ✅ Available |
| Deterministic C# code generation             | ✅ Available |
| Angular SDK                                  | ✅ Available |
| Deterministic TypeScript generation          | ✅ Available |
| Keycloak helpers                             | ✅ Available |
| OpenTelemetry integration                    | ⏳ Planned   |
| Decision visualization                       | ⏳ Planned   |

The latest preview is `0.5.0-preview.2`. The next product milestone is the
OpenTelemetry integration work in `0.6.0-preview.1`.

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

- Manifest-derived C# policy constants
- Manifest-derived resource-type and action constants
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

- Realm-role mapping
- Client-role mapping
- Composite-role helpers
- Claim normalization
- RuleGate subject creation
- Provider-specific integration documentation
- Optional Angular secondary entrypoint
- Package-only npm and NuGet consumer verification
- [Keycloak integration guide](keycloak.md)

### `0.5.0-preview.2` — NuGet Version Alignment

- One synchronized version for all RuleGate NuGet packages
- Aligned package-to-package dependency versions
- Complete six-package NuGet publishing and verification
- Standardized NuGet package README product naming

## Later milestones

### `0.6.0-preview.1` — OpenTelemetry

- Authorization metrics
- Authorization activities and traces
- Low-cardinality telemetry conventions
- Failure and latency instrumentation
- OpenTelemetry registration helpers

### `0.6.0-preview.2` — Decision Explanation

- Higher-level decision explanations
- Safe authorization-tree representation
- Diagnostic export models
- Decision visualization foundations

## Roadmap principles

All roadmap work must preserve these principles:

- Backend authorization remains the security boundary.
- Authorization defaults to deny.
- Missing and malformed policies fail closed.
- Provider-specific integrations remain optional.
- Core authorization remains local-first.
- Public APIs are tested through package-only consumers.
- Diagnostics do not expose sensitive authorization inputs by default.
- Generated output is deterministic.
