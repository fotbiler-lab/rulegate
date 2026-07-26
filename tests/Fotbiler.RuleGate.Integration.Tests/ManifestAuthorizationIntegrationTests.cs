using Fotbiler.RuleGate.Abstractions.Authorization;
using Fotbiler.RuleGate.Abstractions.Constants;
using Fotbiler.RuleGate.Core.Engine;
using Fotbiler.RuleGate.Core.Evaluation;
using Fotbiler.RuleGate.Core.Evaluation.Evaluators;
using Fotbiler.RuleGate.Core.Policies;
using Fotbiler.RuleGate.Manifest.Compilation;
using Fotbiler.RuleGate.Manifest.Loading;
using Fotbiler.RuleGate.Manifest.Validation;

namespace Fotbiler.RuleGate.Integration.Tests;

public sealed class ManifestAuthorizationIntegrationTests
{
    [Fact]
    public async Task EvaluateAsync_AllowsPermissionFromCompiledManifest()
    {
        var engine = await CreateEngineAsync();

        var decision = await engine.EvaluateAsync(
            CreateRequest(
                resourceType: "sample-resource",
                action: "read",
                permissions:
                [
                    "sample.read"
                ]));

        Assert.True(decision.IsAllowed);
        Assert.Empty(decision.Failures);
    }

    [Fact]
    public async Task EvaluateAsync_DeniesMissingPermissionFromCompiledManifest()
    {
        var engine = await CreateEngineAsync();

        var decision = await engine.EvaluateAsync(
            CreateRequest(
                resourceType: "sample-resource",
                action: "read"));

        Assert.False(decision.IsAllowed);

        var failure =
            Assert.Single(decision.Failures);

        Assert.Equal(
            AuthorizationFailureCodes.MissingPermission,
            failure.Code);

        Assert.Equal(
            "required-permission",
            failure.RequirementId);
    }

    [Fact]
    public async Task EvaluateAsync_AllowsRoleFromCompiledManifest()
    {
        var engine = await CreateEngineAsync();

        var decision = await engine.EvaluateAsync(
            CreateRequest(
                resourceType: "sample-resource",
                action: "update",
                roles:
                [
                    "sample.editor"
                ]));

        Assert.True(decision.IsAllowed);
        Assert.Empty(decision.Failures);
    }

    [Fact]
    public async Task EvaluateAsync_AllowsNestedRequirementsWhenNegatedRoleIsAbsent()
    {
        var engine = await CreateEngineAsync();

        var decision = await engine.EvaluateAsync(
            CreateRequest(
                resourceType: "secure-resource",
                action: "manage",
                permissions:
                [
                    "secure.manage"
                ]));

        Assert.True(decision.IsAllowed);
        Assert.Empty(decision.Failures);
    }

    [Fact]
    public async Task EvaluateAsync_DeniesNestedRequirementsWhenNegatedRoleIsPresent()
    {
        var engine = await CreateEngineAsync();

        var decision = await engine.EvaluateAsync(
            CreateRequest(
                resourceType: "secure-resource",
                action: "manage",
                roles:
                [
                    "sample.blocked"
                ],
                permissions:
                [
                    "secure.manage"
                ]));

        Assert.False(decision.IsAllowed);

        Assert.Contains(
            decision.Failures,
            failure =>
                failure.Code ==
                    AuthorizationFailureCodes
                        .NegatedRequirementSatisfied &&
                failure.RequirementId ==
                    "not-blocked");
    }

    [Fact]
    public async Task EvaluateAsync_DeniesWhenNoCompiledPolicyMatches()
    {
        var engine = await CreateEngineAsync();

        var decision = await engine.EvaluateAsync(
            CreateRequest(
                resourceType: "another-resource",
                action: "read"));

        Assert.False(decision.IsAllowed);

        Assert.Equal(
            AuthorizationFailureCodes.NoMatchingPolicy,
            Assert.Single(decision.Failures).Code);
    }

    [Fact]
    public async Task EvaluateAsync_UsesCaseSensitiveCompiledPolicyRoutes()
    {
        var engine = await CreateEngineAsync();

        var resourceTypeMismatch =
            await engine.EvaluateAsync(
                CreateRequest(
                    resourceType: "Sample-Resource",
                    action: "read",
                    permissions:
                    [
                        "sample.read"
                    ]));

        var actionMismatch =
            await engine.EvaluateAsync(
                CreateRequest(
                    resourceType: "sample-resource",
                    action: "Read",
                    permissions:
                    [
                        "sample.read"
                    ]));

        Assert.False(
            resourceTypeMismatch.IsAllowed);

        Assert.False(actionMismatch.IsAllowed);

        Assert.Equal(
            AuthorizationFailureCodes.NoMatchingPolicy,
            Assert.Single(
                resourceTypeMismatch.Failures).Code);

        Assert.Equal(
            AuthorizationFailureCodes.NoMatchingPolicy,
            Assert.Single(
                actionMismatch.Failures).Code);
    }

    [Fact]
    public void CompileFromText_InvalidYamlDoesNotProducePolicies()
    {
        const string yaml = """
            schemaVersion: [1
            """;

        var result =
            new RuleGateManifestCompiler()
                .CompileFromText(yaml);

        Assert.False(result.IsSuccess);
        Assert.Empty(result.Policies);
        Assert.Empty(result.ValidationErrors);

        Assert.Equal(
            ManifestLoadCodes.InvalidYaml,
            Assert.Single(result.LoadErrors).Code);
    }

    [Fact]
    public void CompileFromText_InvalidManifestDoesNotProducePolicies()
    {
        const string yaml = """
            schemaVersion: 999

            application:
              id: invalid-example
              name: Invalid Example

            policies: []
            """;

        var result =
            new RuleGateManifestCompiler()
                .CompileFromText(yaml);

        Assert.False(result.IsSuccess);
        Assert.Empty(result.Policies);
        Assert.Empty(result.LoadErrors);

        Assert.Equal(
            ManifestValidationCodes
                .UnsupportedSchemaVersion,
            Assert.Single(
                result.ValidationErrors).Code);
    }

    private static async Task<
        PolicyAuthorizationEngine> CreateEngineAsync()
    {
        var compiler =
            new RuleGateManifestCompiler();

        var path = Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "rulegate.integration.yaml");

        var compilation =
            await compiler.CompileFromFileAsync(path);

        Assert.True(compilation.IsSuccess);
        Assert.Empty(compilation.LoadErrors);
        Assert.Empty(
            compilation.ValidationErrors);

        return new PolicyAuthorizationEngine(
            new InMemoryPolicyProvider(
                compilation.Policies),
            CreateDispatcher());
    }

    private static RequirementEvaluationDispatcher
        CreateDispatcher()
    {
        return new RequirementEvaluationDispatcher(
        [
            new PermissionRequirementEvaluator(),
            new RoleRequirementEvaluator(),
            new AllRequirementEvaluator(),
            new AnyRequirementEvaluator(),
            new NotRequirementEvaluator()
        ]);
    }

    private static AuthorizationRequest CreateRequest(
        string resourceType,
        string action,
        IEnumerable<string>? roles = null,
        IEnumerable<string>? permissions = null)
    {
        return new AuthorizationRequest(
            subject:
                new AuthorizationSubject(
                    id: "user-1",
                    roles: roles,
                    permissions: permissions),
            resource:
                new AuthorizationResource(
                    type: resourceType,
                    id: "resource-1"),
            action: action,
            context:
                new AuthorizationContext(
                    DateTimeOffset.UnixEpoch));
    }
}
