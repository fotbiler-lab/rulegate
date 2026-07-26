using Fotbiler.RuleGate.Abstractions.Authorization;
using Fotbiler.RuleGate.Abstractions.Constants;
using Fotbiler.RuleGate.Abstractions.Evaluation;
using Fotbiler.RuleGate.Abstractions.Policies;
using Fotbiler.RuleGate.Core.Engine;
using Fotbiler.RuleGate.Core.Evaluation;
using Fotbiler.RuleGate.Core.Evaluation.Evaluators;
using Fotbiler.RuleGate.Core.Policies;

namespace Fotbiler.RuleGate.Core.Tests;

public sealed class PolicyAuthorizationEngineTests
{
    [Fact]
    public async Task EvaluateAsync_AllowsWhenRequirementIsSatisfied()
    {
        var engine = CreateEngine(
            new PermissionRequirementDefinition(
                "sample.read"));

        var decision = await engine.EvaluateAsync(
            CreateRequest(
                permissions:
                [
                    "sample.read"
                ]));

        Assert.True(decision.IsAllowed);
        Assert.Empty(decision.Failures);
    }

    [Fact]
    public async Task EvaluateAsync_DeniesWhenRequirementIsNotSatisfied()
    {
        var engine = CreateEngine(
            new PermissionRequirementDefinition(
                permission: "sample.read",
                id: "required-permission"));

        var decision = await engine.EvaluateAsync(
            CreateRequest());

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
    public async Task EvaluateAsync_DeniesWhenNoPolicyMatches()
    {
        var engine = CreateEngine(
            new PermissionRequirementDefinition(
                "sample.read"));

        var decision = await engine.EvaluateAsync(
            CreateRequest(
                resourceType: "another-resource"));

        Assert.False(decision.IsAllowed);

        Assert.Equal(
            AuthorizationFailureCodes.NoMatchingPolicy,
            Assert.Single(decision.Failures).Code);
    }

    [Fact]
    public async Task EvaluateAsync_DeniesIndeterminateResult()
    {
        var engine = CreateEngine(
            new UnsupportedRequirementDefinition());

        var decision = await engine.EvaluateAsync(
            CreateRequest());

        Assert.False(decision.IsAllowed);

        Assert.Equal(
            AuthorizationFailureCodes
                .RequirementEvaluatorNotFound,
            Assert.Single(decision.Failures).Code);
    }

    [Fact]
    public async Task EvaluateAsync_RejectsNullRequest()
    {
        var engine = CreateEngine(
            new PermissionRequirementDefinition(
                "sample.read"));

        await Assert.ThrowsAsync<
            ArgumentNullException>(
            async () =>
                await engine.EvaluateAsync(null!));
    }

    [Fact]
    public async Task EvaluateAsync_HonorsCancellation()
    {
        var engine = CreateEngine(
            new PermissionRequirementDefinition(
                "sample.read"));

        using var cancellation =
            new CancellationTokenSource();

        cancellation.Cancel();

        await Assert.ThrowsAsync<
            OperationCanceledException>(
            async () =>
                await engine.EvaluateAsync(
                    CreateRequest(),
                    cancellation.Token));
    }

    [Fact]
    public void Constructor_RejectsNullPolicyProvider()
    {
        var dispatcher = CreateDispatcher();

        Assert.Throws<ArgumentNullException>(
            () => new PolicyAuthorizationEngine(
                null!,
                dispatcher));
    }

    [Fact]
    public void Constructor_RejectsNullRequirementDispatcher()
    {
        var provider =
            new InMemoryPolicyProvider([]);

        Assert.Throws<ArgumentNullException>(
            () => new PolicyAuthorizationEngine(
                provider,
                null!));
    }

    private static PolicyAuthorizationEngine CreateEngine(
        RequirementDefinition requirement)
    {
        var policy = new PolicyDefinition(
            id: "sample-read",
            resourceType: "sample-resource",
            action: "read",
            requirement);

        return new PolicyAuthorizationEngine(
            new InMemoryPolicyProvider(
            [
                policy
            ]),
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
        string resourceType = "sample-resource",
        IEnumerable<string>? roles = null,
        IEnumerable<string>? permissions = null)
    {
        return new AuthorizationRequest(
            subject: new AuthorizationSubject(
                id: "user-1",
                roles: roles,
                permissions: permissions),
            resource: new AuthorizationResource(
                type: resourceType,
                id: "resource-1"),
            action: "read",
            context: new AuthorizationContext(
                DateTimeOffset.UtcNow));
    }

    private sealed record UnsupportedRequirementDefinition
        : RequirementDefinition;
}
