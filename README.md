<p align="center">
  <img src="docs/assets/rulegate-logo.svg" alt="RuleGate" width="520">
</p>

<p align="center">
  <strong>Local-first, provider-independent authorization for .NET and Angular</strong>
</p>

<p align="center">
  RBAC · ABAC · CBAC · Resource-based authorization · ASP.NET Core · Angular · YAML manifests · RuleGate CLI
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
  <a href="docs/roadmap.md">Roadmap</a>
  ·
  <a href="#security-behavior">Security</a>
  ·
  <a href="https://github.com/fotbiler-lab/rulegate/releases">Releases</a>
</p>

> [!WARNING]
> RuleGate is currently in preview. Public APIs may change before the first
> stable release, and the packages are not yet recommended for production use.

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
  endpoint helpers, controller attributes, diagnostics, and safe HTTP result
  mapping.
- **Manifest-enabled:** YAML policies can be loaded, validated, and compiled
  into immutable runtime policy definitions.
- **CLI-ready:** manifests can be validated and converted into deterministic C#
  policy, resource-type, and action constants locally or in CI, with stable
  process exit codes and stale-output detection.
- **Angular-ready:** a fail-closed frontend projection, declarative route
  authorization, template composition, disabled state, and generated
  TypeScript identifiers keep frontend behavior aligned with RuleGate.
- **Provider integrations:** optional adapters normalize provider claims
  without coupling the RuleGate engine or primary Angular entrypoint to an
  identity provider.

## Packages

| Package                                                                                           | Purpose                                                                                                                                           |
| ------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------- |
| [`Fotbiler.RuleGate.Abstractions`](https://www.nuget.org/packages/Fotbiler.RuleGate.Abstractions) | Authorization contracts, policy definitions, requests, decisions, and evaluation abstractions                                                     |
| [`Fotbiler.RuleGate.Core`](https://www.nuget.org/packages/Fotbiler.RuleGate.Core)                 | Policy engine, built-in requirement evaluators, dispatcher, and in-memory policy provider                                                         |
| [`Fotbiler.RuleGate.Manifest`](https://www.nuget.org/packages/Fotbiler.RuleGate.Manifest)         | YAML manifest loading, validation, compilation, and domain mapping                                                                                |
| [`Fotbiler.RuleGate.AspNetCore`](https://www.nuget.org/packages/Fotbiler.RuleGate.AspNetCore)     | ASP.NET Core dependency injection, claims mapping, dynamic policies, endpoint helpers, authorization attributes, and resource-based authorization |
| [`Fotbiler.RuleGate.Cli`](https://www.nuget.org/packages/Fotbiler.RuleGate.Cli)                   | .NET tool for manifest validation, deterministic C# constant generation, stale-output checks, stable exit codes, and CI usage                     |
| [`Fotbiler.RuleGate.Keycloak`](https://www.nuget.org/packages/Fotbiler.RuleGate.Keycloak)         | Optional Keycloak claim normalization and RuleGate subject mapping                                                                                |
| [`@fotbiler/rulegate-angular`](https://www.npmjs.com/package/@fotbiler/rulegate-angular)          | Angular 22 authorization client, declarative route guards, UI directives, and TypeScript identifier generation                                    |

## Supported .NET and Angular versions

| Package family          | Current preview   | Supported platform                                        | Distribution |
| ----------------------- | ----------------- | --------------------------------------------------------- | ------------ |
| RuleGate NuGet packages | `0.6.0-preview.2` | .NET 8 (`net8.0`), .NET 9 (`net9.0`), .NET 10 (`net10.0`) | NuGet        |
| RuleGate Angular SDK    | `0.5.0-preview.1` | Angular 22                                                | npm          |

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
- ASP.NET Core dependency injection and dynamic policies
- Minimal API and controller authorization
- Configurable claims-to-subject mapping
- Resource-based authorization
- Opt-in safe HTTP authorization results
- Opt-in structured authorization diagnostics
- Deterministic CLI manifest validation with text and JSON output
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
- .NET 8, .NET 9, and .NET 10 packages

See the [roadmap](docs/roadmap.md) for published milestones and upcoming
previews.

## Documentation

| Goal                                             | Guide                                              |
| ------------------------------------------------ | -------------------------------------------------- |
| Make the first authorization decision            | [Getting started](docs/getting-started.md)         |
| Understand the authorization model               | [Authorization model](docs/authorization-model.md) |
| Define `rulegate.yaml` policies                  | [Manifest guide](docs/manifests.md)                |
| Protect ASP.NET Core applications                | [ASP.NET Core integration](docs/aspnetcore.md)     |
| Configure logs and diagnostic sinks              | [Diagnostics](docs/diagnostics.md)                 |
| Review trust boundaries and production controls  | [Security model](docs/security.md)                 |
| Use the RuleGate command-line tool               | [CLI guide](docs/cli.md)                           |
| Generate deterministic C# constants              | [C# code generation](docs/code-generation.md)      |
| Add frontend permission, policy, and role checks | [Angular SDK](docs/angular.md)                     |
| Map Keycloak roles without coupling the engine   | [Keycloak integration](docs/keycloak.md)           |
| Browse all documentation                         | [Documentation index](docs/README.md)              |
| Review current and planned capabilities          | [Roadmap](docs/roadmap.md)                         |

## Installation

Install the ASP.NET Core and manifest packages:

```bash
dotnet add package Fotbiler.RuleGate.AspNetCore --version 0.6.0-preview.2
dotnet add package Fotbiler.RuleGate.Manifest --version 0.6.0-preview.2
```

`Fotbiler.RuleGate.AspNetCore` references the core engine and abstractions
packages.

Applications using only RuleGate contracts may reference the abstractions
package directly:

```bash
dotnet add package Fotbiler.RuleGate.Abstractions --version 0.6.0-preview.2
```

Install the Angular SDK:

```bash
pnpm add @fotbiler/rulegate-angular@0.5.0-preview.1
```

Install the optional Keycloak integration when Keycloak supplies the identity:

```bash
dotnet add package Fotbiler.RuleGate.Keycloak --version 0.6.0-preview.2
```

## Use the RuleGate CLI

Install the RuleGate command-line tool:

```bash
dotnet tool install \
  --global \
  Fotbiler.RuleGate.Cli \
  --version 0.6.0-preview.2
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

Use `rulegate --help`, `rulegate --version`, and `rulegate info` to inspect the
installed tool.

The installed `0.6.0-preview.2` tool provides deterministic C# generation:

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

See the [RuleGate CLI guide](docs/cli.md) for validation and command reference,
and the [C# code-generation guide](docs/code-generation.md) for generated
output, stale checks, identifier rules, and CI usage.

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

### 2. Compile the complete manifest

```csharp
using Fotbiler.RuleGate.Manifest.Compilation;

var compiler =
    new RuleGateManifestCompiler();

var compilation =
    await compiler.CompileFromFileAsync(
        "rulegate.yaml");

if (!compilation.IsSuccess)
{
    throw new InvalidOperationException(
        "RuleGate manifest compilation failed.");
}
```

A failed compilation returns no partial policy collection.

The [manifest guide](docs/manifests.md) documents structured load and
validation errors.

### 3. Register RuleGate

```csharp
using Fotbiler.RuleGate.AspNetCore.DependencyInjection;

builder.Services
    .AddRuleGate()
    .AddPolicies(compilation.Policies);
```

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
controllers, diagnostics, and HTTP result mapping.

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

The latest RuleGate preview is
[`0.6.0-preview.2`](https://github.com/fotbiler-lab/rulegate/releases/tag/v0.6.0-preview.2).
It adds subject, resource, context, and literal operand comparisons for
ownership, organization-scope, numeric, and date/time policies.

All NuGet packages are `0.6.0-preview.2`. The independently versioned Angular
npm package remains at `0.5.0-preview.1`.

See the [roadmap](docs/roadmap.md) for the complete release path.

## Community

- [Contributing](CONTRIBUTING.md)
- [Support](SUPPORT.md)
- [Security policy](SECURITY.md)
- [Code of Conduct](CODE_OF_CONDUCT.md)

## License

RuleGate is licensed under the
[Apache License 2.0](LICENSE).
