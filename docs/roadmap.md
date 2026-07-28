# Fotbiler RuleGate Roadmap

This document describes the planned development direction of Fotbiler RuleGate.

The roadmap is outcome-oriented. Planned scope may change as public APIs,
security boundaries, package structure, and consumer feedback evolve during
the preview releases.

## Current status

| Capability | Status |
|---|---|
| Authorization engine | ✅ Available |
| Permission-based authorization | ✅ Available |
| Role-based authorization | ✅ Available |
| Logical `all`, `any`, and `not` requirements | ✅ Available |
| Subject, resource, and context attributes | ✅ Available |
| YAML manifest compilation | ✅ Available |
| ASP.NET Core integration | ✅ Available |
| Dynamic authorization policies | ✅ Available |
| Minimal API endpoint integration | ✅ Available |
| Controller and action attributes | ✅ Available |
| Authorization diagnostics and logging | ✅ Available |
| HTTP authorization-result mapping | ✅ Available |
| CLI manifest validation | ✅ Published in `0.3.0-preview.1` |
| Code generation | ⏳ Planned |
| Angular SDK | ⏳ Planned |
| Keycloak helpers | ⏳ Planned |
| OpenTelemetry integration | ⏳ Planned |
| Decision visualization | ⏳ Planned |

## Published previews

### `0.1.0-preview.1`

Authorization core foundation:

- Public authorization contracts
- Policy and requirement definitions
- Permission and role requirements
- Logical requirements
- Default-deny and fail-closed evaluation
- In-memory policy provider
- YAML manifest foundation

### `0.2.0-preview.1`

ASP.NET Core integration foundation:

- Dependency injection registration
- `ClaimsPrincipal` subject mapping
- Resource-based authorization
- Dynamic RuleGate policy names
- Minimal API integration
- Controller and action authorization
- Package-only consumer verification

### `0.2.0-preview.2`

Advanced authorization and diagnostics:

- Subject, resource, and context attribute requirements
- Typed scalar comparison
- Nested manifest attribute requirements
- Authorization diagnostics contracts
- Structured ASP.NET Core logging
- Generic RuleGate HTTP `401` and `403` problem responses
- Multi-targeting for .NET 8, .NET 9, and .NET 10

## Latest published preview

### `0.3.0-preview.1` — CLI and Manifest Validation

This preview introduces the first RuleGate command-line experience and is published on NuGet.org.

Its primary outcome is a deterministic manifest-validation command that can be
used locally, in CI pipelines, and by repository tooling without starting an
application.

#### Delivered scope

- New `Fotbiler.RuleGate.Cli` project
- Distribution as a .NET tool
- `rulegate validate` command
- Automatic discovery of `rulegate.yaml`
- Explicit manifest file path support
- Reuse of the existing manifest compiler and validator
- Human-readable terminal output
- Machine-readable JSON output
- Stable process exit codes
- Separate reporting for file-loading, YAML, and validation errors
- Fail-closed behavior
- Package-only CLI installation and execution smoke tests
- CLI usage documentation
- Release verification for the CLI package

#### Command surface

```bash
rulegate validate
rulegate validate ./policies/rulegate.yaml
rulegate validate --format json
```

#### Exit codes

| Exit code | Meaning |
|---:|---|
| `0` | The manifest is valid |
| `1` | The manifest is invalid |
| `2` | The command, input file, or environment is invalid |
| `3` | An unexpected internal failure occurred |

#### Explicit non-goals

The following items are intentionally excluded from `0.3.0-preview.1`:

- C# code generation
- TypeScript code generation
- Angular SDK
- npm publishing
- Keycloak integration
- OpenTelemetry integration
- Watch mode
- IDE extensions
- Graphical decision visualization

## Current development milestone

### `0.3.0-preview.2` — Code Generation

Implemented on the current `main` development line and pending release
preparation:

- Manifest-derived C# policy constants
- Manifest-derived resource-type and action constants
- Deterministic UTF-8, LF-only generated output
- Atomic generated-file replacement
- Byte-exact stale-output detection through `--check`
- `rulegate generate csharp`
- Identifier, namespace, and collision diagnostics
- Generated-code compilation and execution smoke tests on .NET 8, .NET 9, and
  .NET 10
- Normal CI and release-verification integration

## Planned milestone queue

### `0.3.0-preview.3` — Developer Experience

- Domain resource mapping helpers
- Subject attribute extraction helpers
- Resource attribute extraction helpers
- Context attribute extraction helpers
- Higher-level authorization result APIs
- Clearer decision explanation models

### `0.4.0-preview.1` — Angular SDK Foundation

- Angular authorization client models
- Permission and policy guards
- Structural authorization directive
- Generated policy constant consumption
- Initial `@fotbiler` npm package

### `0.4.0-preview.2` — Angular Developer Experience

- Route integration
- Template authorization helpers
- Framework-aware result handling
- Frontend and backend policy identifier alignment
- Angular examples and consumer tests

### `0.5.0-preview.1` — Keycloak Helpers

- Realm-role mapping
- Client-role mapping
- Composite-role helpers
- Claim normalization
- RuleGate subject creation
- Provider-specific integration documentation

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
