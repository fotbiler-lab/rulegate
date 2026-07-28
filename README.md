<p align="center">
  <strong>FOTBILER</strong>
</p>

<h1 align="center">RuleGate</h1>

<p align="center">
  <strong>Local-first, provider-independent authorization for .NET</strong>
</p>

<p align="center">
  RBAC · ABAC · CBAC · Resource-based authorization · ASP.NET Core · YAML manifests · RuleGate CLI
</p>

<p align="center">
  <a href="https://github.com/fotbiler-lab/rulegate/actions/workflows/ci.yml">
    <img
      alt="CI"
      src="https://img.shields.io/github/actions/workflow/status/fotbiler-lab/rulegate/ci.yml?branch=main&amp;style=flat-square&amp;label=CI">
  </a>
  <a href="https://www.nuget.org/packages/Fotbiler.RuleGate.AspNetCore">
    <img
      alt="NuGet"
      src="https://img.shields.io/nuget/vpre/Fotbiler.RuleGate.AspNetCore?style=flat-square&amp;logo=nuget&amp;label=NuGet">
  </a>
  <a href="https://www.nuget.org/packages/Fotbiler.RuleGate.AspNetCore">
    <img
      alt="NuGet downloads"
      src="https://img.shields.io/nuget/dt/Fotbiler.RuleGate.AspNetCore?style=flat-square&amp;logo=nuget&amp;label=downloads">
  </a>
  <a href="https://github.com/fotbiler-lab/rulegate/releases/tag/v0.3.0-preview.2">
    <img
      alt="GitHub release 0.3.0-preview.2"
      src="https://img.shields.io/badge/release-v0.3.0--preview.2-orange?style=flat-square">
  </a>
  <a href="https://dotnet.microsoft.com/">
    <img
      alt=".NET 8, 9, and 10"
      src="https://img.shields.io/badge/.NET-8%20%7C%209%20%7C%2010-512BD4?style=flat-square&amp;logo=dotnet">
  </a>
  <a href="LICENSE">
    <img
      alt="License"
      src="https://img.shields.io/github/license/fotbiler-lab/rulegate?style=flat-square&amp;label=license">
  </a>
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

## Packages

| Package | Purpose |
|---|---|
| [`Fotbiler.RuleGate.Abstractions`](https://www.nuget.org/packages/Fotbiler.RuleGate.Abstractions) | Authorization contracts, policy definitions, requests, decisions, and evaluation abstractions |
| [`Fotbiler.RuleGate.Core`](https://www.nuget.org/packages/Fotbiler.RuleGate.Core) | Policy engine, built-in requirement evaluators, dispatcher, and in-memory policy provider |
| [`Fotbiler.RuleGate.Manifest`](https://www.nuget.org/packages/Fotbiler.RuleGate.Manifest) | YAML manifest loading, validation, compilation, and domain mapping |
| [`Fotbiler.RuleGate.AspNetCore`](https://www.nuget.org/packages/Fotbiler.RuleGate.AspNetCore) | ASP.NET Core dependency injection, claims mapping, dynamic policies, endpoint helpers, authorization attributes, and resource-based authorization |
| [`Fotbiler.RuleGate.Cli`](https://www.nuget.org/packages/Fotbiler.RuleGate.Cli) | .NET tool for manifest validation, deterministic C# constant generation, stale-output checks, stable exit codes, and CI usage |

## Supported .NET versions

The current preview targets:

| Runtime | Target framework |
|---|---|
| .NET 8 | `net8.0` |
| .NET 9 | `net9.0` |
| .NET 10 | `net10.0` |

Every RuleGate NuGet package includes framework-specific assemblies for all three targets. The source, test suites, package assets, and package-only consumer are verified across each supported framework.

## Current capabilities

RuleGate currently provides:

- Permission- and role-based authorization
- Subject, resource, and context attribute requirements
- Composable `all`, `any`, and `not` requirements
- Default-deny and fail-closed evaluation
- Exact, ordinal, case-sensitive matching
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
- .NET 8, .NET 9, and .NET 10 packages

See the [roadmap](docs/roadmap.md) for published milestones and planned
modules.

## Documentation

| Goal | Guide |
|---|---|
| Make the first authorization decision | [Getting started](docs/getting-started.md) |
| Understand the authorization model | [Authorization model](docs/authorization-model.md) |
| Define `rulegate.yaml` policies | [Manifest guide](docs/manifests.md) |
| Protect ASP.NET Core applications | [ASP.NET Core integration](docs/aspnetcore.md) |
| Configure logs and diagnostic sinks | [Diagnostics](docs/diagnostics.md) |
| Review trust boundaries and production controls | [Security model](docs/security.md) |
| Use the RuleGate command-line tool | [CLI guide](docs/cli.md) |
| Browse all documentation | [Documentation index](docs/README.md) |
| Review current and planned capabilities | [Roadmap](docs/roadmap.md) |

## Installation

Install the ASP.NET Core and manifest packages:

```bash
dotnet add package Fotbiler.RuleGate.AspNetCore --version 0.3.0-preview.2
dotnet add package Fotbiler.RuleGate.Manifest --version 0.3.0-preview.2
```

`Fotbiler.RuleGate.AspNetCore` references the core engine and abstractions
packages.

Applications using only RuleGate contracts may reference the abstractions
package directly:

```bash
dotnet add package Fotbiler.RuleGate.Abstractions --version 0.3.0-preview.2
```

## Use the RuleGate CLI

Install the RuleGate command-line tool:

```bash
dotnet tool install \
  --global \
  Fotbiler.RuleGate.Cli \
  --version 0.3.0-preview.2
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

The installed `0.3.0-preview.2` tool provides deterministic C# generation:

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

See the [RuleGate CLI guide](docs/cli.md) for the complete validation,
generation, output, exit-code, CI, and security contract.

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

Read the [security model](docs/security.md) before production integration.

## Project status

RuleGate is published as
[`0.3.0-preview.2`](https://github.com/fotbiler-lab/rulegate/releases/tag/v0.3.0-preview.2).

This preview includes the authorization core, manifest compilation, ASP.NET
Core integration, safe HTTP authorization result mapping, structured
diagnostics, deterministic CLI manifest validation, manifest-derived C# policy,
resource-type, and action constants, atomic output writes, byte-exact
stale-output checks, and generated-code compilation coverage across .NET 8,
.NET 9, and .NET 10.

See the [roadmap](docs/roadmap.md) for the complete release path.

## Community

- [Contributing](CONTRIBUTING.md)
- [Support](SUPPORT.md)
- [Security policy](SECURITY.md)
- [Code of Conduct](CODE_OF_CONDUCT.md)

## License

Fotbiler RuleGate is licensed under the
[Apache License 2.0](LICENSE).
