# Fotbiler RuleGate

Fotbiler RuleGate is a local-first, provider-independent, and policy-driven authorization framework for .NET.

> RuleGate is currently in preview and is not yet recommended for production use.

## Packages

| Package | Purpose |
|---|---|
| `Fotbiler.RuleGate.Abstractions` | Authorization contracts, policy definitions, requests, decisions, and evaluation abstractions |
| `Fotbiler.RuleGate.Core` | Policy engine, built-in requirement evaluators, dispatcher, and in-memory policy provider |
| `Fotbiler.RuleGate.Manifest` | YAML manifest loading, validation, compilation, and domain mapping |
| `Fotbiler.RuleGate.AspNetCore` | ASP.NET Core dependency injection, claims mapping, dynamic policy resolution, and resource-based authorization |

## Supported .NET versions

The current preview targets:

| Runtime | Target framework |
|---|---|
| .NET 8 | `net8.0` |
| .NET 9 | `net9.0` |
| .NET 10 | `net10.0` |

Every RuleGate NuGet package includes framework-specific assemblies for all three targets. The source, test suites, package assets, and package-only consumer are verified across each supported framework.

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
- Resource-based ASP.NET Core authorization handler
- Dynamic ASP.NET Core policy names using `RuleGate:<resource-type>:<action>`
- Fallback-compatible standard ASP.NET Core policies
- Fail-closed resource-type enforcement
- Multi-targeted .NET 8, .NET 9, and .NET 10 packages

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

## Use dynamic ASP.NET Core policies

Register ASP.NET Core authorization together with RuleGate:

```csharp
using Fotbiler.RuleGate.AspNetCore.DependencyInjection;

builder.Services.AddAuthorization();

builder.Services
    .AddRuleGate()
    .AddPolicies(compilation.Policies);
```

RuleGate dynamically resolves policy names with this format:

```text
RuleGate:<resource-type>:<action>
```

For example:

```text
RuleGate:document:read
RuleGate:document:update
RuleGate:invoice:approve
```

Policy names, resource types, and actions use ordinal and case-sensitive matching. Each segment must be non-empty and cannot contain whitespace or the `:` separator.

Authorize an `AuthorizationResource` through the RuleGate authorization-service extension:

```csharp
using Fotbiler.RuleGate.Abstractions.Authorization;
using Fotbiler.RuleGate.AspNetCore.Authorization;

var resource =
    new AuthorizationResource(
        type: "document",
        id: documentId);

var result =
    await authorizationService.AuthorizeRuleGateAsync(
        httpContext.User,
        resource,
        action: "read");

if (!result.Succeeded)
{
    return Results.Forbid();
}
```

This overload derives the policy resource type from `AuthorizationResource.Type` and constructs the structured RuleGate policy name automatically.

Applications using a custom `IRuleGateAuthorizationResourceFactory` may authorize a domain object by supplying its RuleGate resource type explicitly:

```csharp
var result =
    await authorizationService.AuthorizeRuleGateAsync(
        httpContext.User,
        document,
        resourceType: "document",
        action: "read");
```

Direct `IAuthorizationService.AuthorizeAsync` calls together with `RuleGatePolicyName` remain available as the lower-level API.

The dynamic policy provider creates an authenticated-user requirement together with a `RuleGateAuthorizationRequirement` for the resource type and action encoded in the policy name.

Policy names not owned by RuleGate are delegated to the standard ASP.NET Core policy provider. Applications may therefore continue registering ordinary named policies through `AddAuthorization`.

Malformed names beginning with `RuleGate:` do not fall back to an ordinary policy. They remain unresolved and authorization fails closed.

The default resource factory accepts an `AuthorizationResource` instance. Applications can replace `IRuleGateAuthorizationResourceFactory` to map domain-specific resource objects.

The resource type carried by the dynamic policy must match the mapped `AuthorizationResource.Type`. A mismatch fails before the RuleGate engine is evaluated.

A RuleGate deny decision, a missing or ambiguous subject identifier, an unsupported resource, or a resource-type mismatch causes ASP.NET Core authorization to fail closed. Unexpected authorization-engine failures are propagated instead of being converted into a denial.

This integration supports resource-based `IAuthorizationService.AuthorizeAsync` calls. Authorization attributes, endpoint helpers, automatic domain-resource mapping, and automatic HTTP result mapping remain outside the current scope.

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

The current preview contains the authorization core, YAML manifest compilation, ASP.NET Core dependency injection, claims mapping, resource-based authorization handling, and dynamic named-policy resolution.

Planned future modules include:

- Authorization attributes and endpoint helpers
- Automatic HTTP authorization-result mapping
- Domain-specific resource mapping helpers
- Subject, resource, and context attribute extraction
- Attribute-based authorization
- Context-based authorization
- Decision diagnostics and explanations
- CLI validation and code generation
- Angular integration
- Keycloak helpers
- OpenTelemetry integration

## License

Fotbiler RuleGate is licensed under the Apache License 2.0.
