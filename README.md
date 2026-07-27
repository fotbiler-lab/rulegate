# Fotbiler RuleGate

Fotbiler RuleGate is a local-first, provider-independent, and policy-driven authorization framework for .NET.

> RuleGate is currently in preview and is not yet recommended for production use.

## Packages

| Package | Purpose |
|---|---|
| `Fotbiler.RuleGate.Abstractions` | Authorization contracts, policy definitions, requests, decisions, and evaluation abstractions |
| `Fotbiler.RuleGate.Core` | Policy engine, built-in requirement evaluators, dispatcher, and in-memory policy provider |
| `Fotbiler.RuleGate.Manifest` | YAML manifest loading, validation, compilation, and domain mapping |
| `Fotbiler.RuleGate.AspNetCore` | ASP.NET Core dependency injection, claims mapping, dynamic policies, endpoint helpers, authorization attributes, and resource-based authorization |

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
- Subject, resource, and context attribute requirements
- Strict scalar attribute comparison with typed normalization
- YAML attribute requirements with explicit scalar value types
- Requirement-level failure identifiers
- Opt-in authorization decision diagnostics
- Nested requirement evaluation traces with parent-child identifiers and durations
- Safe ASP.NET Core structured logging diagnostics
- ASP.NET Core dependency injection integration
- Fluent registration of policies and custom requirement evaluators
- `ClaimsPrincipal` to `AuthorizationSubject` mapping
- Configurable subject identifier, role, and permission claim types
- Resource-based ASP.NET Core authorization handler
- Dynamic ASP.NET Core policy names using `RuleGate:<resource-type>:<action>`
- Minimal API endpoint authorization through `RequireRuleGate`
- Controller and action authorization through `[RuleGateAuthorize]`
- Route-value mapping into `AuthorizationResource.Id`
- Standard ASP.NET Core `401 Challenge` and `403 Forbid` behavior
- Opt-in RuleGate `401` and `403` `application/problem+json` mapping
- Authentication challenge and forbid header preservation
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

## Enable authorization diagnostics

Authorization diagnostics are disabled by default. Enable the built-in ASP.NET Core logging sink explicitly:

```csharp
builder.Services
    .AddRuleGate()
    .AddLoggingDiagnostics()
    .AddPolicies(compilation.Policies);
```

The logging sink emits:

- Event `2000` at `Information` level for the completed authorization decision
- Event `2001` at `Debug` level for every evaluated requirement

The authorization-level entry includes the evaluation identifier, policy identifier, allow/deny result, elapsed duration, failure codes, and requirement count.

Requirement entries include evaluation and parent identifiers, requirement identifier and kind, outcome, elapsed duration, failure codes, and the attribute source when applicable. This preserves the complete `all`, `any`, and `not` evaluation tree.

Diagnostic models never contain attribute values. The built-in logging sink additionally omits attribute names, subject identifiers, resource identifiers, claims, roles, permissions, and raw authorization requests.

Applications can provide a custom sink:

```csharp
using Fotbiler.RuleGate.Abstractions.Diagnostics;

builder.Services.AddSingleton<
    IAuthorizationDiagnosticsSink,
    ApplicationAuthorizationDiagnosticsSink>();

builder.Services.AddRuleGate();
```

A custom sink participates only when its `IsEnabled` property returns `true`. Sink failures are isolated and cannot change an authorization decision or make authorization unavailable.

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

The default resource factory accepts an `AuthorizationResource` instance for imperative authorization. For HTTP endpoint authorization, it also maps the current `HttpContext` into an `AuthorizationResource` by reading matching RuleGate endpoint metadata and an optional route-value name.

### Protect Minimal API endpoints

Use `RequireRuleGate` to attach RuleGate metadata and the corresponding dynamic authorization policy:

```csharp
using Fotbiler.RuleGate.AspNetCore.Endpoints;

app.MapPost(
        "/documents/{id}/approve",
        ApproveDocumentAsync)
    .RequireRuleGate(
        resourceType: "document",
        action: "approve",
        resourceIdRouteValue: "id");
```

The `id` route value is mapped into `AuthorizationResource.Id`. For collection-level operations such as creating a resource, omit `resourceIdRouteValue`:

```csharp
app.MapPost(
        "/documents",
        CreateDocumentAsync)
    .RequireRuleGate(
        resourceType: "document",
        action: "create");
```

### Protect controllers and actions

Use `RuleGateAuthorizeAttribute` on a controller or action:

```csharp
using Fotbiler.RuleGate.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("documents")]
public sealed class DocumentsController
    : ControllerBase
{
    [HttpPost("{id}/approve")]
    [RuleGateAuthorize(
        resourceType: "document",
        action: "approve",
        resourceIdRouteValue: "id")]
    public IActionResult Approve(
        string id)
    {
        return Ok();
    }
}
```

Both integrations use the standard ASP.NET Core authentication and authorization middleware. Anonymous requests are challenged, denied authenticated requests are forbidden, and allowed requests proceed to the endpoint or controller action.

### Map RuleGate HTTP authorization results

HTTP authorization-result mapping is disabled by default. Enable it explicitly during RuleGate registration:

```csharp
builder.Services
    .AddRuleGate()
    .AddHttpAuthorizationResultMapping()
    .AddPolicies(compilation.Policies);
```

The mapping applies only to authorization policies containing a `RuleGateAuthorizationRequirement`:

- Anonymous RuleGate requests produce a `401` `application/problem+json` response.
- Denied authenticated RuleGate requests produce a `403` `application/problem+json` response.
- Successful RuleGate requests continue to the endpoint normally.
- Ordinary ASP.NET Core policies retain the framework's default challenge and forbid behavior.

The configured authentication scheme still performs its normal challenge or forbid operation first. Authentication headers such as `WWW-Authenticate`, together with any other scheme-generated headers, are preserved. When an authentication handler has already started the response, RuleGate does not replace its response body.

A denied response has this general shape:

```json
{
  "type": "urn:fotbiler:rulegate:authorization:access-forbidden",
  "title": "Access is forbidden.",
  "status": 403,
  "detail": "The authenticated identity is not authorized to access this resource.",
  "code": "RULEGATE_ACCESS_FORBIDDEN",
  "traceId": "..."
}
```

The public problem identifiers are available through:

- `RuleGateHttpAuthorizationProblemTypes.AuthenticationRequired`
- `RuleGateHttpAuthorizationProblemTypes.AccessForbidden`
- `RuleGateHttpAuthorizationProblemCodes.AuthenticationRequired`
- `RuleGateHttpAuthorizationProblemCodes.AccessForbidden`

The default response intentionally excludes RuleGate engine failure codes, requirement identifiers, policy details, claims, roles, permissions, subject identifiers, resource identifiers, and route values.

`AddHttpAuthorizationResultMapping` does not replace a custom `IAuthorizationMiddlewareResultHandler` that was already registered by the application.

Applications can replace `IRuleGateAuthorizationResourceFactory` to map domain-specific resource objects. Existing implementations of its original `Create(object?)` method remain compatible.

The resource type carried by the dynamic policy must match the mapped `AuthorizationResource.Type`. A mismatch fails before the RuleGate engine is evaluated.

A RuleGate deny decision, a missing or ambiguous subject identifier, an unsupported resource, or a resource-type mismatch causes ASP.NET Core authorization to fail closed. Unexpected authorization-engine failures are propagated instead of being converted into a denial.

The imperative authorization-service extensions, Minimal API endpoint helper, and controller/action attribute all use the same dynamic policy provider and RuleGate authorization handler. Missing endpoints, missing required route values, empty route values, conflicting metadata, unsupported resources, and resource-type mismatches fail closed before the RuleGate engine is allowed to grant access.

Automatic loading of domain entities from route identifiers remains outside the current scope.

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
        new AttributeRequirementEvaluator(),
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

### Attribute

`AttributeRequirementDefinition` compares a named attribute from the subject, resource, or authorization context.

Attribute requirements can be created through the code API or declared in `rulegate.yaml`:

```yaml
requirement:
  id: finance-department
  attribute:
    source: subject
    name: department
    operator: equal
    valueType: string
    value: finance
```

Supported sources are:

- `subject`
- `resource`
- `context`

Supported operators are:

- `equal`
- `notEqual`
- `greaterThan`
- `greaterThanOrEqual`
- `lessThan`
- `lessThanOrEqual`

Supported `valueType` tokens are:

- `nullValue`
- `string`
- `boolean`
- `number`
- `dateTimeOffset`

The `value` member is always required, including explicit null comparisons:

```yaml
attribute:
  source: resource
  name: parentId
  operator: equal
  valueType: nullValue
  value: null
```

`nullValue` is used instead of `null` because an unquoted YAML `null` token is deserialized as an absent scalar value.

Integral and decimal YAML numbers are parsed as invariant-culture decimal values. Scientific notation and implicit string-to-number coercion are not supported.

Boolean values accept the canonical lowercase `true` and `false` tokens.

`dateTimeOffset` values must include an explicit UTC marker or numeric offset, such as `2026-07-27T07:30:00Z` or `2026-07-27T10:30:00+03:00`. Local date-time values without an offset are rejected.

The `equal` and `notEqual` operators support every scalar value kind. Ordering operators support only `number` and `dateTimeOffset`.

A missing runtime attribute produces a not-satisfied result. Type mismatches, unsupported runtime values, and unsupported operator/type combinations produce an indeterminate result. Both outcomes deny authorization through the fail-closed policy engine.

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

Diagnostics are disabled by default. Diagnostic models never contain attribute values. The built-in logging sink also omits attribute names, subject and resource identifiers, claims, role and permission values, and raw authorization requests. Diagnostics sink failures are isolated from authorization decisions.

## Project status

The current preview contains the authorization core, typed subject/resource/context attribute requirements, YAML manifest compilation, opt-in authorization diagnostics, safe ASP.NET Core structured logging, dependency injection, claims mapping, resource-based authorization handling, dynamic named-policy resolution, Minimal API endpoint authorization, controller/action authorization attributes, and opt-in HTTP authorization problem-details mapping.

Planned future modules include:

- Domain-specific resource mapping helpers
- Subject, resource, and context attribute extraction
- Context-based authorization
- Higher-level decision explanation and visualization tooling
- CLI validation and code generation
- Angular integration
- Keycloak helpers
- OpenTelemetry integration

## License

Fotbiler RuleGate is licensed under the Apache License 2.0.
