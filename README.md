# Fotbiler RuleGate

Fotbiler RuleGate is a local-first, provider-independent, and policy-driven authorization framework for .NET.

> RuleGate is currently in preview and is not yet recommended for production use.

## Packages

| Package | Purpose |
|---|---|
| `Fotbiler.RuleGate.Abstractions` | Authorization contracts, policy definitions, requests, decisions, and evaluation abstractions |
| `Fotbiler.RuleGate.Core` | Policy engine, built-in requirement evaluators, dispatcher, and in-memory policy provider |
| `Fotbiler.RuleGate.Manifest` | YAML manifest loading, validation, compilation, and domain mapping |

The current preview targets .NET 10.

## Current capabilities

- Permission-based authorization
- Role-based authorization
- Logical `all`, `any`, and `not` requirements
- YAML policy manifests
- Structured YAML loading errors
- Structured manifest validation errors
- Immutable in-memory policy provider
- Ordinal and case-sensitive matching
- Default-deny authorization
- Fail-closed requirement evaluation
- Requirement-level failure identifiers

## Installation

Install the packages required by your application:

```bash
dotnet add package Fotbiler.RuleGate.Core --prerelease
dotnet add package Fotbiler.RuleGate.Manifest --prerelease
```

`Fotbiler.RuleGate.Core` and `Fotbiler.RuleGate.Manifest` automatically reference the required abstractions package.

The abstractions package may also be referenced directly:

```bash
dotnet add package Fotbiler.RuleGate.Abstractions --prerelease
```

## Example manifest

Create a `rulegate.yaml` file:

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

## Compile a manifest

```csharp
using Fotbiler.RuleGate.Manifest.Compilation;

var compiler = new RuleGateManifestCompiler();

var compilation =
    await compiler.CompileFromFileAsync(
        "rulegate.yaml");

if (!compilation.IsSuccess)
{
    foreach (var error in compilation.LoadErrors)
    {
        Console.WriteLine(
            $"Load error: {error.Code} - {error.Message}");
    }

    foreach (var error in compilation.ValidationErrors)
    {
        Console.WriteLine(
            $"Validation error: {error.Code} at {error.Path} - {error.Message}");
    }

    return;
}
```

## Create the authorization engine

```csharp
using Fotbiler.RuleGate.Core.Engine;
using Fotbiler.RuleGate.Core.Evaluation;
using Fotbiler.RuleGate.Core.Evaluation.Evaluators;
using Fotbiler.RuleGate.Core.Policies;

var policyProvider =
    new InMemoryPolicyProvider(
        compilation.Policies);

var dispatcher =
    new RequirementEvaluationDispatcher(
    [
        new PermissionRequirementEvaluator(),
        new RoleRequirementEvaluator(),
        new AllRequirementEvaluator(),
        new AnyRequirementEvaluator(),
        new NotRequirementEvaluator()
    ]);

var engine =
    new PolicyAuthorizationEngine(
        policyProvider,
        dispatcher);
```

## Evaluate an authorization request

```csharp
using Fotbiler.RuleGate.Abstractions.Authorization;

var request =
    new AuthorizationRequest(
        subject:
            new AuthorizationSubject(
                id: "user-1",
                roles:
                [
                    "sample.editor"
                ],
                permissions:
                [
                    "sample.read"
                ]),
        resource:
            new AuthorizationResource(
                type: "sample-resource",
                id: "resource-1"),
        action: "read",
        context:
            new AuthorizationContext(
                DateTimeOffset.UtcNow));

var decision =
    await engine.EvaluateAsync(request);

Console.WriteLine(
    decision.IsAllowed
        ? "Allowed"
        : "Denied");
```

Denied decisions contain one or more authorization failures:

```csharp
foreach (var failure in decision.Failures)
{
    Console.WriteLine(
        $"Code: {failure.Code}, Requirement: {failure.RequirementId}");
}
```

## Matching semantics

RuleGate uses ordinal and case-sensitive matching for:

- Policy identifiers
- Resource types
- Actions
- Roles
- Permissions

For example, these resource types are different:

```text
sample-resource
Sample-Resource
```

When no policy matches the requested resource type and action, RuleGate denies the request.

## Manifest compilation pipeline

```text
YAML text or file
        |
        v
RuleGateManifestYamlLoader
        |
        v
RuleGateManifestValidator
        |
        v
RuleGateManifestMapper
        |
        v
PolicyDefinition collection
```

Manifest compilation keeps two failure categories separate:

- YAML and file-loading errors
- Manifest validation errors

A failed compilation never returns a partially compiled policy collection.

## Authorization pipeline

```text
AuthorizationRequest
        |
        v
PolicyAuthorizationEngine
        |
        v
InMemoryPolicyProvider
        |
        v
RequirementEvaluationDispatcher
        |
        v
AuthorizationDecision
```

## Supported requirement types

### Permission

```yaml
requirement:
  permission: sample.read
```

### Role

```yaml
requirement:
  role: sample.editor
```

### All

Every child requirement must be satisfied:

```yaml
requirement:
  all:
    - permission: sample.read
    - role: sample.editor
```

### Any

At least one child requirement must be satisfied:

```yaml
requirement:
  any:
    - role: sample.editor
    - role: sample.administrator
```

### Not

The nested requirement must not be satisfied:

```yaml
requirement:
  not:
    role: sample.blocked
```

## Security behavior

RuleGate follows these principles:

- Authorization defaults to deny.
- Missing policies produce denied decisions.
- Unsupported requirement types fail closed.
- Indeterminate requirement results produce denied decisions.
- Backend authorization remains the security boundary.
- Policy manifests do not execute arbitrary scripts.

## Project status

The current preview contains the authorization core and YAML manifest foundation.

Planned future modules include:

- ASP.NET Core integration
- Dependency injection registration
- Attribute-based authorization
- Context-based authorization
- Resource-based authorization
- Decision diagnostics and explanations
- CLI validation and code generation
- Angular integration
- Keycloak helpers
- OpenTelemetry integration

## License

Fotbiler RuleGate is licensed under the Apache License 2.0.
