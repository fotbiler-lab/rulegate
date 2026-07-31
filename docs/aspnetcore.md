# ASP.NET Core Integration

This guide explains how to integrate RuleGate with ASP.NET Core applications.

It covers:

- Dependency-injection registration
- Manifest policy registration
- Dynamic ASP.NET Core policies
- `ClaimsPrincipal` subject mapping
- Ordered subject, resource, and context attribute enrichment
- Minimal API endpoint authorization
- Controller and action authorization
- Imperative authorization
- Route-value resource mapping
- HTTP `401` and `403` result mapping
- Diagnostics and extension points

For the authorization concepts behind these APIs, read the
[authorization model](authorization-model.md).

For the complete YAML format, read the
[manifest guide](manifests.md).

Before production deployment, review the
[security model](security.md).

## Supported frameworks

The current RuleGate preview supports:

- .NET 8
- .NET 9
- .NET 10

## Install the packages

Install the ASP.NET Core integration:

```bash
dotnet add package \
  Fotbiler.RuleGate.AspNetCore \
  --version 0.8.0-preview.2
```

Install the manifest package when policies are defined in `rulegate.yaml`:

```bash
dotnet add package \
  Fotbiler.RuleGate.Manifest \
  --version 0.8.0-preview.2
```

`Fotbiler.RuleGate.AspNetCore` references the RuleGate core and abstractions
packages automatically.

## Integration flow

A protected HTTP request follows this conceptual flow:

```text
Authentication middleware
        |
        v
ClaimsPrincipal
        |
        v
Dynamic RuleGate policy and handler
        |
        v
IRuleGateSubjectFactory
        |
        v
AuthorizationSubject
        |
        v
IRuleGateAuthorizationResourceFactory
        |
        v
AuthorizationResource
        |
        v
Subject attribute providers
        |
        v
Resource attribute providers
        |
        v
Context attribute providers
        |
        v
IAuthorizationEngine
        |
        v
Allow or deny
        |
        v
Endpoint execution, challenge, or forbid
```

ASP.NET Core remains responsible for authentication and authorization
middleware orchestration.

The backend authorization engine remains the security boundary. Client-side
visibility checks may improve user experience, but they cannot replace
authorization on the protected backend operation.

RuleGate supplies:

- Subject mapping
- Ordered trusted-attribute enrichment
- Dynamic policy creation
- Resource mapping
- Authorization handling
- Policy evaluation
- Optional diagnostics
- Optional safe HTTP problem responses

## Example manifest

The examples in this guide use the following manifest.

<!-- executable-aspnetcore-manifest -->

```yaml
schemaVersion: 1

application:
  id: document-api
  name: Document API

policies:
  - id: document-read
    resourceType: document
    action: read
    requirement:
      permission: document.read

  - id: document-delete
    resourceType: document
    action: delete
    requirement:
      permission: document.delete
```

## Compile policies before registration

Compile the manifest before building the application:

```csharp
using Fotbiler.RuleGate.Manifest.Compilation;

var compiler =
    new RuleGateManifestCompiler();

var compilation =
    await compiler.CompileFromFileAsync(
        "rulegate.yaml");

if (!compilation.IsSuccess)
{
    foreach (var error in compilation.LoadErrors)
    {
        Console.Error.WriteLine(
            $"{error.Code}: {error.Message}");
    }

    foreach (var error in compilation.ValidationErrors)
    {
        Console.Error.WriteLine(
            $"{error.Code} at {error.Path}: " +
            error.Message);
    }

    throw new InvalidOperationException(
        "RuleGate manifest compilation failed.");
}
```

Do not register policies when compilation fails.

A failed manifest compilation returns no partially compiled policy collection.

## Register ASP.NET Core services

Register authentication and ASP.NET Core authorization according to the
application's normal security configuration.

Then register RuleGate:

```csharp
using Fotbiler.RuleGate.AspNetCore.DependencyInjection;

builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

builder.Services
    .AddRuleGate()
    .AddPolicies(compilation.Policies);
```

`AddRuleGate` registers the default:

- RuleGate authorization engine
- In-memory policy provider
- Built-in requirement evaluators
- Requirement dispatcher
- Dynamic ASP.NET Core policy provider
- RuleGate authorization handler
- Claims-based subject factory
- HTTP resource factory
- Scoped authorization request enricher
- System `TimeProvider`

Registration is idempotent for the default services.

### Registration order

A typical registration order is:

```csharp
builder.Services.AddAuthentication(
    authenticationOptions =>
    {
        // Configure the application authentication scheme.
    });

builder.Services.AddAuthorization(
    authorizationOptions =>
    {
        // Configure ordinary ASP.NET Core policies when needed.
    });

builder.Services
    .AddRuleGate()
    .AddPolicies(compilation.Policies);
```

RuleGate's policy provider delegates ordinary policy names to the standard
ASP.NET Core policy provider.

This allows RuleGate policies and normal ASP.NET Core policies to coexist.

## Enrich trusted authorization attributes

Use the ASP.NET Core enrichment pipeline when policies need tenant,
organization, clearance, ownership, MFA, trusted-device, network, or request
channel data from application services.

Register providers through the same builder in minimal-hosting applications:

```csharp
builder.Services
    .AddRuleGate()
    .AddSubjectAttributeProvider<TenantAttributeProvider>()
    .AddResourceAttributeProvider<DocumentAttributeProvider>()
    .AddContextAttributeProvider<RequestContextAttributeProvider>()
    .AddPolicies(compilation.Policies);
```

The same registration chain can be used inside `Startup.ConfigureServices`.
Providers are scoped by default and may depend on other scoped application
services.

Execution is deterministic:

1. Subject providers run.
2. Resource providers run with the enriched subject.
3. Context providers run with the enriched subject and resource.
4. The authorization engine evaluates the resulting immutable request.

Within each stage, lower `Order` values run first. Equal-order providers retain
registration order. Providers run sequentially and receive the HTTP
`RequestAborted` cancellation token.

Attribute collisions fail closed by default. A provider can explicitly select
`KeepExisting` or `ReplaceExisting` when its precedence rule is intentional.
Missing required data, provider exceptions, unsupported attribute values, and
cancellation stop the pipeline before policy evaluation.

RuleGate never trusts headers, addresses, claims, or device assertions merely
because a provider can access `HttpContext`. Providers must validate and
normalize request-derived data through trusted application components.

See [ASP.NET Core attribute enrichment](enrichment.md) for provider contracts,
complete examples, collision semantics, diagnostics, and security guidance.

## Configure the middleware

Add authentication before authorization:

```csharp
var app =
    builder.Build();

app.UseAuthentication();
app.UseAuthorization();
```

Map protected endpoints after the services and middleware are configured.

## Dynamic policy names

RuleGate uses dynamic ASP.NET Core policy names with this format:

```text
RuleGate:<resource-type>:<action>
```

Example:

```text
RuleGate:document:read
```

The public helper can create and parse this name:

```csharp
using Fotbiler.RuleGate.AspNetCore.Authorization;

var policyName =
    new RuleGatePolicyName(
        resourceType: "document",
        action: "read");

Console.WriteLine(
    policyName.ToString());
```

Output:

```text
RuleGate:document:read
```

### Policy-name rules

The prefix is exactly:

```text
RuleGate
```

The separator is:

```text
:
```

The resource type and action:

- Must not be empty
- Must not contain whitespace
- Must not contain `:`
- Are matched using ordinal, case-sensitive comparison

These are different policy names:

```text
RuleGate:document:read
RuleGate:Document:read
RuleGate:document:Read
```

Malformed policy names beginning with the owned `RuleGate:` prefix do not fall
back to an ordinary ASP.NET Core policy.

This prevents malformed RuleGate policies from silently resolving to another
authorization mechanism.

### Authentication requirement

Every dynamic RuleGate policy requires an authenticated principal.

Anonymous requests are challenged before RuleGate policy evaluation can grant
access.

## Map a ClaimsPrincipal

The default `IRuleGateSubjectFactory` converts the current
`ClaimsPrincipal` into an `AuthorizationSubject`.

Default claim types:

| Subject member     | Default claim type          |
| ------------------ | --------------------------- |
| Subject identifier | `ClaimTypes.NameIdentifier` |
| Roles              | `ClaimTypes.Role`           |
| Permissions        | `permission`                |

Example principal:

```csharp
using System.Security.Claims;

var principal =
    new ClaimsPrincipal(
        new ClaimsIdentity(
            claims:
            [
                new Claim(
                    ClaimTypes.NameIdentifier,
                    "user-42"),

                new Claim(
                    ClaimTypes.Role,
                    "document.reader"),

                new Claim(
                    "permission",
                    "document.read"),
            ],
            authenticationType:
                "Bearer"));
```

The resulting RuleGate subject contains:

```text
Id: user-42
Roles:
  - document.reader
Permissions:
  - document.read
```

### Subject identifier rules

The principal must contain exactly one distinct, non-empty subject identifier.

The following conditions fail closed:

- No subject identifier claim
- Only empty subject identifier values
- Multiple distinct subject identifier values

Repeated claims containing the same identifier are treated as one value.

### Role and permission rules

Role and permission values:

- Are compared using ordinal, case-sensitive semantics
- Ignore null, empty, and whitespace values
- Remove exact duplicates
- Preserve values that differ only by case

For example, these are distinct permissions:

```text
document.read
Document.Read
```

## Configure claim types

Identity providers often use different claim names.

Configure the mapping during RuleGate registration:

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
                "role");

            options.PermissionClaimTypes.Clear();
            options.PermissionClaimTypes.Add(
                "permissions");
        })
    .AddPolicies(compilation.Policies);
```

Multiple role or permission claim types may be configured:

```csharp
options.RoleClaimTypes.Add(
    ClaimTypes.Role);

options.RoleClaimTypes.Add(
    "realm_role");
```

Every configured claim type must contain non-whitespace text.

### Claims are not automatically attributes

The default subject factory maps:

- Subject identifier
- Roles
- Permissions

It does not automatically copy arbitrary claims into
`AuthorizationSubject.Attributes`.

A policy such as:

```yaml
attribute:
  source: subject
  name: department
  operator: equal
  valueType: string
  value: finance
```

requires a custom `IRuleGateSubjectFactory` that maps a trusted department
value into the subject's attribute collection.

Do not assume that a claim named `department` becomes a RuleGate attribute
without explicit mapping.

## Protect Minimal API endpoints

Use `RequireRuleGate`:

```csharp
using Fotbiler.RuleGate.AspNetCore.Endpoints;

app.MapGet(
        "/documents/{id}",
        (
            string id) =>
        {
            return Results.Ok(
                new
                {
                    id,
                });
        })
    .RequireRuleGate(
        resourceType: "document",
        action: "read",
        resourceIdRouteValue: "id");
```

This creates the dynamic policy:

```text
RuleGate:document:read
```

It also adds endpoint metadata describing:

- Resource type
- Action
- Route-value name used as the resource ID

For a request to:

```text
/documents/document-42
```

the default resource factory creates:

```text
AuthorizationResource
├── Type: document
└── Id: document-42
```

### Collection-level endpoints

A resource ID is optional:

```csharp
app.MapPost(
        "/documents",
        () =>
        {
            return Results.Created();
        })
    .RequireRuleGate(
        resourceType: "document",
        action: "create");
```

The default resource contains the resource type without an instance ID.

This is useful for operations such as:

- Create
- List
- Search
- Export a collection

### Route-value requirements

When `resourceIdRouteValue` is configured:

- The endpoint must expose that route value.
- The route value must not be null.
- Its invariant string representation must not be empty or whitespace.

A missing or empty required route value fails authorization before the
RuleGate engine is evaluated.

### Endpoint metadata consistency

Equivalent RuleGate metadata for the same policy is allowed.

Conflicting metadata for the same resource type and action fails closed.

The current request must also have a resolved ASP.NET Core endpoint.

## Protect controllers and actions

Use `RuleGateAuthorizeAttribute`:

```csharp
using Fotbiler.RuleGate.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/documents")]
public sealed class DocumentsController
    : ControllerBase
{
    [HttpGet("{id}")]
    [RuleGateAuthorize(
        resourceType: "document",
        action: "read",
        resourceIdRouteValue: "id")]
    public IActionResult Get(
        string id)
    {
        return Ok(
            new
            {
                id,
            });
    }
}
```

The attribute can be placed on:

- A controller class
- An action method

It participates in the same dynamic policy, subject mapping, resource mapping,
and authorization handler used by Minimal APIs.

## Imperative authorization

Use `IAuthorizationService` when authorization must occur after loading or
constructing a resource.

```csharp
using Fotbiler.RuleGate.Abstractions.Authorization;
using Fotbiler.RuleGate.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization;

public static async Task<IResult>
    GetDocumentAsync(
        string id,
        ClaimsPrincipal user,
        IAuthorizationService authorizationService)
{
    var resource =
        new AuthorizationResource(
            type: "document",
            id: id);

    var result =
        await authorizationService
            .AuthorizeRuleGateAsync(
                user,
                resource,
                action: "read");

    if (!result.Succeeded)
    {
        return Results.Forbid();
    }

    return Results.Ok(
        new
        {
            id,
        });
}
```

The `AuthorizationResource` overload uses `resource.Type` to create the dynamic
policy name.

Conceptually:

```text
AuthorizationResource.Type: document
Action: read
Policy: RuleGate:document:read
```

### Explicit resource-type overload

A second overload accepts:

- A `ClaimsPrincipal`
- An arbitrary resource object
- An explicit RuleGate resource type
- An action

```csharp
var result =
    await authorizationService
        .AuthorizeRuleGateAsync(
            user,
            document,
            resourceType: "document",
            action: "read");
```

The default resource factory does not support arbitrary domain objects.

This overload requires a custom `IRuleGateAuthorizationResourceFactory` that
knows how to map the supplied domain object.

## Default resource mapping

The default `RuleGateAuthorizationResourceFactory` supports two integration
paths.

### Existing AuthorizationResource

For imperative authorization, it accepts an existing:

```csharp
new AuthorizationResource(
    type: "document",
    id: "document-42");
```

### HttpContext endpoint resource

For Minimal APIs and controller attributes, it maps the current `HttpContext`
using RuleGate endpoint metadata.

It creates:

- Resource type from the dynamic policy metadata
- Optional resource ID from a configured route value

It does not automatically:

- Load a domain entity
- Read a database
- Add resource attributes
- Validate that the domain entity exists
- Map route values other than the configured identifier

Automatic domain loading from route identifiers is outside the current
preview scope.

## Custom resource mapping

Register a custom resource factory before calling `AddRuleGate`:

```csharp
builder.Services.AddSingleton<
    IRuleGateAuthorizationResourceFactory,
    ApplicationRuleGateResourceFactory>();

builder.Services
    .AddRuleGate()
    .AddPolicies(compilation.Policies);
```

A custom implementation may map domain objects used by imperative
authorization.

When the application also uses endpoint helpers or controller attributes, the
custom implementation must preserve `HttpContext` mapping.

Example structure:

```csharp
using Fotbiler.RuleGate.Abstractions.Authorization;
using Fotbiler.RuleGate.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

public sealed class
    ApplicationRuleGateResourceFactory
    : IRuleGateAuthorizationResourceFactory
{
    private readonly
        RuleGateAuthorizationResourceFactory
        _frameworkFactory = new();

    public AuthorizationResource Create(
        object? resource)
    {
        return resource switch
        {
            AuthorizationResource existing =>
                existing,

            Document document =>
                new AuthorizationResource(
                    type: "document",
                    id: document.Id),

            _ =>
                throw new InvalidOperationException(
                    "The resource type is not supported."),
        };
    }

    public AuthorizationResource Create(
        object? resource,
        RuleGateAuthorizationRequirement requirement)
    {
        if (resource is HttpContext ||
            resource is AuthorizationResource)
        {
            return _frameworkFactory.Create(
                resource,
                requirement);
        }

        return Create(resource);
    }
}
```

The factory must build resources from trusted server-side data.

## Resource type consistency

The dynamic policy resource type must match the mapped
`AuthorizationResource.Type`.

For example:

```text
Policy resource type: document
Mapped resource type: invoice
```

fails before the RuleGate engine evaluates the request.

This prevents a resource prepared for one policy domain from being evaluated
under another domain's policy.

## Authorization context

The default ASP.NET Core handler creates an `AuthorizationContext` with:

- Evaluation time from `TimeProvider.GetUtcNow()`
- No additional context attributes

The default provider is:

```text
TimeProvider.System
```

Applications can register a custom `TimeProvider` before calling
`AddRuleGate`:

```csharp
builder.Services.AddSingleton<TimeProvider>(
    applicationTimeProvider);

builder.Services
    .AddRuleGate()
    .AddPolicies(compilation.Policies);
```

RuleGate preserves a previously registered `TimeProvider`.

### Context attributes

The default ASP.NET Core handler does not map request headers, network
information, authentication methods, or other `HttpContext` data into
`AuthorizationContext.Attributes`.

Policies requiring context attributes need an explicit application integration
that constructs the required RuleGate context.

The direct `IAuthorizationEngine` API provides full control over subject,
resource, action, and context construction.

First-class `timeWindow` and `dateTimeWindow` policies work with the default
handler because it supplies evaluation time from `TimeProvider`. Register a
controlled `TimeProvider` in tests to exercise schedule boundaries
deterministically.

`contextAge` and `context` policies require canonical trusted attributes such
as `authenticationTime`, `multiFactorAuthenticationTime`,
`authenticationMethod`, `networkZone`, or `trustedDevice`. The default handler
does not infer these values. Until an application-specific context integration
constructs them explicitly, such requirements deny access.

## HTTP authorization results

ASP.NET Core authentication and authorization use the normal framework
semantics:

| Situation                        | Result            |
| -------------------------------- | ----------------- |
| Anonymous request                | Challenge         |
| Authenticated but denied request | Forbid            |
| Allowed request                  | Endpoint executes |

Without optional RuleGate HTTP mapping, the configured authentication scheme
and ASP.NET Core result handler determine the response body.

## Enable ProblemDetails mapping

HTTP authorization-result mapping is disabled by default.

Enable it explicitly:

```csharp
builder.Services
    .AddRuleGate()
    .AddHttpAuthorizationResultMapping()
    .AddPolicies(compilation.Policies);
```

The mapping applies only to policies containing a
`RuleGateAuthorizationRequirement`.

Ordinary ASP.NET Core policies continue to use the framework's default result
handling.

### Anonymous response

An anonymous RuleGate request produces:

```http
HTTP/1.1 401 Unauthorized
Content-Type: application/problem+json
```

General response shape:

```json
{
  "type": "urn:fotbiler:rulegate:authorization:authentication-required",
  "title": "Authentication is required.",
  "status": 401,
  "detail": "The request requires an authenticated identity.",
  "code": "RULEGATE_AUTHENTICATION_REQUIRED",
  "traceId": "..."
}
```

### Forbidden response

A denied authenticated request produces:

```http
HTTP/1.1 403 Forbidden
Content-Type: application/problem+json
```

General response shape:

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

### Public constants

Problem codes:

```csharp
RuleGateHttpAuthorizationProblemCodes
    .AuthenticationRequired

RuleGateHttpAuthorizationProblemCodes
    .AccessForbidden
```

Problem types:

```csharp
RuleGateHttpAuthorizationProblemTypes
    .AuthenticationRequired

RuleGateHttpAuthorizationProblemTypes
    .AccessForbidden
```

### Authentication handler behavior

The configured authentication scheme performs its normal challenge or forbid
operation first.

Headers produced by the authentication scheme are preserved, including values
such as:

```text
WWW-Authenticate
```

If the authentication handler has already started the response, RuleGate does
not replace the response body.

### Custom result handlers

`AddHttpAuthorizationResultMapping` does not replace a custom
`IAuthorizationMiddlewareResultHandler` already registered by the application.

## Public response safety

Default RuleGate problem responses intentionally exclude:

- Engine failure codes
- Requirement identifiers
- Policy details
- Claims
- Roles
- Permission values
- Subject identifiers
- Resource identifiers
- Route values
- Attribute names
- Attribute values

The response contains only generic public authorization information and a
trace identifier.

Detailed authorization diagnostics belong in trusted server-side
observability systems.

## Authorization diagnostics

Diagnostics are disabled by default.

Enable the built-in structured logging sink explicitly:

```csharp
builder.Services
    .AddRuleGate()
    .AddLoggingDiagnostics()
    .AddPolicies(compilation.Policies);
```

RuleGate emits authorization-level Information events and requirement-level
Debug events. A custom `IAuthorizationDiagnosticsSink` can replace the
built-in sink.

See the dedicated [diagnostics guide](diagnostics.md) for event IDs, diagnostic
models, parent-child traces, custom sink behavior, cancellation semantics, and
sensitive-data boundaries.

## Custom requirement evaluators

Register an application-specific evaluator:

```csharp
builder.Services
    .AddRuleGate()
    .AddRequirementEvaluator<
        ApplicationRequirementEvaluator>()
    .AddPolicies(compilation.Policies);
```

Evaluators are registered by implementation type and do not replace the
built-in evaluators automatically.

Custom evaluators must preserve:

- Deterministic behavior
- Cancellation
- Structured failure information
- Fail-closed outcomes
- Thread safety appropriate for singleton registration

## Direct engine access

The ASP.NET Core package also registers `IAuthorizationEngine`.

Use it when the application needs complete control over:

- Subject attributes
- Resource attributes
- Context attributes
- Evaluation time
- Non-HTTP authorization flows
- Background jobs
- Message consumers

Example:

```csharp
var decision =
    await authorizationEngine.EvaluateAsync(
        new AuthorizationRequest(
            subject,
            resource,
            action,
            context),
        cancellationToken);
```

Direct engine access bypasses ASP.NET Core challenge and forbid behavior.

The application must map the resulting decision to its own transport or
application response.

## Failure behavior

RuleGate ASP.NET Core integration denies authorization when:

- The engine returns a denied decision
- Subject mapping fails
- The subject identifier is missing
- The subject identifier is ambiguous
- Resource mapping fails
- A required route value is missing
- A required route value is empty
- Endpoint metadata is missing
- Endpoint metadata conflicts
- The mapped resource type does not match the policy
- No RuleGate policy matches the resource type and action

The RuleGate engine is not evaluated until a valid subject and resource have
been created.

### Unexpected failures

Unexpected authorization-engine exceptions are propagated.

They are not silently converted into an ordinary deny result.

This preserves operational visibility while still preventing the protected
endpoint from executing.

Applications should handle unexpected failures through their normal exception
handling, logging, telemetry, and availability strategy.

## Testing guidance

Test RuleGate integration at several levels.

### Manifest tests

Verify:

- The manifest compiles.
- Expected policy IDs exist.
- Duplicate routes fail.
- Invalid policy input produces no partial policy set.

### Subject mapping tests

Verify:

- The expected subject ID claim is mapped.
- Role claims are mapped.
- Permission claims are mapped.
- Missing subject IDs fail.
- Ambiguous subject IDs fail.
- Case-sensitive values remain distinct.

### Endpoint tests

Verify:

- Anonymous requests return a challenge.
- Authenticated denied requests return a forbid.
- Allowed requests execute the endpoint.
- Route IDs are mapped correctly.
- Missing route IDs fail closed.
- Controller attributes behave like Minimal API helpers.

### Response safety tests

When ProblemDetails mapping is enabled, verify that responses do not expose:

- Policy failures
- Requirement IDs
- Claims
- Permissions
- Subject IDs
- Resource IDs
- Route values

### Direct engine tests

For attribute-based rules, test:

- Missing attributes
- Invalid attribute types
- Indeterminate outcomes
- Allowed and denied combinations
- Context-specific behavior

## Common mistakes

### Omitting ASP.NET Core authorization registration

Register ASP.NET Core authorization:

```csharp
builder.Services.AddAuthorization();
```

before using protected endpoints.

### Omitting authentication middleware

Incorrect:

```csharp
app.UseAuthorization();
```

Correct:

```csharp
app.UseAuthentication();
app.UseAuthorization();
```

### Using a policy route that does not exist

This endpoint:

```csharp
.RequireRuleGate(
    "document",
    "archive")
```

requires a RuleGate policy with:

```yaml
resourceType: document
action: archive
```

When no policy matches, authorization is denied.

### Using the wrong casing

These are different:

```text
document
Document
```

The same applies to:

- Actions
- Roles
- Permissions
- Policy names
- Claim values

### Assuming route IDs load domain entities

This:

```csharp
resourceIdRouteValue: "id"
```

maps the route value into `AuthorizationResource.Id`.

It does not load the document from a repository.

### Assuming claims become attributes

Default claims mapping handles:

- Subject ID
- Roles
- Permissions

Other claims require an explicit custom subject mapping implementation when
they must become RuleGate attributes.

### Returning internal failures to clients

Do not serialize `AuthorizationDecision.Failures` directly into public HTTP
responses.

Use generic `401` and `403` responses and keep detailed diagnostics in trusted
logs.

## Current integration boundaries

The current ASP.NET Core integration includes:

- Dependency-injection registration
- Dynamic RuleGate policies
- Standard-policy fallback
- Claims-based subject mapping
- Configurable claim types
- Minimal API endpoint helper
- Controller and action attribute
- Route-value resource-ID mapping
- Imperative authorization extensions
- Replaceable subject and resource factories
- Replaceable `TimeProvider`
- Optional logging diagnostics
- Optional HTTP ProblemDetails mapping
- Generic safe `401` and `403` responses

The current integration does not automatically provide:

- Authentication configuration
- Identity-provider-specific claim transformation
- Arbitrary claim-to-attribute mapping
- Domain entity loading from route IDs
- Resource attribute loading
- Context attribute mapping from `HttpContext`
- Manifest hot reload
- Remote policy loading
- Frontend authorization enforcement

## Next steps

Continue with:

- [Getting started](getting-started.md) for the smallest executable example.
- [Authorization model](authorization-model.md) for core concepts.
- [Manifest guide](manifests.md) for complete YAML reference.
- The root [README](../README.md) for the repository overview.
- [Documentation index](README.md) for all guides.
