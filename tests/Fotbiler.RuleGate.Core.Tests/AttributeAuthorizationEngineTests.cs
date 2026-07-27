using Fotbiler.RuleGate.Abstractions.Attributes;
using Fotbiler.RuleGate.Abstractions.Authorization;
using Fotbiler.RuleGate.Abstractions.Constants;
using Fotbiler.RuleGate.Abstractions.Evaluation;
using Fotbiler.RuleGate.Abstractions.Policies;
using Fotbiler.RuleGate.Core.Engine;
using Fotbiler.RuleGate.Core.Evaluation;
using Fotbiler.RuleGate.Core.Evaluation.Evaluators;
using Fotbiler.RuleGate.Core.Policies;

namespace Fotbiler.RuleGate.Core.Tests;

public sealed class AttributeAuthorizationEngineTests
{
    [Fact]
    public async Task
        Engine_allows_matching_subject_attribute()
    {
        var engine =
            CreateEngine(
                new AttributeRequirementDefinition(
                    source:
                        AuthorizationAttributeSource
                            .Subject,
                    name: "department",
                    @operator:
                        AuthorizationAttributeOperator
                            .Equal,
                    value: "finance"));

        var decision =
            await engine.EvaluateAsync(
                CreateRequest(
                    subjectAttributes:
                        CreateAttributes(
                            "department",
                            "finance")));

        Assert.True(decision.IsAllowed);
        Assert.Empty(decision.Failures);
    }

    [Fact]
    public async Task
        Engine_denies_missing_attribute()
    {
        var engine =
            CreateEngine(
                new AttributeRequirementDefinition(
                    source:
                        AuthorizationAttributeSource
                            .Subject,
                    name: "department",
                    @operator:
                        AuthorizationAttributeOperator
                            .Equal,
                    value: "finance",
                    id: "finance-department"));

        var decision =
            await engine.EvaluateAsync(
                CreateRequest());

        Assert.False(decision.IsAllowed);

        var failure =
            Assert.Single(decision.Failures);

        Assert.Equal(
            AuthorizationFailureCodes
                .AttributeNotFound,
            failure.Code);

        Assert.Equal(
            "finance-department",
            failure.RequirementId);
    }

    [Fact]
    public async Task
        Engine_denies_indeterminate_type_mismatch()
    {
        var engine =
            CreateEngine(
                new AttributeRequirementDefinition(
                    source:
                        AuthorizationAttributeSource
                            .Resource,
                    name: "classification",
                    @operator:
                        AuthorizationAttributeOperator
                            .LessThanOrEqual,
                    value: 3m,
                    id: "classification-limit"));

        var decision =
            await engine.EvaluateAsync(
                CreateRequest(
                    resourceAttributes:
                        CreateAttributes(
                            "classification",
                            "3")));

        Assert.False(decision.IsAllowed);

        var failure =
            Assert.Single(decision.Failures);

        Assert.Equal(
            AuthorizationFailureCodes
                .AttributeTypeMismatch,
            failure.Code);

        Assert.Equal(
            "classification-limit",
            failure.RequirementId);
    }

    [Fact]
    public async Task
        Engine_allows_combined_permission_and_resource_attribute()
    {
        var requirement =
            new AllRequirementDefinition(
            [
                new PermissionRequirementDefinition(
                    "document.read"),

                new AttributeRequirementDefinition(
                    source:
                        AuthorizationAttributeSource
                            .Resource,
                    name: "classification",
                    @operator:
                        AuthorizationAttributeOperator
                            .LessThanOrEqual,
                    value: 3m)
            ]);

        var engine =
            CreateEngine(requirement);

        var decision =
            await engine.EvaluateAsync(
                CreateRequest(
                    permissions:
                    [
                        "document.read"
                    ],
                    resourceAttributes:
                        CreateAttributes(
                            "classification",
                            2)));

        Assert.True(decision.IsAllowed);
        Assert.Empty(decision.Failures);
    }

    [Fact]
    public async Task
        Engine_allows_matching_context_attribute()
    {
        var engine =
            CreateEngine(
                new AttributeRequirementDefinition(
                    source:
                        AuthorizationAttributeSource
                            .Context,
                    name: "trustedNetwork",
                    @operator:
                        AuthorizationAttributeOperator
                            .Equal,
                    value: true));

        var decision =
            await engine.EvaluateAsync(
                CreateRequest(
                    contextAttributes:
                        CreateAttributes(
                            "trustedNetwork",
                            true)));

        Assert.True(decision.IsAllowed);
        Assert.Empty(decision.Failures);
    }

    private static PolicyAuthorizationEngine
        CreateEngine(
            RequirementDefinition requirement)
    {
        var policy =
            new PolicyDefinition(
                id: "sample-read",
                resourceType: "sample-resource",
                action: "read",
                requirement);

        var dispatcher =
            new RequirementEvaluationDispatcher(
            [
                new AttributeRequirementEvaluator(),
                new PermissionRequirementEvaluator(),
                new AllRequirementEvaluator()
            ]);

        return new PolicyAuthorizationEngine(
            new InMemoryPolicyProvider(
            [
                policy
            ]),
            dispatcher);
    }

    private static AuthorizationRequest CreateRequest(
        IEnumerable<string>? permissions = null,
        AuthorizationAttributes? subjectAttributes = null,
        AuthorizationAttributes? resourceAttributes = null,
        AuthorizationAttributes? contextAttributes = null)
    {
        return new AuthorizationRequest(
            subject:
                new AuthorizationSubject(
                    id: "user-1",
                    permissions: permissions,
                    attributes:
                        subjectAttributes),
            resource:
                new AuthorizationResource(
                    type: "sample-resource",
                    id: "resource-1",
                    attributes:
                        resourceAttributes),
            action: "read",
            context:
                new AuthorizationContext(
                    DateTimeOffset.UnixEpoch,
                    contextAttributes));
    }

    private static AuthorizationAttributes
        CreateAttributes(
            string name,
            object? value)
    {
        return new AuthorizationAttributes(
        [
            new KeyValuePair<string, object?>(
                name,
                value)
        ]);
    }
}
