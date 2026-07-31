<p align="center">
  <img src="docs/assets/rulegate-logo.svg" alt="RuleGate" width="520">
</p>

<p align="center">
  <strong>Local-first, provider-independent authorization for .NET and Angular</strong>
</p>

<p align="center">
  Role-based authorization · Attribute-based authorization · Context-based authorization · Resource-based authorization · ASP.NET Core · Angular · YAML manifests · RuleGate CLI
</p>

<p align="center">
  <a href="https://github.com/fotbiler-lab/rulegate/actions/workflows/ci.yml"><img alt="CI" src="https://github.com/fotbiler-lab/rulegate/actions/workflows/ci.yml/badge.svg?branch=main"></a>
  <a href="https://www.nuget.org/packages/Fotbiler.RuleGate.AspNetCore"><img alt="NuGet" src="https://img.shields.io/nuget/vpre/Fotbiler.RuleGate.AspNetCore?logo=nuget&amp;label=NuGet"></a>
  <a href="https://www.nuget.org/packages/Fotbiler.RuleGate.AspNetCore"><img alt="NuGet downloads" src="https://img.shields.io/nuget/dt/Fotbiler.RuleGate.AspNetCore?logo=nuget&amp;label=downloads"></a>
  <a href="https://www.npmjs.com/package/@fotbiler/rulegate-angular"><img alt="npm" src="https://img.shields.io/npm/v/%40fotbiler%2Frulegate-angular?logo=npm&amp;label=npm"></a>
  <a href="https://www.npmjs.com/package/@fotbiler/rulegate-angular"><img alt="npm downloads" src="https://img.shields.io/npm/dm/%40fotbiler%2Frulegate-angular?logo=npm&amp;label=downloads"></a>
  <a href="https://dotnet.microsoft.com/"><img alt=".NET 8, 9, and 10" src="https://img.shields.io/badge/.NET-8%20%7C%209%20%7C%2010-512BD4?logo=dotnet"></a>
  <a href="https://angular.dev/"><img alt="Angular 22" src="https://img.shields.io/badge/Angular-22-DD0031?logo=angular&amp;logoColor=white"></a>
  <a href="LICENSE"><img alt="License" src="https://img.shields.io/github/license/fotbiler-lab/rulegate?label=license"></a>
</p>

<p align="center">
  <a href="docs/getting-started.md">Getting started</a>
  ·
  <a href="#packages">Packages</a>
  ·
  <a href="#reference-applications">Samples</a>
  ·
  <a href="docs/roadmap.md">Roadmap</a>
  ·
  <a href="#security-behavior">Security</a>
  ·
  <a href="https://github.com/fotbiler-lab/rulegate/releases">Releases</a>
</p>

> [!WARNING]
> RuleGate is currently in preview. Public APIs may change before the first
> stable release, and the packages are not yet recommended for production use.

## Authorization, not authentication

RuleGate does not authenticate users, issue tokens, or manage identities. The
host application validates an identity and supplies trusted subject, resource,
and request-context data. RuleGate then decides whether that subject may
perform an action on a resource.

```mermaid
flowchart LR
    IdP[Identity provider] -->|Validated identity| Host[Host application]
    Data[Application data] -->|Trusted attributes| Host
    Host --> Subject[Subject]
    Host --> Resource[Resource]
    Host --> Context[Context]
    Subject --> Engine[RuleGate policy engine]
    Resource --> Engine
    Context --> Engine
    Engine --> Decision[Allow or deny]
    Decision --> API[ASP.NET Core enforcement]
    Host -. Frontend authorization snapshot .-> UI[Angular UX projection]
```

## Why RuleGate?

RuleGate provides a unified authorization model for applications that need
more than framework-level roles or ad hoc permission checks.

- **Local-first:** Policy evaluation happens inside the application process.
- **Provider-independent:** The authorization engine is not coupled to an
  identity provider, database, or remote policy service.
- **Policy-driven:** Permissions, roles, attributes, resources, and contextual
  rules use one composable requirement model.
- **Fail-closed:** Missing, malformed, unsupported, and indeterminate
  authorization inputs deny access.
- **Framework-ready:** ASP.NET Core applications can use dynamic policies,
  endpoint helpers, controller attributes, ordered attribute enrichment,
  diagnostics, and safe HTTP result mapping.
- **Manifest-enabled:** YAML policies can be loaded, validated, and compiled
  from files, embedded resources, configuration, or application-defined
  sources into atomically replaceable immutable runtime snapshots.
- **CLI-ready:** manifests can be validated and linted, deterministic fixtures
  can be tested and explained safely, and C# policy, resource-type, and action
  constants can be generated locally or in CI with stable process exit codes.
- **Angular-ready:** a fail-closed frontend projection, declarative route
  authorization, template composition, disabled state, and generated
  TypeScript identifiers keep frontend behavior aligned with RuleGate.
- **Provider integrations:** optional adapters normalize provider claims
  without coupling the RuleGate engine or primary Angular entrypoint to an
  identity provider.

One policy can combine several authorization styles without moving evaluation
to a remote service:

| Style                          | Typical RuleGate input                                   |
| ------------------------------ | -------------------------------------------------------- |
| Permission-based authorization | Exact application permissions such as `document.update`  |
| Role-based authorization       | Effective roles such as `finance.approver`               |
| Attribute-based authorization  | Department, clearance, classification, or workflow state |
| Context-based authorization    | Time, network zone, request channel, or trusted device   |
| Resource-based authorization   | Ownership, organization scope, state, or resource value  |

## Which package should I install?

| Need                                                      | Start with                       |
| --------------------------------------------------------- | -------------------------------- |
| ASP.NET Core application                                  | `Fotbiler.RuleGate.AspNetCore`   |
| Framework-independent authorization engine                | `Fotbiler.RuleGate.Core`         |
| Custom contracts or evaluators                            | `Fotbiler.RuleGate.Abstractions` |
| YAML policy loading and compilation                       | `Fotbiler.RuleGate.Manifest`     |
| Keycloak claim normalization                              | `Fotbiler.RuleGate.Keycloak`     |
| Validation, testing, explanation, linting, and generation | `Fotbiler.RuleGate.Cli`          |
| Angular route and template projection                     | `@fotbiler/rulegate-angular`     |

## Packages

| Package                                                                                           | Purpose                                                                                                                           |
| ------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------- |
| [`Fotbiler.RuleGate.Abstractions`](https://www.nuget.org/packages/Fotbiler.RuleGate.Abstractions) | Authorization contracts, policy definitions, source and reload contracts, requests, decisions, and evaluation abstractions        |
| [`Fotbiler.RuleGate.Core`](https://www.nuget.org/packages/Fotbiler.RuleGate.Core)                 | Policy engine, built-in requirement evaluators, in-memory sources, and atomic immutable snapshots                                 |
| [`Fotbiler.RuleGate.Manifest`](https://www.nuget.org/packages/Fotbiler.RuleGate.Manifest)         | YAML loading, validation, compilation, file sources, embedded-resource sources, and domain mapping                                |
| [`Fotbiler.RuleGate.AspNetCore`](https://www.nuget.org/packages/Fotbiler.RuleGate.AspNetCore)     | ASP.NET Core integration, configuration policy sources, atomic reload hosting, enrichment, dynamic policies, and endpoint helpers |
| [`Fotbiler.RuleGate.Cli`](https://www.nuget.org/packages/Fotbiler.RuleGate.Cli)                   | .NET tool for validation, testing, redacted explanations, linting, deterministic C# generation, stale-output checks, and CI       |
| [`Fotbiler.RuleGate.Keycloak`](https://www.nuget.org/packages/Fotbiler.RuleGate.Keycloak)         | Optional Keycloak claim normalization and RuleGate subject mapping                                                                |
| [`@fotbiler/rulegate-angular`](https://www.npmjs.com/package/@fotbiler/rulegate-angular)          | Angular 22 authorization client, declarative route guards, UI directives, and TypeScript identifier generation                    |

## Supported .NET and Angular versions

| Package family          | Current preview   | Supported platform                                        | Distribution |
| ----------------------- | ----------------- | --------------------------------------------------------- | ------------ |
| RuleGate NuGet packages | `0.9.0-preview.2` | .NET 8 (`net8.0`), .NET 9 (`net9.0`), .NET 10 (`net10.0`) | NuGet        |
| RuleGate Angular SDK    | `0.7.0-preview.1` | Angular 22                                                | npm          |

Every RuleGate NuGet package includes framework-specific assemblies for all
three .NET targets. The source, test suites, package assets, and package-only
consumers are verified across each supported platform.

## Current capabilities

RuleGate currently provides:

- Permission and role-based authorization
- Subject, resource, and context attribute requirements
- Advanced string, collection, presence, and null attribute operators
- Attribute-to-attribute comparisons for ownership and organization scope
- Explicit-time-zone workday and overnight time-window requirements
- Before, after, and bounded date-time requirements
- Authentication-age, MFA-age, and canonical trusted-context requirements
- Composable `all`, `any`, and `not` requirements
- Default-deny and fail-closed evaluation
- Ordinal string matching with explicit case-insensitive opt-in
- Homogeneous attribute collections with a 256-element safety limit
- Immutable in-memory policy registration
- YAML manifest loading, validation, and compilation
- In-memory, YAML file, embedded-resource, configuration, and
  application-defined policy sources
- Complete-source validation, immutable snapshots, and atomic policy reload
- Last-valid-snapshot preservation with deterministic reload diagnostics
- ASP.NET Core dependency injection and dynamic policies
- Minimal API and controller authorization
- Configurable claims-to-subject mapping
- Ordered asynchronous subject, resource, and context attribute enrichment
- Explicit fail, keep-existing, and replace-existing collision behavior
- Resource-based authorization
- Opt-in safe HTTP authorization results
- Opt-in structured authorization diagnostics
- Deterministic CLI manifest validation with text and JSON output
- Host-independent policy fixtures with allow, deny, indeterminate, and
  failure-code expectations
- Redacted structural decision explanations using the runtime evaluator path
- Deterministic manifest linting with stable rule codes and CI exit behavior
- Manifest-derived C# policy, resource-type, and action constants
- Atomic generated-file writes and byte-exact stale-output checks
- Generated-code compilation smoke coverage on .NET 8, .NET 9, and .NET 10
- Signal-backed Angular frontend authorization state
- Angular permission, policy, and role route guards
- Declarative Angular route metadata and denied-navigation handling
- Angular visibility, fallback-template, and disabled-state directives
- Manifest-derived TypeScript policy, permission, role, resource-type, and action
  constants
- Optional Keycloak realm-role and selected client-role normalization for
  ASP.NET Core and Angular
- Package-only Angular npm tarball verification
- Package-consuming minimal ASP.NET Core and full-stack Angular reference
  applications
- .NET 8, .NET 9, and .NET 10 packages

See the [roadmap](docs/roadmap.md) for published milestones and upcoming
previews.

## Documentation

| Goal                                             | Guide                                                    |
| ------------------------------------------------ | -------------------------------------------------------- |
| Make the first authorization decision            | [Getting started](docs/getting-started.md)               |
| Understand the authorization model               | [Authorization model](docs/authorization-model.md)       |
| Define `rulegate.yaml` policies                  | [Manifest guide](docs/manifests.md)                      |
| Protect ASP.NET Core applications                | [ASP.NET Core integration](docs/aspnetcore.md)           |
| Supply trusted authorization attributes          | [ASP.NET Core enrichment](docs/enrichment.md)            |
| Configure logs and diagnostic sinks              | [Diagnostics](docs/diagnostics.md)                       |
| Review trust boundaries and production controls  | [Security model](docs/security.md)                       |
| Use the RuleGate command-line tool               | [CLI guide](docs/cli.md)                                 |
| Test policies without starting an application    | [Policy testing](docs/policy-testing.md)                 |
| Explain decisions and lint policy structure      | [Explain and Lint](docs/explain-and-lint.md)             |
| Load and atomically reload local policy sources  | [Policy sources](docs/policy-sources.md)                 |
| Generate deterministic C# constants              | [C# code generation](docs/code-generation.md)            |
| Add frontend permission, policy, and role checks | [Angular SDK](docs/angular.md)                           |
| Map Keycloak roles without coupling the engine   | [Keycloak integration](docs/keycloak.md)                 |
| Run package-consuming reference applications     | [Reference applications](docs/reference-applications.md) |
| Browse all documentation                         | [Documentation index](docs/README.md)                    |
| Review current and planned capabilities          | [Roadmap](docs/roadmap.md)                               |

## Installation

Install the ASP.NET Core package:

```bash
dotnet add package Fotbiler.RuleGate.AspNetCore --version 0.9.0-preview.2
```

`Fotbiler.RuleGate.AspNetCore` references the core engine, abstractions, and
manifest packages.

Applications using only RuleGate contracts may reference the abstractions
package directly:

```bash
dotnet add package Fotbiler.RuleGate.Abstractions --version 0.9.0-preview.2
```

Install the Angular SDK:

```bash
pnpm add @fotbiler/rulegate-angular@0.7.0-preview.1
```

Install the optional Keycloak integration when Keycloak supplies the identity:

```bash
dotnet add package Fotbiler.RuleGate.Keycloak --version 0.9.0-preview.2
```

## Use the RuleGate CLI

Install the RuleGate command-line tool:

```bash
dotnet tool install \
  --global \
  Fotbiler.RuleGate.Cli \
  --version 0.9.0-preview.2
```

Validate the default `rulegate.yaml` in the current directory:

```bash
rulegate validate
```

Validate an explicit manifest or request pure JSON output:

```bash
rulegate validate ./policies/rulegate.yaml
rulegate validate --format json
```

The `0.9.0-preview.2` tool evaluates and safely explains deterministic
authorization requests against a manifest without starting the host
application:

```bash
rulegate test ./policies/authorization.tests.yaml
rulegate test ./policies/authorization.tests.yaml --format json
```

Explain one fixture decision without exposing request values, then lint the
manifest for structural risks:

```bash
rulegate explain \
  ./policies/authorization.tests.yaml \
  --test confidential-document-denied

rulegate lint ./policies/rulegate.yaml --format json
```

Use `rulegate --help`, `rulegate --version`, and `rulegate info` to inspect the
installed tool.

The installed tool also provides deterministic C# generation:

```bash
rulegate generate csharp \
  ./rulegate.yaml \
  --namespace Sample.Authorization \
  --output Generated/RuleGate.g.cs
```

Verify in CI that the committed generated file is current without modifying it:

```bash
rulegate generate csharp \
  ./rulegate.yaml \
  --namespace Sample.Authorization \
  --output Generated/RuleGate.g.cs \
  --check
```

See the [RuleGate CLI guide](docs/cli.md) for the command reference, the
[policy-testing guide](docs/policy-testing.md) for deterministic fixtures, and
the [C# code-generation guide](docs/code-generation.md) for generated output,
stale checks, identifier rules, and CI usage.

## Quick start

The following example compiles a manifest, registers RuleGate, and evaluates
an authorization request.

### 1. Define a policy

Create `rulegate.yaml`:

```yaml
schemaVersion: 1

application:
  id: sample-application
  name: Sample Application

policies:
  - id: sample-resource-read
    resourceType: sample-resource
    action: read
    requirement:
      all:
        - id: required-permission
          permission: sample.read

        - id: accepted-role
          any:
            - role: sample.editor
            - role: sample.administrator
```

Even a small real-world rule can combine a permission, resource ownership,
workflow state, and trusted request context:

```yaml
- id: document-update
  resourceType: document
  action: update
  requirement:
    all:
      - permission: document.update
      - attributeComparison:
          left:
            source: resource
            name: ownerId
          operator: equal
          right:
            source: subject
            name: id
      - attribute:
          source: resource
          name: status
          operator: in
          valueType: stringCollection
          value: [draft, returned]
      - context:
          property: trustedDevice
          operator: equal
          valueType: boolean
          value: true
```

The host must derive `ownerId`, `status`, and `trustedDevice` from trusted
application sources. Missing or incompatible input denies access.

### 2. Validate the complete manifest

```bash
rulegate validate rulegate.yaml
rulegate lint rulegate.yaml
```

A failed validation returns no partial policy collection.

The [manifest guide](docs/manifests.md) documents structured load and
validation errors.

### 3. Register RuleGate

```csharp
using Fotbiler.RuleGate.AspNetCore.DependencyInjection;

builder.Services
    .AddRuleGate()
    .AddYamlPolicyFile(
        "rulegate.yaml",
        options =>
            options.ReloadOnChange = true);
```

RuleGate activates the complete manifest as one immutable snapshot. A failed
reload preserves the last valid snapshot. See
[Policy sources and atomic reload](docs/policy-sources.md) for in-memory,
embedded-resource, configuration, and application-defined sources.

### 4. Evaluate a request

```csharp
using Fotbiler.RuleGate.Abstractions.Authorization;
using Fotbiler.RuleGate.Abstractions.Evaluation;

var engine =
    app.Services.GetRequiredService<
        IAuthorizationEngine>();

var request =
    new AuthorizationRequest(
        new AuthorizationSubject(
            "user-42",
            roles:
            [
                "sample.editor"
            ],
            permissions:
            [
                "sample.read"
            ]),

        new AuthorizationResource(
            "sample-resource",
            "resource-42"),

        "read",

        new AuthorizationContext(
            DateTimeOffset.UtcNow));

var decision =
    await engine.EvaluateAsync(
        request);

if (!decision.IsAllowed)
{
    return Results.Forbid();
}
```

RuleGate grants access only when a matching policy exists and the root
requirement is satisfied.

## Protect ASP.NET Core endpoints

After configuring ASP.NET Core authentication and authorization, protect a
Minimal API endpoint:

```csharp
using Fotbiler.RuleGate.AspNetCore.Endpoints;

app.MapGet(
        "/sample-resources/{id}",
        (string id) =>
        {
            return Results.Ok(
                new
                {
                    id,
                });
        })
    .RequireRuleGate(
        resourceType: "sample-resource",
        action: "read",
        resourceIdRouteValue: "id");
```

Protect a controller or action:

```csharp
using Fotbiler.RuleGate.AspNetCore.Authorization;

[RuleGateAuthorize(
    "sample-resource",
    "read",
    "id")]
public IActionResult Get(
    string id)
{
    return Ok(
        new
        {
            id,
        });
}
```

Dynamic policy names use:

```text
RuleGate:<resource-type>:<action>
```

The [ASP.NET Core integration guide](docs/aspnetcore.md) covers authentication,
claims mapping, domain resources, imperative authorization, endpoint metadata,
controllers, diagnostics, and HTTP result mapping. The dedicated
[enrichment guide](docs/enrichment.md) covers ordered trusted subject,
resource, and context attribute providers.

## Reference applications

The repository includes a
[minimal ASP.NET Core sample](samples/aspnetcore-minimal/README.md) and a
[full-stack document-approval sample](samples/document-approval/README.md).
The full-stack sample is also the Angular reference and combines PrimeNG,
Keycloak, SQLite, generated identifiers, guards, directives, and backend
resource authorization without source-project shortcuts.

The document-approval sample requires an accessible Keycloak instance, the
[documented realm and client configuration](samples/document-approval/keycloak/README.md),
and a [local PrimeUI license](samples/document-approval/README.md#prerequisites).
Its checked-in Docker Compose file builds the API and web application only; it
does not provision Keycloak, import a realm, create test users, or supply a UI
license.

## Security behavior

RuleGate defaults to deny.

Missing policies, failed requirements, unsupported values, ambiguous identity,
and indeterminate evaluations cannot grant access.

The protected backend operation remains the security boundary. Subject,
resource, and context data must be derived from trusted application sources.
Angular guards and template directives improve user experience only and cannot
protect an API.

Read the [security model](docs/security.md) before production integration.

## Project status

The latest RuleGate NuGet preview is
[`0.9.0-preview.2`](https://github.com/fotbiler-lab/rulegate/releases/tag/v0.9.0-preview.2).
It adds composable policy sources, immutable combined snapshots, and safe
atomic reload while preserving the last valid policy set on failure.

All NuGet packages are `0.9.0-preview.2`. The independently versioned Angular
npm package remains at `0.7.0-preview.1`.

See the [roadmap](docs/roadmap.md) for the complete release path.

## Community

- [Contributing](CONTRIBUTING.md)
- [Support](SUPPORT.md)
- [Security policy](SECURITY.md)
- [Code of Conduct](CODE_OF_CONDUCT.md)

## License

RuleGate is licensed under the
[Apache License 2.0](LICENSE).
