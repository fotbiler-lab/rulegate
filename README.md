# Fotbiler RuleGate

Fotbiler RuleGate is a local-first, provider-independent, and policy-driven authorization framework for .NET.

> RuleGate is currently in preview and is not yet recommended for production use.

## Packages

| Package | Purpose |
|---|---|
| `Fotbiler.RuleGate.Abstractions` | Authorization contracts, policy definitions, requests, decisions, and evaluation abstractions |
| `Fotbiler.RuleGate.Core` | Policy engine, built-in requirement evaluators, dispatcher, and in-memory policy provider |
| `Fotbiler.RuleGate.Manifest` | YAML manifest loading, validation, compilation, and domain mapping |
| `Fotbiler.RuleGate.AspNetCore` | ASP.NET Core dependency injection registration and RuleGate configuration builder |

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
- ASP.NET Core dependency injection integration
- Fluent registration of policies and custom requirement evaluators
- `ClaimsPrincipal` to `AuthorizationSubject` mapping
- Configurable subject identifier, role, and permission claim types

## Installation

Install the packages required by your application:

```bash
dotnet add package Fotbiler.RuleGate.AspNetCore --prerelease
dotnet add package Fotbiler.RuleGate.Manifest --prerelease
```

`Fotbiler.RuleGate.AspNetCore` automatically references the core engine and abstractions packages. `Fotbiler.RuleGate.Manifest` automatically references the abstractions package.

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

## Register RuleGate with ASP.NET Core

Register the built-in engine, policy provider, dispatcher, and requirement evaluators:

```csharp
using Fotbiler.RuleGate.AspNetCore.DependencyInjection;

var builder =
    WebApplication.CreateBuilder(args);

builder.Services
    .AddRuleGate()
    .AddPolicies(compilation.Policies);
```

Application services may then receive `IAuthorizationEngine` through dependency injection.

## Map a ClaimsPrincipal

The default subject mapping reads:

- Subject identifier from `ClaimTypes.NameIdentifier`
- Roles from `ClaimTypes.Role`
- Permissions from the `permission` claim type

Resolve the registered factory and map the current principal:

```csharp
using Fotbiler.RuleGate.AspNetCore.Subjects;

var subjectFactory =
    serviceProvider.GetRequiredService<
        IRuleGateSubjectFactory>();

var subject =
    subjectFactory.Create(
        httpContext.User);
```

Claim types can be changed during registration:

```csharp
builder.Services
    .AddRuleGate()
    .ConfigureSubjectMapping(
        options =>
        {
            options.SubjectIdClaimType =
                "sub";

            options.RoleClaimTypes.Clear();
            options.RoleClaimTypes.Add(
                "application-role");

            options.PermissionClaimTypes.Clear();
            options.PermissionClaimTypes.Add(
                "application-permission");
        });
```

Claim type and value matching is ordinal and case-sensitive. Blank role and permission values are ignored, and exact duplicates are removed. Mapping fails when the configured subject identifier is missing or contains multiple distinct values.

## Use the ASP.NET Core authorization handler

Register an ASP.NET Core policy containing a RuleGate requirement:

```csharp
using Fotbiler.RuleGate.AspNetCore.Authorization;
using Fotbiler.RuleGate.AspNetCore.DependencyInjection;

builder.Services.AddAuthorization(
    options =>
    {
        options.AddPolicy(
            "documents.read",
            policy =>
            {
                policy.AddRequirements(
                    new RuleGateAuthorizationRequirement(
                        action: "read"));
            });
    });

builder.Services
    .AddRuleGate()
    .AddPolicies(compilation.Policies);
```

Authorize a RuleGate resource through `IAuthorizationService`:

```csharp
var resource =
    new AuthorizationResource(
        type: "document",
        id: documentId);

var result =
    await authorizationService.AuthorizeAsync(
        httpContext.User,
        resource,
        "documents.read");

if (!result.Succeeded)
{
    return Results.Forbid();
}
```

The default resource factory accepts an `AuthorizationResource` instance. Applications can replace `IRuleGateAuthorizationResourceFactory` to map domain-specific resource objects.

A RuleGate deny decision, a missing or ambiguous subject identifier, or an unsupported resource causes ASP.NET Core authorization to fail closed. Unexpected authorization-engine failures are propagated instead of being converted into a denial.

This foundation supports resource-based `IAuthorizationService.AuthorizeAsync` calls. Automatic endpoint metadata, authorization attributes, dynamic policy names, and HTTP result mapping are outside the current scope.

## Create the authorization engine manually

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
