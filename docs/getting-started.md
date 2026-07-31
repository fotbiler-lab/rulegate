# Getting Started with RuleGate

This guide creates and evaluates a complete RuleGate authorization policy
without requiring an identity provider, database, or remote policy service.

## What you will build

You will create a small .NET console application that:

1. Loads a policy from `rulegate.yaml`.
2. Validates and compiles the manifest.
3. Activates the complete policy source as an immutable snapshot.
4. Creates a subject and a protected resource.
5. Evaluates an authorization request.
6. Prints `Allowed` when the subject has the required permission.

## Prerequisites

Install one of the supported .NET SDKs:

- .NET 8
- .NET 9
- .NET 10

Verify the installed SDK:

```bash
dotnet --version
```

## 1. Create the application

```bash
mkdir rulegate-getting-started
cd rulegate-getting-started

dotnet new console
```

## 2. Install RuleGate

Install the ASP.NET Core integration:

```bash
dotnet add package \
  Fotbiler.RuleGate.AspNetCore \
  --version 0.9.0-preview.4
```

`Fotbiler.RuleGate.AspNetCore` brings in the authorization engine and public
contracts together with YAML loading, validation, policy compilation, and
local policy sources.

The RuleGate CLI is distributed as a separate .NET tool. Install the exact
preview version used by this guide:

```bash
dotnet tool install \
  --global \
  Fotbiler.RuleGate.Cli \
  --version 0.9.0-preview.4
```

## 3. Create the policy manifest

Create `rulegate.yaml` in the project directory:

```yaml
schemaVersion: 1

application:
  id: getting-started
  name: RuleGate Getting Started

policies:
  - id: document-read
    resourceType: document
    action: read
    requirement:
      id: document-read-permission
      permission: document.read
```

This policy matches requests where:

- The resource type is exactly `document`.
- The action is exactly `read`.
- The subject contains the `document.read` permission.

RuleGate matching is ordinal and case-sensitive.

## Validate the manifest before startup

From the directory containing `rulegate.yaml`, run:

```bash
rulegate validate
```

A valid manifest returns exit code `0`. Manifest loading, schema, structural,
or semantic validation failures return exit code `1`.

For CI systems and other automation, request pure JSON output:

```bash
rulegate validate --format json
```

An explicit path may be supplied when the manifest is stored elsewhere:

```bash
rulegate validate ./policies/rulegate.yaml
```

CLI validation uses the same fail-closed manifest compiler as application
startup. A failed validation never produces or exposes a partial policy set.

Before deployment, lint the valid manifest for structural risks:

```bash
rulegate lint ./policies/rulegate.yaml --format json
```

See [Explain and Lint](explain-and-lint.md) for lint finding codes and redacted
decision explanations built from deterministic policy-test fixtures.

Generate deterministic policy, resource-type, and action constants:

```bash
rulegate generate csharp \
  ./rulegate.yaml \
  --namespace Sample.Authorization \
  --output Generated/RuleGate.g.cs
```

Verify committed generated output in CI without modifying it:

```bash
rulegate generate csharp \
  ./rulegate.yaml \
  --namespace Sample.Authorization \
  --output Generated/RuleGate.g.cs \
  --check
```

## 4. Create the authorization flow

Replace `Program.cs` with:

```csharp
using Fotbiler.RuleGate.Abstractions.Authorization;
using Fotbiler.RuleGate.Abstractions.Policies;
using Fotbiler.RuleGate.AspNetCore.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

var services =
    new ServiceCollection();

services.AddLogging();
services.AddAuthorizationCore();

services
    .AddRuleGate()
    .AddYamlPolicyFile("rulegate.yaml");

using var serviceProvider =
    services.BuildServiceProvider(
        new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });

var reload = await serviceProvider
    .GetRequiredService<IPolicyReloadService>()
    .ReloadAsync();

if (!reload.IsSuccess)
{
    foreach (var diagnostic in reload.Diagnostics)
    {
        Console.Error.WriteLine(
            $"{diagnostic.SourceName}: " +
            $"{diagnostic.Code} at " +
            $"{diagnostic.Path ?? "root"}");
    }

    return 1;
}

var engine =
    serviceProvider.GetRequiredService<
        IAuthorizationEngine>();

var request =
    new AuthorizationRequest(
        subject:
            new AuthorizationSubject(
                id: "user-1",
                permissions:
                [
                    "document.read",
                ]),
        resource:
            new AuthorizationResource(
                type: "document",
                id: "document-1"),
        action:
            "read",
        context:
            new AuthorizationContext(
                DateTimeOffset.UnixEpoch));

var decision =
    await engine.EvaluateAsync(
        request);

Console.WriteLine(
    decision.IsAllowed
        ? "Allowed"
        : "Denied");

return decision.IsAllowed
    ? 0
    : 1;
```

This example uses a bare `ServiceCollection` instead of an ASP.NET Core
application builder. `AddLogging` supplies the logging services required by
ASP.NET Core authorization. A `WebApplicationBuilder` normally registers
those services automatically.

`AddAuthorizationCore` registers the ASP.NET Core authorization primitives,
while `AddRuleGate` registers the RuleGate policy engine and built-in
requirement evaluators. `AddYamlPolicyFile` adds the manifest as a local policy
source. The explicit initial reload validates the complete source and activates
one immutable snapshot before the first authorization request.

ASP.NET Core hosts perform the initial source load through a hosted service.
This console example calls `IPolicyReloadService` directly because it builds a
bare service provider rather than starting a host. See
[Policy sources and atomic reload](policy-sources.md) for file-change,
configuration, embedded-resource, and application-defined sources.

## 5. Run the application

```bash
dotnet run
```

Expected output:

```text
Allowed
```

The request is allowed because all three policy inputs match:

| Policy input        | Request value   |
| ------------------- | --------------- |
| Resource type       | `document`      |
| Action              | `read`          |
| Required permission | `document.read` |

## 6. Verify denial behavior

Remove `document.read` from the subject:

```csharp
subject:
    new AuthorizationSubject(
        id: "user-1")
```

Run the application again:

```bash
dotnet run
```

Expected output:

```text
Denied
```

The process exits with a non-zero code because the policy requirement is not
satisfied.

RuleGate denies authorization when:

- No matching policy exists.
- A required permission, role, or attribute is missing.
- A requirement cannot be evaluated safely.
- A policy or request contains incompatible values.

## Understanding the flow

The example follows this pipeline:

```text
rulegate.yaml
      |
      v
RuleGateManifestCompiler
      |
      v
Compiled policy definitions
      |
      v
AddRuleGate + AddPolicies
      |
      v
IAuthorizationEngine
      |
      v
AuthorizationRequest
      |
      v
AuthorizationDecision
```

Manifest loading and validation happen before the policies are registered. A
failed compilation never returns a partial policy collection.

## Common problems

### The manifest file is not found

Run `dotnet run` from the directory containing `rulegate.yaml`, or pass the
correct path to `CompileFromFileAsync`.

### The request is denied unexpectedly

Check the exact spelling and casing of:

- Resource type
- Action
- Permission
- Role
- Attribute name

RuleGate does not perform case-insensitive matching or implicit string
normalization.

### The manifest does not compile

Read both error collections:

- `LoadErrors` contains file and YAML-loading failures.
- `ValidationErrors` contains invalid RuleGate manifest structures and values.

Do not register policies when `compilation.IsSuccess` is `false`.

## Next steps

Continue with:

- The root [README](../README.md) for ASP.NET Core dynamic policies, Minimal
  API endpoints, controller attributes, diagnostics, and HTTP result mapping.
- The [ASP.NET Core enrichment guide](enrichment.md) when subject, resource, or
  context attributes must be loaded from trusted application services.
- The [RuleGate CLI guide](cli.md) for deterministic manifest validation.
- The [policy-testing guide](policy-testing.md) for host-independent
  authorization fixtures.
- The [C# code-generation guide](code-generation.md) for generated constants
  and stale-output checks.
- The [Angular SDK guide](angular.md) for frontend route and template checks.
- The [Keycloak integration guide](keycloak.md) for optional provider mapping.
- The [roadmap](roadmap.md) for planned capabilities and releases.
- The [documentation index](README.md) to navigate all available guides.
