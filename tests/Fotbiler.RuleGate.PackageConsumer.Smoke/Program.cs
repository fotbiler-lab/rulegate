using System.Security.Claims;
using Fotbiler.RuleGate.Abstractions.Attributes;
using Fotbiler.RuleGate.Abstractions.Authorization;
using Fotbiler.RuleGate.Abstractions.Constants;
using Fotbiler.RuleGate.Abstractions.Policies;
using Fotbiler.RuleGate.AspNetCore.Authorization;
using Fotbiler.RuleGate.AspNetCore.DependencyInjection;
using Fotbiler.RuleGate.AspNetCore.Endpoints;
using Fotbiler.RuleGate.AspNetCore.Subjects;
using Fotbiler.RuleGate.Manifest.Compilation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;

const string yaml = """
    schemaVersion: 1

    application:
      id: package-consumer-smoke
      name: Package Consumer Smoke

    policies:
      - id: package-resource-read
        resourceType: package-resource
        action: read
        requirement:
          id: required-package-permission
          permission: package.read

      - id: attribute-resource-read
        resourceType: attribute-resource
        action: read
        requirement:
          id: finance-department
          attribute:
            source: subject
            name: department
            operator: equal
            valueType: string
            value: finance

      - id: advanced-attribute-access
        resourceType: advanced-attribute-resource
        action: access
        requirement:
          all:
            - attribute:
                source: subject
                name: department
                operator: startsWith
                stringComparison: ordinalIgnoreCase
                valueType: string
                value: finance
            - attribute:
                source: subject
                name: scopes
                operator: containsAll
                valueType: stringCollection
                value:
                  - document.read
                  - document.approve
            - attribute:
                source: context
                name: blocked
                operator: notExists

      - id: owned-resource-update
        resourceType: owned-resource
        action: update
        requirement:
          id: resource-owner
          attributeComparison:
            left:
              source: resource
              name: ownerId
            operator: equal
            right:
              source: subject
              name: id

      - id: secure-context-access
        resourceType: secure-context-resource
        action: access
        requirement:
          all:
            - timeWindow:
                days: [thursday]
                start: "00:00"
                end: "01:00"
                timeZone: UTC
            - dateTimeWindow:
                startsAt: "1969-12-31T00:00:00Z"
                endsAt: "1970-01-02T00:00:00Z"
            - contextAge:
                timestamp: authentication
                maximumAge: "00:05:00"
            - context:
                property: requestChannel
                operator: equal
                valueType: string
                value: api
            - context:
                property: trustedDevice
                operator: equal
                valueType: boolean
                value: true
    """;

var compiler =
    new RuleGateManifestCompiler();

var compilation =
    compiler.CompileFromText(yaml);

if (!compilation.IsSuccess)
{
    throw new InvalidOperationException(
        "The package consumer manifest could not be compiled.");
}

var services =
    new ServiceCollection();

services.AddLogging();

services.AddAuthorizationCore();

services
    .AddRuleGate()
    .AddLoggingDiagnostics()
    .AddHttpAuthorizationResultMapping()
    .AddPolicies(compilation.Policies);

using var serviceProvider =
    services.BuildServiceProvider(
        new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });

var authorizationResultHandler =
    serviceProvider.GetRequiredService<
        IAuthorizationMiddlewareResultHandler>();

if (authorizationResultHandler.GetType().Name !=
    "RuleGateAuthorizationMiddlewareResultHandler")
{
    throw new InvalidOperationException(
        "The packaged HTTP authorization result mapping was not registered.");
}

var firstEngine =
    serviceProvider.GetRequiredService<
        IAuthorizationEngine>();

var secondEngine =
    serviceProvider.GetRequiredService<
        IAuthorizationEngine>();

if (!ReferenceEquals(firstEngine, secondEngine))
{
    throw new InvalidOperationException(
        "The authorization engine was not registered as a singleton.");
}

var firstSubjectFactory =
    serviceProvider.GetRequiredService<
        IRuleGateSubjectFactory>();

var secondSubjectFactory =
    serviceProvider.GetRequiredService<
        IRuleGateSubjectFactory>();

if (!ReferenceEquals(
        firstSubjectFactory,
        secondSubjectFactory))
{
    throw new InvalidOperationException(
        "The RuleGate subject factory was not registered as a singleton.");
}

var authorizationService =
    serviceProvider.GetRequiredService<
        IAuthorizationService>();

var resource =
    new AuthorizationResource(
        type: "package-resource",
        id: "package-resource-1");

var allowedPrincipal =
    CreatePrincipal(
        roles:
        [
            "package.reader",
        ],
        permissions:
        [
            "package.read",
        ]);

var allowedSubject =
    firstSubjectFactory.Create(
        allowedPrincipal);

if (allowedSubject.Id != "package-user" ||
    !allowedSubject.Roles.Contains(
        "package.reader") ||
    !allowedSubject.Permissions.Contains(
        "package.read"))
{
    throw new InvalidOperationException(
        "The claims principal was not mapped to the expected authorization subject.");
}

var directAllowedDecision =
    await firstEngine.EvaluateAsync(
        CreateRequest(
            allowedSubject,
            resource));

if (!directAllowedDecision.IsAllowed ||
    directAllowedDecision.Failures.Count != 0)
{
    throw new InvalidOperationException(
        "The RuleGate engine did not allow the valid direct request.");
}

var frameworkAllowedResult =
    await authorizationService.AuthorizeRuleGateAsync(
        allowedPrincipal,
        resource,
        action: "read");

if (!frameworkAllowedResult.Succeeded)
{
    throw new InvalidOperationException(
        "The ASP.NET Core authorization handler did not allow the valid request.");
}

var mismatchedResourceResult =
    await authorizationService.AuthorizeRuleGateAsync(
        allowedPrincipal,
        new AuthorizationResource(
            type: "invoice",
            id: "invoice-1"),
        resourceType: "package-resource",
        action: "read");

if (mismatchedResourceResult.Succeeded)
{
    throw new InvalidOperationException(
        "The dynamic RuleGate policy accepted a mismatched resource type.");
}

var deniedPrincipal =
    CreatePrincipal();

var deniedSubject =
    firstSubjectFactory.Create(
        deniedPrincipal);

var directDeniedDecision =
    await firstEngine.EvaluateAsync(
        CreateRequest(
            deniedSubject,
            resource));

if (directDeniedDecision.IsAllowed)
{
    throw new InvalidOperationException(
        "The RuleGate engine allowed the invalid direct request.");
}

var failure =
    directDeniedDecision.Failures.Single();

if (failure.Code !=
        AuthorizationFailureCodes.MissingPermission ||
    failure.RequirementId !=
        "required-package-permission")
{
    throw new InvalidOperationException(
        "The denied decision did not contain the expected authorization failure.");
}

var frameworkDeniedResult =
    await authorizationService.AuthorizeRuleGateAsync(
        deniedPrincipal,
        resource,
        action: "read");

if (frameworkDeniedResult.Succeeded)
{
    throw new InvalidOperationException(
        "The ASP.NET Core authorization handler allowed the invalid request.");
}

var unsupportedResourceResult =
    await authorizationService.AuthorizeRuleGateAsync(
        allowedPrincipal,
        new object(),
        resourceType: "package-resource",
        action: "read");

if (unsupportedResourceResult.Succeeded)
{
    throw new InvalidOperationException(
        "The ASP.NET Core authorization handler accepted an unsupported resource.");
}

var missingSubjectResult =
    await authorizationService.AuthorizeRuleGateAsync(
        new ClaimsPrincipal(
            new ClaimsIdentity(
                authenticationType:
                    "PackageConsumer")),
        resource,
        action: "read");

if (missingSubjectResult.Succeeded)
{
    throw new InvalidOperationException(
        "The ASP.NET Core authorization handler accepted a principal without a subject identifier.");
}

var attributeResource =
    new AuthorizationResource(
        type: "attribute-resource",
        id: "attribute-resource-1");

var matchingAttributeSubject =
    new AuthorizationSubject(
        id: "attribute-user",
        attributes:
            new AuthorizationAttributes(
            [
                new KeyValuePair<string, object?>(
                    "department",
                    "finance")
            ]));

var attributeAllowedDecision =
    await firstEngine.EvaluateAsync(
        CreateRequest(
            matchingAttributeSubject,
            attributeResource));

if (!attributeAllowedDecision.IsAllowed ||
    attributeAllowedDecision.Failures.Count != 0)
{
    throw new InvalidOperationException(
        "The packaged attribute evaluator did not allow the matching subject attribute.");
}

var attributeDeniedDecision =
    await firstEngine.EvaluateAsync(
        CreateRequest(
            new AuthorizationSubject(
                id: "attribute-user"),
            attributeResource));

if (attributeDeniedDecision.IsAllowed)
{
    throw new InvalidOperationException(
        "The packaged attribute evaluator allowed a subject without the required attribute.");
}

var attributeFailure =
    attributeDeniedDecision.Failures.Single();

if (attributeFailure.Code !=
        AuthorizationFailureCodes.AttributeNotFound ||
    attributeFailure.RequirementId !=
        "finance-department")
{
    throw new InvalidOperationException(
        "The packaged attribute evaluator did not return the expected failure.");
}

var advancedAttributeSubject =
    new AuthorizationSubject(
        id: "advanced-attribute-user",
        attributes:
            new AuthorizationAttributes(
            [
                new KeyValuePair<string, object?>(
                    "department",
                    "Finance-Europe"),
                new KeyValuePair<string, object?>(
                    "scopes",
                    new[]
                    {
                        "document.approve",
                        "document.read",
                        "document.archive"
                    })
            ]));

var advancedAttributeResource =
    new AuthorizationResource(
        type: "advanced-attribute-resource",
        id: "advanced-attribute-resource-1");

var advancedAttributeDecision =
    await firstEngine.EvaluateAsync(
        CreateRequest(
            advancedAttributeSubject,
            advancedAttributeResource,
            action: "access"));

if (!advancedAttributeDecision.IsAllowed ||
    advancedAttributeDecision.Failures.Count != 0)
{
    throw new InvalidOperationException(
        "The packaged advanced attribute operators did not allow the valid request.");
}

var ownerSubject =
    new AuthorizationSubject(
        id: "owner-user",
        attributes:
            new AuthorizationAttributes(
            [
                new KeyValuePair<string, object?>(
                    "id",
                    "owner-user")
            ]));

var ownedResource =
    new AuthorizationResource(
        type: "owned-resource",
        id: "owned-resource-1",
        attributes:
            new AuthorizationAttributes(
            [
                new KeyValuePair<string, object?>(
                    "ownerId",
                    "owner-user")
            ]));

var ownershipDecision =
    await firstEngine.EvaluateAsync(
        CreateRequest(
            ownerSubject,
            ownedResource,
            action: "update"));

if (!ownershipDecision.IsAllowed)
{
    throw new InvalidOperationException(
        "The packaged attribute-comparison evaluator did not allow the matching owner.");
}

var secureContextDecision = await firstEngine.EvaluateAsync(
    CreateRequest(
        new AuthorizationSubject("context-user"),
        new AuthorizationResource(
            "secure-context-resource",
            "secure-context-resource-1"),
        "access",
        new AuthorizationContext(
            DateTimeOffset.UnixEpoch,
            new AuthorizationAttributes(
            [
                new KeyValuePair<string, object?>(
                    AuthorizationContextAttributeNames.AuthenticationTime,
                    DateTimeOffset.UnixEpoch),
                new KeyValuePair<string, object?>(
                    AuthorizationContextAttributeNames.RequestChannel,
                    "api"),
                new KeyValuePair<string, object?>(
                    AuthorizationContextAttributeNames.TrustedDevice,
                    true)
            ]))));

if (!secureContextDecision.IsAllowed)
{
    throw new InvalidOperationException(
        "The packaged time and context evaluators did not allow the trusted request.");
}

var publicTimeRequirement = new TimeWindowRequirementDefinition(
    [DayOfWeek.Thursday],
    new TimeOnly(0, 0),
    new TimeOnly(1, 0),
    TimeZoneInfo.Utc);

var publicContextRequirement = new ContextRequirementDefinition(
    AuthorizationContextProperty.IdentityType,
    AuthorizationAttributeOperator.Equal,
    "service");

if (publicTimeRequirement.CrossesMidnight ||
    publicContextRequirement.AttributeName !=
        AuthorizationContextAttributeNames.IdentityType)
{
    throw new InvalidOperationException(
        "The packaged time and context public APIs did not preserve their contracts.");
}

var publicComparisonRequirement =
    new AttributeComparisonRequirementDefinition(
        AuthorizationAttributeOperand.Resource(
            "ownerId"),
        AuthorizationAttributeOperator.Equal,
        AuthorizationAttributeOperand.Subject("id"),
        id: "resource-owner");

if (publicComparisonRequirement.Left.Kind !=
        AuthorizationAttributeOperandKind.Resource ||
    publicComparisonRequirement.Right.Kind !=
        AuthorizationAttributeOperandKind.Subject)
{
    throw new InvalidOperationException(
        "The packaged attribute-comparison public API did not preserve its operand structure.");
}

var publicRequirement =
    new AttributeRequirementDefinition(
        AuthorizationAttributeSource.Subject,
        name: "department",
        AuthorizationAttributeOperator.Contains,
        value: "FINANCE",
        stringComparison:
            AuthorizationStringComparison
                .OrdinalIgnoreCase);

if (publicRequirement.StringComparison !=
        AuthorizationStringComparison
            .OrdinalIgnoreCase ||
    publicRequirement.Operator !=
        AuthorizationAttributeOperator.Contains)
{
    throw new InvalidOperationException(
        "The packaged advanced attribute public API did not preserve its configuration.");
}

var ruleGateAttribute =
    new RuleGateAuthorizeAttribute(
        resourceType: "package-resource",
        action: "read",
        resourceIdRouteValue: "id");

if (ruleGateAttribute.Policy !=
        "RuleGate:package-resource:read" ||
    ruleGateAttribute.ResourceType !=
        "package-resource" ||
    ruleGateAttribute.Action != "read" ||
    ruleGateAttribute.ResourceIdRouteValue !=
        "id")
{
    throw new InvalidOperationException(
        "The RuleGate authorization attribute did not expose the expected metadata.");
}

var endpointConventionBuilder =
    new RecordingEndpointConventionBuilder();

var returnedEndpointBuilder =
    endpointConventionBuilder.RequireRuleGate(
        resourceType: "package-resource",
        action: "read",
        resourceIdRouteValue: "id");

if (!ReferenceEquals(
        endpointConventionBuilder,
        returnedEndpointBuilder))
{
    throw new InvalidOperationException(
        "RequireRuleGate did not return the original endpoint builder.");
}

var routeEndpointBuilder =
    new RouteEndpointBuilder(
        requestDelegate:
            _ => Task.CompletedTask,
        routePattern:
            RoutePatternFactory.Parse(
                "/package-resources/{id}"),
        order: 0);

endpointConventionBuilder.ApplyTo(
    routeEndpointBuilder);

var endpointRuleGateMetadata =
    routeEndpointBuilder.Metadata
        .OfType<
            IRuleGateAuthorizationMetadata>()
        .Single();

if (endpointRuleGateMetadata.ResourceType !=
        "package-resource" ||
    endpointRuleGateMetadata.Action !=
        "read" ||
    endpointRuleGateMetadata
        .ResourceIdRouteValue != "id")
{
    throw new InvalidOperationException(
        "RequireRuleGate did not attach the expected RuleGate endpoint metadata.");
}

var endpointAuthorizeData =
    routeEndpointBuilder.Metadata
        .OfType<IAuthorizeData>()
        .Single();

if (endpointAuthorizeData.Policy !=
    "RuleGate:package-resource:read")
{
    throw new InvalidOperationException(
        "RequireRuleGate did not attach the expected dynamic authorization policy.");
}

var endpoint =
    new Endpoint(
        requestDelegate:
            _ => Task.CompletedTask,
        metadata:
            new EndpointMetadataCollection(
                routeEndpointBuilder
                    .Metadata
                    .ToArray()),
        displayName:
            "RuleGate package consumer endpoint");

var endpointHttpContext =
    new DefaultHttpContext();

endpointHttpContext.SetEndpoint(endpoint);

endpointHttpContext.Request
    .RouteValues["id"] =
        "package-resource-1";

var endpointResource =
    new RuleGateAuthorizationResourceFactory()
        .Create(
            endpointHttpContext,
            new RuleGateAuthorizationRequirement(
                resourceType:
                    "package-resource",
                action:
                    "read"));

if (endpointResource.Type !=
        "package-resource" ||
    endpointResource.Id !=
        "package-resource-1")
{
    throw new InvalidOperationException(
        "The default RuleGate HTTP resource factory did not map the endpoint route value.");
}

Console.WriteLine(
    "RULEGATE_PACKAGE_CONSUMER_SMOKE_OK");

static ClaimsPrincipal CreatePrincipal(
    IEnumerable<string>? roles = null,
    IEnumerable<string>? permissions = null)
{
    var claims =
        new List<Claim>
        {
            new(
                ClaimTypes.NameIdentifier,
                "package-user"),
        };

    foreach (var role in roles ?? [])
    {
        claims.Add(
            new Claim(
                ClaimTypes.Role,
                role));
    }

    foreach (var permission in permissions ?? [])
    {
        claims.Add(
            new Claim(
                RuleGateSubjectOptions
                    .DefaultPermissionClaimType,
                permission));
    }

    return new ClaimsPrincipal(
        new ClaimsIdentity(
            claims,
            authenticationType:
                "PackageConsumer"));
}

static AuthorizationRequest CreateRequest(
    AuthorizationSubject subject,
    AuthorizationResource resource,
    string action = "read",
    AuthorizationContext? context = null)
{
    return new AuthorizationRequest(
        subject: subject,
        resource: resource,
        action,
        context:
            context ?? new AuthorizationContext(
                DateTimeOffset.UnixEpoch));
}

internal sealed class
    RecordingEndpointConventionBuilder
    : IEndpointConventionBuilder
{
    public List<Action<EndpointBuilder>>
        Conventions
    { get; } = [];

    public void Add(
        Action<EndpointBuilder> convention)
    {
        ArgumentNullException.ThrowIfNull(
            convention);

        Conventions.Add(convention);
    }

    public void ApplyTo(
        EndpointBuilder endpointBuilder)
    {
        ArgumentNullException.ThrowIfNull(
            endpointBuilder);

        foreach (var convention in Conventions)
        {
            convention(endpointBuilder);
        }
    }
}
