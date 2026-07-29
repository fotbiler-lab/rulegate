# ASP.NET Core Attribute Enrichment

The RuleGate ASP.NET Core enrichment pipeline supplies trusted subject,
resource, and context attributes immediately before policy evaluation.

Use it when authorization data must come from application services such as a
tenant resolver, organization directory, resource repository, MFA session, or
trusted network classifier instead of being embedded directly in identity
claims.

The authorization engine and policy model remain provider-independent. The
pipeline is an ASP.NET Core integration boundary; it does not add identity,
database, or remote-policy dependencies to the core engine.

## Pipeline order

RuleGate enriches one authorization request in three fixed stages:

```text
ClaimsPrincipal              Framework resource
        |                            |
        v                            v
IRuleGateSubjectFactory  IRuleGateAuthorizationResourceFactory
        |                            |
        +-------------+--------------+
                      |
                      v
          Base AuthorizationRequest
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
```

Each provider receives the subject, resource, and context produced so far.
Resource providers can therefore use enriched subject attributes, and context
providers can use both enriched subject and resource attributes.

Within one stage, providers run sequentially by ascending `Order`. Providers
with the same order retain dependency-injection registration order. RuleGate
does not execute providers concurrently.

## Provider contracts

Implement the contract matching the target attribute source:

- `IRuleGateSubjectAttributeProvider`
- `IRuleGateResourceAttributeProvider`
- `IRuleGateContextAttributeProvider`

All three expose the same members:

```csharp
int Order { get; }

RuleGateAttributeCollisionBehavior CollisionBehavior { get; }

ValueTask<RuleGateAttributeProviderResult> ProvideAttributesAsync(
    RuleGateAttributeProviderContext context,
    CancellationToken cancellationToken = default);
```

The default order is `0`. The default collision behavior is `Fail`.

`RuleGateAttributeProviderContext` exposes:

- The authenticated `ClaimsPrincipal`
- The original ASP.NET Core authorization resource
- `HttpContext` when the framework resource is an HTTP request
- The current `AuthorizationSubject`
- The current `AuthorizationResource`
- The current `AuthorizationContext`
- The requested action

The original framework resource and `HttpContext` are integration inputs, not
automatically trusted authorization data.

## Register providers

Providers are scoped by default so they can safely depend on request-scoped
services such as EF Core contexts.

Minimal hosting:

```csharp
using Fotbiler.RuleGate.AspNetCore.DependencyInjection;

builder.Services
    .AddRuleGate()
    .AddSubjectAttributeProvider<TenantAttributeProvider>()
    .AddResourceAttributeProvider<DocumentAttributeProvider>()
    .AddContextAttributeProvider<RequestContextAttributeProvider>()
    .AddPolicies(compilation.Policies);
```

The same builder works in `Startup.ConfigureServices`:

```csharp
public void ConfigureServices(
    IServiceCollection services)
{
    services.AddAuthentication();
    services.AddAuthorization();

    services
        .AddRuleGate()
        .AddSubjectAttributeProvider<TenantAttributeProvider>()
        .AddResourceAttributeProvider<DocumentAttributeProvider>()
        .AddContextAttributeProvider<RequestContextAttributeProvider>()
        .AddPolicies(compilation.Policies);
}
```

An explicit lifetime can be supplied when a provider is stateless or needs a
different lifecycle:

```csharp
builder.Services
    .AddRuleGate()
    .AddContextAttributeProvider<StaticChannelProvider>(
        ServiceLifetime.Singleton);
```

Do not register a singleton provider that depends on scoped services.

## Subject provider example

This provider loads a trusted tenant assignment from an application service.

```csharp
using Fotbiler.RuleGate.Abstractions.Attributes;
using Fotbiler.RuleGate.AspNetCore.Enrichment;

public sealed class TenantAttributeProvider
    : IRuleGateSubjectAttributeProvider
{
    private readonly ITenantAssignmentReader _assignments;

    public TenantAttributeProvider(
        ITenantAssignmentReader assignments)
    {
        _assignments = assignments;
    }

    public int Order => 100;

    public async ValueTask<RuleGateAttributeProviderResult>
        ProvideAttributesAsync(
            RuleGateAttributeProviderContext context,
            CancellationToken cancellationToken = default)
    {
        var tenantId = await _assignments.FindTenantIdAsync(
            context.Subject.Id,
            cancellationToken);

        if (tenantId is null)
        {
            return RuleGateAttributeProviderResult
                .MissingRequiredData();
        }

        return RuleGateAttributeProviderResult.Success(
            new AuthorizationAttributes(
            [
                new KeyValuePair<string, object?>(
                    "tenantId",
                    tenantId),
            ]));
    }
}
```

Returning `MissingRequiredData()` denies authorization and stops the remaining
pipeline. It is appropriate when the provider owns data required for a safe
decision but cannot resolve it.

## Resource provider example

Resource providers can load ownership or classification data by using the
mapped resource identifier:

```csharp
public sealed class DocumentAttributeProvider
    : IRuleGateResourceAttributeProvider
{
    private readonly IDocumentAuthorizationReader _documents;

    public DocumentAttributeProvider(
        IDocumentAuthorizationReader documents)
    {
        _documents = documents;
    }

    public async ValueTask<RuleGateAttributeProviderResult>
        ProvideAttributesAsync(
            RuleGateAttributeProviderContext context,
            CancellationToken cancellationToken = default)
    {
        if (context.Resource.Id is null)
        {
            return RuleGateAttributeProviderResult
                .MissingRequiredData();
        }

        var document = await _documents.FindAsync(
            context.Resource.Id,
            cancellationToken);

        if (document is null)
        {
            return RuleGateAttributeProviderResult
                .MissingRequiredData();
        }

        return RuleGateAttributeProviderResult.Success(
            new AuthorizationAttributes(
            [
                new KeyValuePair<string, object?>(
                    "ownerId",
                    document.OwnerId),
                new KeyValuePair<string, object?>(
                    "classification",
                    document.Classification),
            ]));
    }
}
```

Use read models that enforce the same tenant and data-isolation rules as the
protected operation.

## Context provider example

Context providers supply trusted request facts such as canonical channel,
network zone, MFA time, or device state:

```csharp
using Fotbiler.RuleGate.Abstractions.Authorization;

public sealed class RequestContextAttributeProvider
    : IRuleGateContextAttributeProvider
{
    private readonly IRequestTrustEvaluator _trustEvaluator;

    public RequestContextAttributeProvider(
        IRequestTrustEvaluator trustEvaluator)
    {
        _trustEvaluator = trustEvaluator;
    }

    public async ValueTask<RuleGateAttributeProviderResult>
        ProvideAttributesAsync(
            RuleGateAttributeProviderContext context,
            CancellationToken cancellationToken = default)
    {
        var trustedContext = await _trustEvaluator.EvaluateAsync(
            context.HttpContext,
            cancellationToken);

        if (trustedContext is null)
        {
            return RuleGateAttributeProviderResult
                .MissingRequiredData();
        }

        return RuleGateAttributeProviderResult.Success(
            new AuthorizationAttributes(
            [
                new KeyValuePair<string, object?>(
                    AuthorizationContextAttributeNames.RequestChannel,
                    trustedContext.Channel),
                new KeyValuePair<string, object?>(
                    AuthorizationContextAttributeNames.TrustedDevice,
                    trustedContext.IsTrustedDevice),
            ]));
    }
}
```

Do not copy an arbitrary header, query value, remote address, or client device
assertion into authorization context. Validate and normalize it through a
trusted server-side component first.

## Collision and precedence rules

Each provider explicitly controls collisions through
`RuleGateAttributeCollisionBehavior`:

| Behavior          | Result                                                      |
| ----------------- | ----------------------------------------------------------- |
| `Fail`            | A duplicate key fails closed and stops the pipeline         |
| `KeepExisting`    | The earlier value remains; the new duplicate is ignored     |
| `ReplaceExisting` | The later provider replaces the value for the duplicate key |

Existing attributes created by the subject or resource factory are treated as
earlier values. A lower-order provider also precedes a higher-order provider.

The default `Fail` behavior prevents registration order from silently changing
authorization data. Use `KeepExisting` or `ReplaceExisting` only when the
precedence rule is intentional and covered by tests.

Attribute names use ordinal, case-sensitive comparison. `tenantId` and
`TenantId` are different keys.

## Result and failure behavior

A provider returns one of these results:

| Result                  | Behavior                                       |
| ----------------------- | ---------------------------------------------- |
| `Success(attributes)`   | Valid attributes are merged and work continues |
| `Success()`             | No attributes are added and work continues     |
| `MissingRequiredData()` | Authorization fails closed                     |
| `Fail()`                | Authorization fails closed                     |

RuleGate also fails closed when:

- A provider throws an exception
- Request cancellation is signaled
- A provider returns an unsupported attribute value
- An attribute name is empty or whitespace
- A collision uses the default `Fail` behavior
- A custom request enricher throws or returns failure

When enrichment fails, the authorization engine is not invoked and ASP.NET
Core authorization fails.

Supported attribute values follow the normal RuleGate attribute model:

- `null`
- `string`
- `bool`
- Integer and decimal numeric values
- `DateTimeOffset`
- Homogeneous scalar collections with at most 256 items

Nested collections, dictionaries, mixed collections, null collection elements,
and unsupported runtime types are rejected.

## Cancellation

HTTP authorization uses `HttpContext.RequestAborted`. The token is passed to
each provider and then to the authorization engine.

Providers must pass the token to database, HTTP, and other asynchronous calls.
RuleGate executes providers sequentially and stops before starting another
provider after cancellation.

## Diagnostics

Calling `AddLoggingDiagnostics()` enables enrichment events in addition to
authorization evaluation events:

```csharp
builder.Services
    .AddRuleGate()
    .AddLoggingDiagnostics();
```

Enrichment diagnostics contain only:

- Provider type name
- Attribute source
- Order
- Collision behavior
- Outcome
- Attribute count
- Duration

They do not contain attribute names, attribute values, exception messages, or
provider result payloads. Custom sinks implement
`IRuleGateEnrichmentDiagnosticsSink` and must preserve the same security
boundary.

For event IDs and logging configuration, read the
[diagnostics guide](diagnostics.md).

## Testing guidance

Test every provider for:

- Trusted-data success
- Missing-data denial
- Provider exceptions
- Cancellation
- Duplicate keys
- Unsupported values
- Tenant and organization isolation
- The exact precedence behavior selected by the provider

At least one integration test should exercise the provider through ASP.NET
Core authorization rather than invoking the provider directly.

For the wider trust model, read the [security guide](security.md).
