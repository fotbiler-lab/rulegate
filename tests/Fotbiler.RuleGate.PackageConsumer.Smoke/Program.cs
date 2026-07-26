using Fotbiler.RuleGate.Abstractions.Authorization;
using Fotbiler.RuleGate.Abstractions.Constants;
using Fotbiler.RuleGate.Core.Engine;
using Fotbiler.RuleGate.Core.Evaluation;
using Fotbiler.RuleGate.Core.Evaluation.Evaluators;
using Fotbiler.RuleGate.Core.Policies;
using Fotbiler.RuleGate.Manifest.Compilation;

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
        new InMemoryPolicyProvider(
            compilation.Policies),
        dispatcher);

var allowedDecision =
    await engine.EvaluateAsync(
        CreateRequest(
            permissions:
            [
                "package.read"
            ]));

if (!allowedDecision.IsAllowed ||
    allowedDecision.Failures.Count != 0)
{
    throw new InvalidOperationException(
        "The package-based authorization engine did not allow the valid request.");
}

var deniedDecision =
    await engine.EvaluateAsync(
        CreateRequest());

if (deniedDecision.IsAllowed)
{
    throw new InvalidOperationException(
        "The package-based authorization engine allowed an invalid request.");
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
