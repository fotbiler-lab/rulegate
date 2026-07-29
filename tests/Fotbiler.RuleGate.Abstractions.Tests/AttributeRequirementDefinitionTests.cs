using Fotbiler.RuleGate.Abstractions.Attributes;
using Fotbiler.RuleGate.Abstractions.Policies;

namespace Fotbiler.RuleGate.Abstractions.Tests;

public sealed class AttributeRequirementDefinitionTests
{
    public static TheoryData<object, decimal>
        NumericValues
    {
        get
        {
            return new TheoryData<object, decimal>
            {
                { (byte)1, 1m },
                { (sbyte)-1, -1m },
                { (short)-2, -2m },
                { (ushort)2, 2m },
                { -3, -3m },
                { 3U, 3m },
                { -4L, -4m },
                { 4UL, 4m },
                { 5.25m, 5.25m }
            };
        }
    }

    [Fact]
    public void Constructor_preserves_requirement_metadata()
    {
        var requirement =
            new AttributeRequirementDefinition(
                source:
                    AuthorizationAttributeSource
                        .Subject,
                name: "department",
                @operator:
                    AuthorizationAttributeOperator
                        .Equal,
                value: "finance",
                id: "finance-department");

        Assert.Equal(
            AuthorizationAttributeSource.Subject,
            requirement.Source);

        Assert.Equal(
            "department",
            requirement.Name);

        Assert.Equal(
            AuthorizationAttributeOperator.Equal,
            requirement.Operator);

        Assert.Equal(
            "finance-department",
            requirement.Id);

        Assert.Equal(
            AuthorizationAttributeValueKind.String,
            requirement.ExpectedValue.Kind);

        Assert.Equal(
            "finance",
            requirement.ExpectedValue.Value);
    }

    [Theory]
    [MemberData(nameof(NumericValues))]
    public void Constructor_normalizes_numbers_to_decimal(
        object value,
        decimal expected)
    {
        var requirement =
            CreateRequirement(value);

        Assert.Equal(
            AuthorizationAttributeValueKind.Number,
            requirement.ExpectedValue.Kind);

        Assert.Equal(
            expected,
            Assert.IsType<decimal>(
                requirement.ExpectedValue.Value));
    }

    [Fact]
    public void Constructor_supports_null_value()
    {
        var requirement =
            CreateRequirement(value: null);

        Assert.Equal(
            AuthorizationAttributeValueKind.Null,
            requirement.ExpectedValue.Kind);

        Assert.Null(
            requirement.ExpectedValue.Value);
    }

    [Fact]
    public void Constructor_supports_boolean_value()
    {
        var requirement =
            CreateRequirement(true);

        Assert.Equal(
            AuthorizationAttributeValueKind.Boolean,
            requirement.ExpectedValue.Kind);

        Assert.Equal(
            true,
            requirement.ExpectedValue.Value);
    }

    [Fact]
    public void Constructor_supports_date_time_offset()
    {
        var expected =
            new DateTimeOffset(
                year: 2026,
                month: 7,
                day: 27,
                hour: 10,
                minute: 30,
                second: 0,
                offset: TimeSpan.FromHours(3));

        var requirement =
            CreateRequirement(expected);

        Assert.Equal(
            AuthorizationAttributeValueKind
                .DateTimeOffset,
            requirement.ExpectedValue.Kind);

        Assert.Equal(
            expected,
            requirement.ExpectedValue.Value);
    }

    [Fact]
    public void Constructor_preserves_string_comparison()
    {
        var requirement =
            new AttributeRequirementDefinition(
                AuthorizationAttributeSource.Subject,
                name: "department",
                AuthorizationAttributeOperator.Equal,
                value: "finance",
                stringComparison:
                    AuthorizationStringComparison
                        .OrdinalIgnoreCase);

        Assert.Equal(
            AuthorizationStringComparison.OrdinalIgnoreCase,
            requirement.StringComparison);
    }

    [Fact]
    public void Constructor_normalizes_collection_values()
    {
        var requirement =
            CreateRequirement(
                new[]
                {
                    1,
                    2,
                    3
                });

        Assert.Equal(
            AuthorizationAttributeValueKind.Collection,
            requirement.ExpectedValue.Kind);

        Assert.Equal(
            AuthorizationAttributeValueKind.Number,
            requirement.ExpectedValue
                .CollectionElementKind);

        Assert.Equal(
            new[]
            {
                1m,
                2m,
                3m
            },
            requirement.ExpectedValue
                .CollectionItems
                .Select(
                    static item =>
                        Assert.IsType<decimal>(
                            item.Value)));
    }

    [Fact]
    public void Constructor_supports_empty_collection()
    {
        var requirement =
            CreateRequirement(
                Array.Empty<string>());

        Assert.Equal(
            AuthorizationAttributeValueKind.Collection,
            requirement.ExpectedValue.Kind);

        Assert.Null(
            requirement.ExpectedValue
                .CollectionElementKind);

        Assert.Empty(
            requirement.ExpectedValue
                .CollectionItems);
    }

    [Fact]
    public void Constructor_rejects_mixed_collection()
    {
        Assert.Throws<ArgumentException>(
            () =>
                CreateRequirement(
                    new object[]
                    {
                        "finance",
                        1
                    }));
    }

    [Fact]
    public void Constructor_rejects_null_collection_item()
    {
        Assert.Throws<ArgumentException>(
            () =>
                CreateRequirement(
                    new string?[]
                    {
                        "finance",
                        null
                    }));
    }

    [Fact]
    public void Constructor_rejects_nested_collection()
    {
        Assert.Throws<ArgumentException>(
            () =>
                CreateRequirement(
                    new object[]
                    {
                        new[]
                        {
                            "finance"
                        }
                    }));
    }

    [Fact]
    public void Constructor_rejects_oversized_collection()
    {
        var values =
            Enumerable.Range(
                start: 0,
                count:
                    AuthorizationAttributeValue
                        .MaximumCollectionElementCount +
                    1);

        Assert.Throws<ArgumentException>(
            () => CreateRequirement(values));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void Constructor_rejects_blank_attribute_name(
        string name)
    {
        Assert.Throws<ArgumentException>(
            () =>
                new AttributeRequirementDefinition(
                    AuthorizationAttributeSource
                        .Subject,
                    name,
                    AuthorizationAttributeOperator
                        .Equal,
                    value: "finance"));
    }

    [Fact]
    public void Constructor_rejects_unknown_source()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new AttributeRequirementDefinition(
                    (AuthorizationAttributeSource)999,
                    name: "department",
                    AuthorizationAttributeOperator
                        .Equal,
                    value: "finance"));
    }

    [Fact]
    public void Constructor_rejects_unknown_operator()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new AttributeRequirementDefinition(
                    AuthorizationAttributeSource
                        .Subject,
                    name: "department",
                    (AuthorizationAttributeOperator)999,
                    value: "finance"));
    }

    [Fact]
    public void Constructor_rejects_unknown_string_comparison()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () =>
                new AttributeRequirementDefinition(
                    AuthorizationAttributeSource.Subject,
                    name: "department",
                    AuthorizationAttributeOperator.Equal,
                    value: "finance",
                    stringComparison:
                        (AuthorizationStringComparison)999));
    }

    [Fact]
    public void Constructor_rejects_double_value()
    {
        Assert.Throws<ArgumentException>(
            () => CreateRequirement(1.5d));
    }

    [Fact]
    public void Constructor_rejects_date_time_value()
    {
        Assert.Throws<ArgumentException>(
            () =>
                CreateRequirement(
                    DateTime.UtcNow));
    }

    [Fact]
    public void Constructor_rejects_arbitrary_object()
    {
        Assert.Throws<ArgumentException>(
            () =>
                CreateRequirement(
                    new object()));
    }

    private static AttributeRequirementDefinition
        CreateRequirement(
            object? value)
    {
        return new AttributeRequirementDefinition(
            source:
                AuthorizationAttributeSource.Resource,
            name: "classification",
            @operator:
                AuthorizationAttributeOperator.Equal,
            value);
    }
}
