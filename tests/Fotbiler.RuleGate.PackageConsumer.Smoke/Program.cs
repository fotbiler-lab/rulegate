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
    .AddPolicies(compilation.Policies)
    .AddPolicy(
        new PolicyDefinition(
            id: "attribute-resource-read",
            resourceType: "attribute-resource",
            action: "read",
            requirement:
                new AttributeRequirementDefinition(
                    source:
                        AuthorizationAttributeSource
                            .Subject,
                    name: "department",
                    @operator:
                        AuthorizationAttributeOperator
                            .Equal,
                    value: "finance",
                    id: "finance-department")));

using var serviceProvider =
    services.BuildServiceProvider(
        new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });

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
    AuthorizationResource resource)
{
    return new AuthorizationRequest(
        subject: subject,
        resource: resource,
        action: "read",
        context:
            new AuthorizationContext(
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
