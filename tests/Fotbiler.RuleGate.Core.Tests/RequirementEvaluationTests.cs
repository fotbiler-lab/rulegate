using Fotbiler.RuleGate.Abstractions.Attributes;
using Fotbiler.RuleGate.Abstractions.Authorization;
using Fotbiler.RuleGate.Abstractions.Constants;
using Fotbiler.RuleGate.Abstractions.Evaluation;
using Fotbiler.RuleGate.Abstractions.Policies;
using Fotbiler.RuleGate.Core.Evaluation;
using Fotbiler.RuleGate.Core.Evaluation.Evaluators;

namespace Fotbiler.RuleGate.Core.Tests;

public sealed class RequirementEvaluationTests
{
    [Fact]
    public async Task PermissionRequirement_SucceedsWhenPermissionExists()
    {
        var result = await EvaluateAsync(
            new PermissionRequirementDefinition(
                "sample.read"),
            permissions:
            [
                "sample.read"
            ]);

        Assert.True(result.IsSatisfied);
    }

    [Fact]
    public async Task PermissionRequirement_FailsWhenPermissionIsMissing()
    {
        var result = await EvaluateAsync(
            new PermissionRequirementDefinition(
                "sample.read",
                id: "required-permission"));

        Assert.True(result.IsNotSatisfied);

        var failure = Assert.Single(result.Failures);

        Assert.Equal(
            AuthorizationFailureCodes.MissingPermission,
            failure.Code);

        Assert.Equal(
            "required-permission",
            failure.RequirementId);
    }

    [Fact]
    public async Task RoleRequirement_SucceedsWhenRoleExists()
    {
        var result = await EvaluateAsync(
            new RoleRequirementDefinition(
                "sample.editor"),
            roles:
            [
                "sample.editor"
            ]);

        Assert.True(result.IsSatisfied);
    }

    [Fact]
    public async Task RoleRequirement_FailsWhenRoleIsMissing()
    {
        var result = await EvaluateAsync(
            new RoleRequirementDefinition(
                "sample.editor"));

        Assert.True(result.IsNotSatisfied);

        Assert.Equal(
            AuthorizationFailureCodes.MissingRole,
            Assert.Single(result.Failures).Code);
    }

    [Fact]
    public async Task AllRequirement_SucceedsWhenEveryChildSucceeds()
    {
        var requirement = new AllRequirementDefinition(
        [
            new PermissionRequirementDefinition(
                "sample.read"),
            new RoleRequirementDefinition(
                "sample.editor")
        ]);

        var result = await EvaluateAsync(
            requirement,
            permissions:
            [
                "sample.read"
            ],
            roles:
            [
                "sample.editor"
            ]);

        Assert.True(result.IsSatisfied);
    }

    [Fact]
    public async Task AllRequirement_FailsWhenAnyChildFails()
    {
        var requirement = new AllRequirementDefinition(
        [
            new PermissionRequirementDefinition(
                "sample.read"),
            new RoleRequirementDefinition(
                "sample.editor")
        ]);

        var result = await EvaluateAsync(
            requirement,
            permissions:
            [
                "sample.read"
            ]);

        Assert.True(result.IsNotSatisfied);
        Assert.Single(result.Failures);
    }

    [Fact]
    public async Task AllRequirement_IsIndeterminateWhenChildCannotBeEvaluated()
    {
        var requirement = new AllRequirementDefinition(
        [
            new PermissionRequirementDefinition(
                "sample.read"),
            new UnsupportedRequirementDefinition()
        ]);

        var result = await EvaluateAsync(
            requirement,
            permissions:
            [
                "sample.read"
            ]);

        Assert.True(result.IsIndeterminate);
    }

    [Fact]
    public async Task AllRequirement_IsNotSatisfiedWhenOneChildFailsAndAnotherIsIndeterminate()
    {
        var requirement = new AllRequirementDefinition(
        [
            new PermissionRequirementDefinition(
                "sample.read"),
            new UnsupportedRequirementDefinition()
        ]);

        var result = await EvaluateAsync(requirement);

        Assert.True(result.IsNotSatisfied);
        Assert.Equal(2, result.Failures.Count);
    }

    [Fact]
    public async Task AnyRequirement_SucceedsWhenOneChildSucceeds()
    {
        var requirement = new AnyRequirementDefinition(
        [
            new PermissionRequirementDefinition(
                "sample.read"),
            new RoleRequirementDefinition(
                "sample.editor")
        ]);

        var result = await EvaluateAsync(
            requirement,
            roles:
            [
                "sample.editor"
            ]);

        Assert.True(result.IsSatisfied);
    }

    [Fact]
    public async Task AnyRequirement_SucceedsWhenOneChildIsIndeterminateAndAnotherSucceeds()
    {
        var requirement = new AnyRequirementDefinition(
        [
            new UnsupportedRequirementDefinition(),
            new RoleRequirementDefinition(
                "sample.editor")
        ]);

        var result = await EvaluateAsync(
            requirement,
            roles:
            [
                "sample.editor"
            ]);

        Assert.True(result.IsSatisfied);
        Assert.Empty(result.Failures);
    }

    [Fact]
    public async Task AnyRequirement_FailsWhenEveryChildFails()
    {
        var requirement = new AnyRequirementDefinition(
        [
            new PermissionRequirementDefinition(
                "sample.read"),
            new RoleRequirementDefinition(
                "sample.editor")
        ]);

        var result = await EvaluateAsync(requirement);

        Assert.True(result.IsNotSatisfied);
        Assert.Equal(2, result.Failures.Count);
    }

    [Fact]
    public async Task AnyRequirement_IsIndeterminateWhenNothingSucceedsAndChildIsUnknown()
    {
        var requirement = new AnyRequirementDefinition(
        [
            new PermissionRequirementDefinition(
                "sample.read"),
            new UnsupportedRequirementDefinition()
        ]);

        var result = await EvaluateAsync(requirement);

        Assert.True(result.IsIndeterminate);
    }

    [Fact]
    public async Task NotRequirement_SucceedsWhenChildIsNotSatisfied()
    {
        var requirement = new NotRequirementDefinition(
            new RoleRequirementDefinition(
                "sample.blocked"));

        var result = await EvaluateAsync(requirement);

        Assert.True(result.IsSatisfied);
    }

    [Fact]
    public async Task NotRequirement_FailsWhenChildIsSatisfied()
    {
        var requirement = new NotRequirementDefinition(
            new RoleRequirementDefinition(
                "sample.blocked"),
            id: "not-blocked");

        var result = await EvaluateAsync(
            requirement,
            roles:
            [
                "sample.blocked"
            ]);

        Assert.True(result.IsNotSatisfied);

        var failure = Assert.Single(result.Failures);

        Assert.Equal(
            AuthorizationFailureCodes
                .NegatedRequirementSatisfied,
            failure.Code);

        Assert.Equal(
            "not-blocked",
            failure.RequirementId);
    }

    [Fact]
    public async Task NotRequirement_DoesNotInvertIndeterminateResult()
    {
        var requirement = new NotRequirementDefinition(
            new UnsupportedRequirementDefinition());

        var result = await EvaluateAsync(requirement);

        Assert.True(result.IsIndeterminate);
    }

    [Fact]
    public async Task Dispatcher_ReturnsIndeterminateForUnknownRequirement()
    {
        var result = await EvaluateAsync(
            new UnsupportedRequirementDefinition());

        Assert.True(result.IsIndeterminate);

        Assert.Equal(
            AuthorizationFailureCodes
                .RequirementEvaluatorNotFound,
            Assert.Single(result.Failures).Code);
    }

    [Fact]
    public void Dispatcher_RejectsDuplicateEvaluators()
    {
        Assert.Throws<InvalidOperationException>(
            () => new RequirementEvaluationDispatcher(
            [
                new PermissionRequirementEvaluator(),
                new PermissionRequirementEvaluator()
            ]));
    }

    private static async Task<RequirementEvaluationResult>
        EvaluateAsync(
            RequirementDefinition requirement,
            IEnumerable<string>? roles = null,
            IEnumerable<string>? permissions = null)
    {
        var dispatcher = CreateDispatcher();

        var subject = new AuthorizationSubject(
            id: "user-1",
            roles: roles,
            permissions: permissions);

        var request = new AuthorizationRequest(
            subject,
            new AuthorizationResource(
                type: "sample-resource",
                id: "resource-1",
                attributes:
                    AuthorizationAttributes.Empty),
            action: "read",
            new AuthorizationContext(
                DateTimeOffset.UtcNow));

        return await dispatcher.EvaluateAsync(
            requirement,
            new RequirementEvaluationContext(request));
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

    private sealed record UnsupportedRequirementDefinition
        : RequirementDefinition;
}
