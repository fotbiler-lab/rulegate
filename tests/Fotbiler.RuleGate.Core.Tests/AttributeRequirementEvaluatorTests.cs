using Fotbiler.RuleGate.Abstractions.Attributes;
using Fotbiler.RuleGate.Abstractions.Authorization;
using Fotbiler.RuleGate.Abstractions.Constants;
using Fotbiler.RuleGate.Abstractions.Evaluation;
using Fotbiler.RuleGate.Abstractions.Policies;
using Fotbiler.RuleGate.Core.Evaluation;
using Fotbiler.RuleGate.Core.Evaluation.Evaluators;

namespace Fotbiler.RuleGate.Core.Tests;

public sealed class AttributeRequirementEvaluatorTests
{
    [Fact]
    public async Task
        Subject_string_equality_succeeds()
    {
        var result =
            await EvaluateAsync(
                CreateRequirement(
                    AuthorizationAttributeSource.Subject,
                    AuthorizationAttributeOperator.Equal,
                    value: "finance"),
                subjectAttributes:
                    CreateAttributes(
                        "department",
                        "finance"));

        Assert.True(result.IsSatisfied);
        Assert.Empty(result.Failures);
    }

    [Theory]
    [InlineData(
        AuthorizationAttributeOperator.GreaterThan,
        5,
        3)]
    [InlineData(
        AuthorizationAttributeOperator.GreaterThanOrEqual,
        5,
        5)]
    [InlineData(
        AuthorizationAttributeOperator.LessThan,
        3,
        5)]
    [InlineData(
        AuthorizationAttributeOperator.LessThanOrEqual,
        5,
        5)]
    public async Task
        Resource_numeric_ordering_succeeds(
            AuthorizationAttributeOperator @operator,
            int actual,
            int expected)
    {
        var result =
            await EvaluateAsync(
                new AttributeRequirementDefinition(
                    AuthorizationAttributeSource.Resource,
                    name: "classification",
                    @operator,
                    expected),
                resourceAttributes:
                    CreateAttributes(
                        "classification",
                        actual));

        Assert.True(result.IsSatisfied);
    }

    [Fact]
    public async Task
        Context_date_time_ordering_succeeds()
    {
        var expected =
            new DateTimeOffset(
                2026,
                7,
                27,
                12,
                0,
                0,
                TimeSpan.Zero);

        var actual =
            expected.AddMinutes(-30);

        var result =
            await EvaluateAsync(
                new AttributeRequirementDefinition(
                    AuthorizationAttributeSource.Context,
                    name: "requestTime",
                    AuthorizationAttributeOperator
                        .LessThanOrEqual,
                    expected),
                contextAttributes:
                    CreateAttributes(
                        "requestTime",
                        actual));

        Assert.True(result.IsSatisfied);
    }

    [Fact]
    public async Task
        Not_equal_succeeds_for_different_values()
    {
        var result =
            await EvaluateAsync(
                CreateRequirement(
                    AuthorizationAttributeSource.Subject,
                    AuthorizationAttributeOperator.NotEqual,
                    value: "blocked"),
                subjectAttributes:
                    CreateAttributes(
                        "department",
                        "finance"));

        Assert.True(result.IsSatisfied);
    }

    [Fact]
    public async Task
        Present_null_equals_expected_null()
    {
        var result =
            await EvaluateAsync(
                CreateRequirement(
                    AuthorizationAttributeSource.Subject,
                    AuthorizationAttributeOperator.Equal,
                    value: null),
                subjectAttributes:
                    CreateAttributes(
                        "managerId",
                        value: null),
                attributeName: "managerId");

        Assert.True(result.IsSatisfied);
    }

    [Fact]
    public async Task
        Missing_attribute_is_not_satisfied()
    {
        var result =
            await EvaluateAsync(
                CreateRequirement(
                    AuthorizationAttributeSource.Subject,
                    AuthorizationAttributeOperator.Equal,
                    value: "finance",
                    id: "finance-department"));

        Assert.True(result.IsNotSatisfied);

        var failure =
            Assert.Single(result.Failures);

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
        False_comparison_is_not_satisfied()
    {
        var result =
            await EvaluateAsync(
                CreateRequirement(
                    AuthorizationAttributeSource.Subject,
                    AuthorizationAttributeOperator.Equal,
                    value: "finance",
                    id: "finance-department"),
                subjectAttributes:
                    CreateAttributes(
                        "department",
                        "legal"));

        Assert.True(result.IsNotSatisfied);

        Assert.Equal(
            AuthorizationFailureCodes
                .AttributeComparisonNotSatisfied,
            Assert.Single(result.Failures).Code);
    }

    [Fact]
    public async Task
        Unsupported_actual_type_is_indeterminate()
    {
        var result =
            await EvaluateAsync(
                CreateRequirement(
                    AuthorizationAttributeSource.Subject,
                    AuthorizationAttributeOperator.Equal,
                    value: 1m),
                subjectAttributes:
                    CreateAttributes(
                        "department",
                        1.5d));

        Assert.True(result.IsIndeterminate);

        Assert.Equal(
            AuthorizationFailureCodes
                .AttributeTypeNotSupported,
            Assert.Single(result.Failures).Code);
    }

    [Fact]
    public async Task
        Different_supported_types_are_indeterminate()
    {
        var result =
            await EvaluateAsync(
                CreateRequirement(
                    AuthorizationAttributeSource.Subject,
                    AuthorizationAttributeOperator.Equal,
                    value: 1m),
                subjectAttributes:
                    CreateAttributes(
                        "department",
                        "1"));

        Assert.True(result.IsIndeterminate);

        Assert.Equal(
            AuthorizationFailureCodes
                .AttributeTypeMismatch,
            Assert.Single(result.Failures).Code);
    }

    [Fact]
    public async Task
        Ordering_boolean_values_is_indeterminate()
    {
        var result =
            await EvaluateAsync(
                CreateRequirement(
                    AuthorizationAttributeSource.Context,
                    AuthorizationAttributeOperator.GreaterThan,
                    value: false),
                contextAttributes:
                    CreateAttributes(
                        "department",
                        true));

        Assert.True(result.IsIndeterminate);

        Assert.Equal(
            AuthorizationFailureCodes
                .AttributeOperatorNotSupported,
            Assert.Single(result.Failures).Code);
    }

    [Fact]
    public async Task
        String_equality_is_ordinal_and_case_sensitive()
    {
        var result =
            await EvaluateAsync(
                CreateRequirement(
                    AuthorizationAttributeSource.Subject,
                    AuthorizationAttributeOperator.Equal,
                    value: "finance"),
                subjectAttributes:
                    CreateAttributes(
                        "department",
                        "Finance"));

        Assert.True(result.IsNotSatisfied);

        Assert.Equal(
            AuthorizationFailureCodes
                .AttributeComparisonNotSatisfied,
            Assert.Single(result.Failures).Code);
    }

    private static AttributeRequirementDefinition
        CreateRequirement(
            AuthorizationAttributeSource source,
            AuthorizationAttributeOperator @operator,
            object? value,
            string? id = null)
    {
        return CreateRequirement(
            source,
            @operator,
            value,
            attributeName: "department",
            id);
    }

    private static AttributeRequirementDefinition
        CreateRequirement(
            AuthorizationAttributeSource source,
            AuthorizationAttributeOperator @operator,
            object? value,
            string attributeName,
            string? id = null)
    {
        return new AttributeRequirementDefinition(
            source,
            attributeName,
            @operator,
            value,
            id);
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

    private static async Task<
        RequirementEvaluationResult> EvaluateAsync(
        AttributeRequirementDefinition requirement,
        AuthorizationAttributes? subjectAttributes = null,
        AuthorizationAttributes? resourceAttributes = null,
        AuthorizationAttributes? contextAttributes = null,
        string? attributeName = null)
    {
        if (attributeName is not null &&
            requirement.Name != attributeName)
        {
            requirement =
                new AttributeRequirementDefinition(
                    requirement.Source,
                    attributeName,
                    requirement.Operator,
                    requirement.ExpectedValue.Value,
                    requirement.Id);
        }

        var request =
            new AuthorizationRequest(
                subject:
                    new AuthorizationSubject(
                        id: "user-1",
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

        var dispatcher =
            new RequirementEvaluationDispatcher(
            [
                new AttributeRequirementEvaluator()
            ]);

        return await dispatcher.EvaluateAsync(
            requirement,
            new RequirementEvaluationContext(
                request));
    }
}
