using System.Security.Claims;
using Fotbiler.RuleGate.Abstractions.Authorization;
using Fotbiler.RuleGate.Abstractions.Constants;
using Fotbiler.RuleGate.AspNetCore.Authorization;
using Fotbiler.RuleGate.AspNetCore.DependencyInjection;
using Fotbiler.RuleGate.AspNetCore.Subjects;
using Fotbiler.RuleGate.Manifest.Compilation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

const string PolicyName =
    "package-resource-read";

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

services.AddAuthorizationCore(
    options =>
    {
        options.AddPolicy(
            PolicyName,
            policy =>
            {
                policy.AddRequirements(
                    new RuleGateAuthorizationRequirement(
                        action: "read"));
            });
    });

services
    .AddRuleGate()
    .AddPolicies(compilation.Policies);

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
    await authorizationService.AuthorizeAsync(
        allowedPrincipal,
        resource,
        PolicyName);

if (!frameworkAllowedResult.Succeeded)
{
    throw new InvalidOperationException(
        "The ASP.NET Core authorization handler did not allow the valid request.");
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
    await authorizationService.AuthorizeAsync(
        deniedPrincipal,
        resource,
        PolicyName);

if (frameworkDeniedResult.Succeeded)
{
    throw new InvalidOperationException(
        "The ASP.NET Core authorization handler allowed the invalid request.");
}

var unsupportedResourceResult =
    await authorizationService.AuthorizeAsync(
        allowedPrincipal,
        new object(),
        PolicyName);

if (unsupportedResourceResult.Succeeded)
{
    throw new InvalidOperationException(
        "The ASP.NET Core authorization handler accepted an unsupported resource.");
}

var missingSubjectResult =
    await authorizationService.AuthorizeAsync(
        new ClaimsPrincipal(
            new ClaimsIdentity(
                authenticationType:
                    "PackageConsumer")),
        resource,
        PolicyName);

if (missingSubjectResult.Succeeded)
{
    throw new InvalidOperationException(
        "The ASP.NET Core authorization handler accepted a principal without a subject identifier.");
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
