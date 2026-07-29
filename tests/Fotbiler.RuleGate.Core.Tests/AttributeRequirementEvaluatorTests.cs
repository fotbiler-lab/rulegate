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

    [Fact]
    public async Task
        String_equality_can_use_ordinal_ignore_case()
    {
        var result =
            await EvaluateAsync(
                CreateRequirement(
                    AuthorizationAttributeSource.Subject,
                    AuthorizationAttributeOperator.Equal,
                    value: "finance",
                    stringComparison:
                        AuthorizationStringComparison
                            .OrdinalIgnoreCase),
                subjectAttributes:
                    CreateAttributes(
                        "department",
                        "Finance"));

        Assert.True(result.IsSatisfied);
    }

    [Theory]
    [InlineData(
        AuthorizationAttributeOperator.Contains,
        "Finance Department",
        "Finance")]
    [InlineData(
        AuthorizationAttributeOperator.StartsWith,
        "Finance Department",
        "Finance")]
    [InlineData(
        AuthorizationAttributeOperator.EndsWith,
        "Finance Department",
        "Department")]
    public async Task
        String_operators_succeed(
            AuthorizationAttributeOperator @operator,
            string actual,
            string expected)
    {
        var result =
            await EvaluateAsync(
                CreateRequirement(
                    AuthorizationAttributeSource.Subject,
                    @operator,
                    expected),
                subjectAttributes:
                    CreateAttributes(
                        "department",
                        actual));

        Assert.True(result.IsSatisfied);
    }

    [Fact]
    public async Task
        String_contains_is_case_sensitive_by_default()
    {
        var result =
            await EvaluateAsync(
                CreateRequirement(
                    AuthorizationAttributeSource.Subject,
                    AuthorizationAttributeOperator.Contains,
                    value: "finance"),
                subjectAttributes:
                    CreateAttributes(
                        "department",
                        "Finance Department"));

        Assert.True(result.IsNotSatisfied);
    }

    [Fact]
    public async Task
        Collection_contains_uses_ordinal_ignore_case()
    {
        var result =
            await EvaluateAsync(
                CreateRequirement(
                    AuthorizationAttributeSource.Subject,
                    AuthorizationAttributeOperator.Contains,
                    value: "finance.approver",
                    stringComparison:
                        AuthorizationStringComparison
                            .OrdinalIgnoreCase),
                subjectAttributes:
                    CreateAttributes(
                        "department",
                        new[]
                        {
                            "Finance.Approver",
                            "document.reader"
                        }));

        Assert.True(result.IsSatisfied);
    }

    [Theory]
    [InlineData(
        AuthorizationAttributeOperator.ContainsAny)]
    [InlineData(
        AuthorizationAttributeOperator.Intersects)]
    public async Task
        Collection_intersection_operators_succeed(
            AuthorizationAttributeOperator @operator)
    {
        var result =
            await EvaluateAsync(
                CreateRequirement(
                    AuthorizationAttributeSource.Subject,
                    @operator,
                    value:
                        new[]
                        {
                            "document.approver",
                            "document.reader"
                        }),
                subjectAttributes:
                    CreateAttributes(
                        "department",
                        new[]
                        {
                            "finance.user",
                            "document.reader"
                        }));

        Assert.True(result.IsSatisfied);
    }

    [Fact]
    public async Task
        Collection_contains_all_succeeds()
    {
        var result =
            await EvaluateAsync(
                CreateRequirement(
                    AuthorizationAttributeSource.Subject,
                    AuthorizationAttributeOperator.ContainsAll,
                    value:
                        new[]
                        {
                            "document.read",
                            "document.approve"
                        }),
                subjectAttributes:
                    CreateAttributes(
                        "department",
                        new[]
                        {
                            "document.approve",
                            "document.read",
                            "document.archive"
                        }));

        Assert.True(result.IsSatisfied);
    }

    [Theory]
    [InlineData(
        AuthorizationAttributeOperator.In,
        "finance",
        true)]
    [InlineData(
        AuthorizationAttributeOperator.NotIn,
        "legal",
        true)]
    [InlineData(
        AuthorizationAttributeOperator.In,
        "legal",
        false)]
    public async Task
        Collection_membership_operators_are_deterministic(
            AuthorizationAttributeOperator @operator,
            string actual,
            bool expectedSatisfied)
    {
        var result =
            await EvaluateAsync(
                CreateRequirement(
                    AuthorizationAttributeSource.Subject,
                    @operator,
                    value:
                        new[]
                        {
                            "finance",
                            "operations"
                        }),
                subjectAttributes:
                    CreateAttributes(
                        "department",
                        actual));

        Assert.Equal(
            expectedSatisfied,
            result.IsSatisfied);
    }

    [Theory]
    [InlineData(
        AuthorizationAttributeOperator.IsEmpty,
        0,
        true)]
    [InlineData(
        AuthorizationAttributeOperator.IsEmpty,
        1,
        false)]
    [InlineData(
        AuthorizationAttributeOperator.IsNotEmpty,
        1,
        true)]
    public async Task
        Collection_empty_operators_are_deterministic(
            AuthorizationAttributeOperator @operator,
            int itemCount,
            bool expectedSatisfied)
    {
        var actual =
            Enumerable.Range(
                    start: 1,
                    count: itemCount)
                .ToArray();

        var result =
            await EvaluateAsync(
                CreateRequirement(
                    AuthorizationAttributeSource.Subject,
                    @operator,
                    value: null),
                subjectAttributes:
                    CreateAttributes(
                        "department",
                        actual));

        Assert.Equal(
            expectedSatisfied,
            result.IsSatisfied);
    }

    [Theory]
    [InlineData(
        AuthorizationAttributeOperator.Exists,
        true,
        true)]
    [InlineData(
        AuthorizationAttributeOperator.Exists,
        false,
        false)]
    [InlineData(
        AuthorizationAttributeOperator.NotExists,
        true,
        false)]
    [InlineData(
        AuthorizationAttributeOperator.NotExists,
        false,
        true)]
    public async Task
        Presence_operators_distinguish_missing_attributes(
            AuthorizationAttributeOperator @operator,
            bool attributeExists,
            bool expectedSatisfied)
    {
        var attributes = attributeExists
            ? CreateAttributes(
                "department",
                value: null)
            : AuthorizationAttributes.Empty;

        var result =
            await EvaluateAsync(
                CreateRequirement(
                    AuthorizationAttributeSource.Subject,
                    @operator,
                    value: null),
                subjectAttributes: attributes);

        Assert.Equal(
            expectedSatisfied,
            result.IsSatisfied);
    }

    [Theory]
    [InlineData(
        AuthorizationAttributeOperator.IsNull,
        null,
        true)]
    [InlineData(
        AuthorizationAttributeOperator.IsNull,
        "finance",
        false)]
    [InlineData(
        AuthorizationAttributeOperator.IsNotNull,
        "finance",
        true)]
    public async Task
        Null_operators_inspect_present_attributes(
            AuthorizationAttributeOperator @operator,
            object? actual,
            bool expectedSatisfied)
    {
        var result =
            await EvaluateAsync(
                CreateRequirement(
                    AuthorizationAttributeSource.Subject,
                    @operator,
                    value: null),
                subjectAttributes:
                    CreateAttributes(
                        "department",
                        actual));

        Assert.Equal(
            expectedSatisfied,
            result.IsSatisfied);
    }

    [Fact]
    public async Task
        Null_operator_does_not_treat_missing_as_null()
    {
        var result =
            await EvaluateAsync(
                CreateRequirement(
                    AuthorizationAttributeSource.Subject,
                    AuthorizationAttributeOperator.IsNull,
                    value: null));

        Assert.True(result.IsNotSatisfied);

        Assert.Equal(
            AuthorizationFailureCodes.AttributeNotFound,
            Assert.Single(result.Failures).Code);
    }

    [Fact]
    public async Task
        Collection_type_mismatch_is_indeterminate()
    {
        var result =
            await EvaluateAsync(
                CreateRequirement(
                    AuthorizationAttributeSource.Subject,
                    AuthorizationAttributeOperator.Contains,
                    value: "1"),
                subjectAttributes:
                    CreateAttributes(
                        "department",
                        new[]
                        {
                            1,
                            2
                        }));

        Assert.True(result.IsIndeterminate);

        Assert.Equal(
            AuthorizationFailureCodes.AttributeTypeMismatch,
            Assert.Single(result.Failures).Code);
    }

    [Fact]
    public async Task
        Oversized_actual_collection_is_indeterminate()
    {
        var actual =
            Enumerable.Range(
                start: 0,
                count:
                    AuthorizationAttributeValue
                        .MaximumCollectionElementCount +
                    1);

        var result =
            await EvaluateAsync(
                CreateRequirement(
                    AuthorizationAttributeSource.Subject,
                    AuthorizationAttributeOperator.Contains,
                    value: 1),
                subjectAttributes:
                    CreateAttributes(
                        "department",
                        actual));

        Assert.True(result.IsIndeterminate);

        Assert.Equal(
            AuthorizationFailureCodes
                .AttributeTypeNotSupported,
            Assert.Single(result.Failures).Code);
    }

    private static AttributeRequirementDefinition
        CreateRequirement(
        AuthorizationAttributeSource source,
        AuthorizationAttributeOperator @operator,
        object? value,
        string? id = null,
        AuthorizationStringComparison
            stringComparison =
                AuthorizationStringComparison.Ordinal)
    {
        return CreateRequirement(
            source,
            @operator,
            value,
            attributeName: "department",
            id,
            stringComparison);
    }

    private static AttributeRequirementDefinition
        CreateRequirement(
            AuthorizationAttributeSource source,
        AuthorizationAttributeOperator @operator,
        object? value,
        string attributeName,
        string? id = null,
        AuthorizationStringComparison
            stringComparison =
                AuthorizationStringComparison.Ordinal)
    {
        return new AttributeRequirementDefinition(
            source,
            attributeName,
            @operator,
            value,
            id,
            stringComparison);
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
                    requirement.Id,
                    requirement.StringComparison);
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
