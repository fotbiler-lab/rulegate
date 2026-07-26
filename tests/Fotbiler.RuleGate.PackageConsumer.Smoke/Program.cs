using Fotbiler.RuleGate.Abstractions.Authorization;
using Fotbiler.RuleGate.Abstractions.Constants;
using Fotbiler.RuleGate.AspNetCore.DependencyInjection;
using Fotbiler.RuleGate.Manifest.Compilation;
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

var allowedDecision =
    await firstEngine.EvaluateAsync(
        CreateRequest(
            permissions:
            [
                "package.read"
            ]));

if (!allowedDecision.IsAllowed ||
    allowedDecision.Failures.Count != 0)
{
    throw new InvalidOperationException(
        "The DI-based authorization engine did not allow the valid request.");
}

var deniedDecision =
    await firstEngine.EvaluateAsync(
        CreateRequest());

if (deniedDecision.IsAllowed)
{
    throw new InvalidOperationException(
        "The DI-based authorization engine allowed an invalid request.");
}

var failure =
    deniedDecision.Failures.Single();

if (failure.Code !=
        AuthorizationFailureCodes.MissingPermission ||
    failure.RequirementId !=
        "required-package-permission")
{
    throw new InvalidOperationException(
        "The denied decision did not contain the expected authorization failure.");
}

Console.WriteLine(
    "RULEGATE_PACKAGE_CONSUMER_SMOKE_OK");

static AuthorizationRequest CreateRequest(
    IEnumerable<string>? permissions = null)
{
    return new AuthorizationRequest(
        subject:
            new AuthorizationSubject(
                id: "package-user",
                permissions: permissions),
        resource:
            new AuthorizationResource(
                type: "package-resource",
                id: "package-resource-1"),
        action: "read",
        context:
            new AuthorizationContext(
                DateTimeOffset.UnixEpoch));
}
