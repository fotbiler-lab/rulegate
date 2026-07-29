using Fotbiler.RuleGate.Abstractions.Attributes;
using Fotbiler.RuleGate.Abstractions.Authorization;
using Fotbiler.RuleGate.Abstractions.Constants;
using Fotbiler.RuleGate.Abstractions.Evaluation;
using Fotbiler.RuleGate.Abstractions.Policies;
using Fotbiler.RuleGate.Core.Evaluation;
using Fotbiler.RuleGate.Core.Evaluation.Evaluators;

namespace Fotbiler.RuleGate.Core.Tests;

public sealed class
    AttributeComparisonRequirementEvaluatorTests
{
    [Fact]
    public async Task Resource_owner_equals_subject_id()
    {
        var result = await EvaluateAsync(
            CreateRequirement(
                AuthorizationAttributeOperand.Resource(
                    "ownerId"),
                AuthorizationAttributeOperator.Equal,
                AuthorizationAttributeOperand.Subject(
                    "id")),
            subjectAttributes:
                Attributes(("id", "user-1")),
            resourceAttributes:
                Attributes(("ownerId", "user-1")));

        Assert.True(result.IsSatisfied);
    }

    [Fact]
    public async Task Numeric_values_are_normalized()
    {
        var result = await EvaluateAsync(
            CreateRequirement(
                AuthorizationAttributeOperand.Subject(
                    "clearance"),
                AuthorizationAttributeOperator
                    .GreaterThanOrEqual,
                AuthorizationAttributeOperand.Resource(
                    "requiredClearance")),
            subjectAttributes:
                Attributes(("clearance", 5)),
            resourceAttributes:
                Attributes(("requiredClearance", 5m)));

        Assert.True(result.IsSatisfied);
    }

    [Fact]
    public async Task Date_time_values_are_ordered()
    {
        var boundary =
            new DateTimeOffset(
                2026,
                7,
                29,
                12,
                0,
                0,
                TimeSpan.Zero);

        var result = await EvaluateAsync(
            CreateRequirement(
                AuthorizationAttributeOperand.Context(
                    "requestTime"),
                AuthorizationAttributeOperator.LessThan,
                AuthorizationAttributeOperand.Resource(
                    "expiresAt")),
            resourceAttributes:
                Attributes(("expiresAt", boundary)),
            contextAttributes:
                Attributes(
                    ("requestTime",
                     boundary.AddMinutes(-1))));

        Assert.True(result.IsSatisfied);
    }

    [Fact]
    public async Task String_comparison_can_ignore_case()
    {
        var result = await EvaluateAsync(
            new AttributeComparisonRequirementDefinition(
                AuthorizationAttributeOperand.Subject(
                    "organization"),
                AuthorizationAttributeOperator.Equal,
                AuthorizationAttributeOperand.Resource(
                    "organization"),
                stringComparison:
                    AuthorizationStringComparison
                        .OrdinalIgnoreCase),
            subjectAttributes:
                Attributes(("organization", "FINANCE")),
            resourceAttributes:
                Attributes(("organization", "finance")));

        Assert.True(result.IsSatisfied);
    }

    [Fact]
    public async Task Collections_can_intersect()
    {
        var result = await EvaluateAsync(
            CreateRequirement(
                AuthorizationAttributeOperand.Subject(
                    "organizationScopes"),
                AuthorizationAttributeOperator.Intersects,
                AuthorizationAttributeOperand.Resource(
                    "organizationScopes")),
            subjectAttributes:
                Attributes(
                    ("organizationScopes",
                     new[] { "finance", "legal" })),
            resourceAttributes:
                Attributes(
                    ("organizationScopes",
                     new[] { "sales", "finance" })));

        Assert.True(result.IsSatisfied);
    }

    [Fact]
    public async Task Literal_can_be_left_operand()
    {
        var result = await EvaluateAsync(
            new AttributeComparisonRequirementDefinition(
                AuthorizationAttributeOperand.Literal(
                    "FINANCE"),
                AuthorizationAttributeOperator.StartsWith,
                AuthorizationAttributeOperand.Context(
                    "prefix"),
                stringComparison:
                    AuthorizationStringComparison
                        .OrdinalIgnoreCase),
            contextAttributes:
                Attributes(("prefix", "fin")));

        Assert.True(result.IsSatisfied);
    }

    [Fact]
    public async Task Literal_can_be_right_operand()
    {
        var result = await EvaluateAsync(
            CreateRequirement(
                AuthorizationAttributeOperand.Subject(
                    "organizationId"),
                AuthorizationAttributeOperator.In,
                AuthorizationAttributeOperand.Literal(
                    new[] { "org-1", "org-2" })),
            subjectAttributes:
                Attributes(("organizationId", "org-2")));

        Assert.True(result.IsSatisfied);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Missing_operand_is_not_satisfied(
        bool missingLeft)
    {
        var result = await EvaluateAsync(
            CreateRequirement(
                AuthorizationAttributeOperand.Subject(
                    "left"),
                AuthorizationAttributeOperator.Equal,
                AuthorizationAttributeOperand.Resource(
                    "right"),
                id: "ownership"),
            subjectAttributes:
                missingLeft
                    ? null
                    : Attributes(("left", "same")),
            resourceAttributes:
                missingLeft
                    ? Attributes(("right", "same"))
                    : null);

        Assert.True(result.IsNotSatisfied);

        var failure = Assert.Single(result.Failures);
        Assert.Equal(
            AuthorizationFailureCodes.AttributeNotFound,
            failure.Code);
        Assert.Equal("ownership", failure.RequirementId);
    }

    [Fact]
    public async Task Present_null_values_are_equal()
    {
        var result = await EvaluateAsync(
            CreateRequirement(
                AuthorizationAttributeOperand.Subject(
                    "parentId"),
                AuthorizationAttributeOperator.Equal,
                AuthorizationAttributeOperand.Resource(
                    "parentId")),
            subjectAttributes:
                Attributes(("parentId", null)),
            resourceAttributes:
                Attributes(("parentId", null)));

        Assert.True(result.IsSatisfied);
    }

    [Fact]
    public async Task Type_mismatch_is_indeterminate()
    {
        var result = await EvaluateAsync(
            CreateRequirement(
                AuthorizationAttributeOperand.Subject(
                    "organizationId"),
                AuthorizationAttributeOperator.Equal,
                AuthorizationAttributeOperand.Resource(
                    "organizationId")),
            subjectAttributes:
                Attributes(("organizationId", "org-1")),
            resourceAttributes:
                Attributes(("organizationId", 1)));

        Assert.True(result.IsIndeterminate);
        Assert.Equal(
            AuthorizationFailureCodes.AttributeTypeMismatch,
            Assert.Single(result.Failures).Code);
    }

    [Fact]
    public async Task Unsupported_runtime_type_is_indeterminate()
    {
        var result = await EvaluateAsync(
            CreateRequirement(
                AuthorizationAttributeOperand.Subject(
                    "organizationId"),
                AuthorizationAttributeOperator.Equal,
                AuthorizationAttributeOperand.Resource(
                    "organizationId")),
            subjectAttributes:
                Attributes(
                    ("organizationId",
                     new Uri("https://example.com"))),
            resourceAttributes:
                Attributes(("organizationId", "org-1")));

        Assert.True(result.IsIndeterminate);
        Assert.Equal(
            AuthorizationFailureCodes
                .AttributeTypeNotSupported,
            Assert.Single(result.Failures).Code);
    }

    private static AttributeComparisonRequirementDefinition
        CreateRequirement(
            AuthorizationAttributeOperand left,
            AuthorizationAttributeOperator @operator,
            AuthorizationAttributeOperand right,
            string? id = null)
    {
        return new AttributeComparisonRequirementDefinition(
            left,
            @operator,
            right,
            id);
    }

    private static AuthorizationAttributes Attributes(
        params (string Name, object? Value)[] values)
    {
        return new AuthorizationAttributes(
            values.Select(
                static value =>
                    new KeyValuePair<string, object?>(
                        value.Name,
                        value.Value)));
    }

    private static async Task<
        RequirementEvaluationResult> EvaluateAsync(
            AttributeComparisonRequirementDefinition requirement,
            AuthorizationAttributes? subjectAttributes = null,
            AuthorizationAttributes? resourceAttributes = null,
            AuthorizationAttributes? contextAttributes = null)
    {
        var request = new AuthorizationRequest(
            new AuthorizationSubject(
                "user-1",
                attributes: subjectAttributes),
            new AuthorizationResource(
                "document",
                "document-1",
                resourceAttributes),
            "read",
            new AuthorizationContext(
                DateTimeOffset.UnixEpoch,
                contextAttributes));

        var dispatcher =
            new RequirementEvaluationDispatcher(
            [
                new AttributeComparisonRequirementEvaluator()
            ]);

        return await dispatcher.EvaluateAsync(
            requirement,
            new RequirementEvaluationContext(request));
    }
}
