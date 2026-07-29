using Fotbiler.RuleGate.Abstractions.Attributes;
using Fotbiler.RuleGate.Abstractions.Policies;

namespace Fotbiler.RuleGate.Abstractions.Tests;

public sealed class
    AttributeComparisonRequirementDefinitionTests
{
    [Fact]
    public void Attribute_factories_preserve_structure()
    {
        var subject =
            AuthorizationAttributeOperand.Subject(
                "organizationId");

        var resource =
            AuthorizationAttributeOperand.Resource(
                "ownerOrganizationId");

        var context =
            AuthorizationAttributeOperand.Context(
                "tenantId");

        Assert.Equal(
            AuthorizationAttributeOperandKind.Subject,
            subject.Kind);
        Assert.Equal("organizationId", subject.Name);
        Assert.Null(subject.LiteralValue);

        Assert.Equal(
            AuthorizationAttributeOperandKind.Resource,
            resource.Kind);
        Assert.Equal(
            "ownerOrganizationId",
            resource.Name);

        Assert.Equal(
            AuthorizationAttributeOperandKind.Context,
            context.Kind);
        Assert.Equal("tenantId", context.Name);
    }

    [Fact]
    public void Literal_factory_normalizes_value()
    {
        var operand =
            AuthorizationAttributeOperand.Literal(
                new[] { 1, 2, 3 });

        Assert.True(operand.IsLiteral);
        Assert.Null(operand.Name);

        var value = Assert.IsType<
            AuthorizationAttributeValue>(
                operand.LiteralValue);

        Assert.Equal(
            AuthorizationAttributeValueKind.Collection,
            value.Kind);
        Assert.Equal(
            AuthorizationAttributeValueKind.Number,
            value.CollectionElementKind);
    }

    [Fact]
    public void Constructor_preserves_comparison()
    {
        var left =
            AuthorizationAttributeOperand.Resource(
                "ownerId");

        var right =
            AuthorizationAttributeOperand.Subject(
                "id");

        var requirement =
            new AttributeComparisonRequirementDefinition(
                left,
                AuthorizationAttributeOperator.Equal,
                right,
                id: "resource-owner",
                AuthorizationStringComparison
                    .OrdinalIgnoreCase);

        Assert.Same(left, requirement.Left);
        Assert.Same(right, requirement.Right);
        Assert.Equal(
            AuthorizationAttributeOperator.Equal,
            requirement.Operator);
        Assert.Equal(
            AuthorizationStringComparison
                .OrdinalIgnoreCase,
            requirement.StringComparison);
        Assert.Equal("resource-owner", requirement.Id);
    }

    [Theory]
    [InlineData(AuthorizationAttributeOperator.IsEmpty)]
    [InlineData(AuthorizationAttributeOperator.IsNotEmpty)]
    [InlineData(AuthorizationAttributeOperator.Exists)]
    [InlineData(AuthorizationAttributeOperator.NotExists)]
    [InlineData(AuthorizationAttributeOperator.IsNull)]
    [InlineData(AuthorizationAttributeOperator.IsNotNull)]
    public void Constructor_rejects_unary_operator(
        AuthorizationAttributeOperator @operator)
    {
        Assert.Throws<ArgumentException>(
            () =>
                new AttributeComparisonRequirementDefinition(
                    AuthorizationAttributeOperand.Subject(
                        "left"),
                    @operator,
                    AuthorizationAttributeOperand.Resource(
                        "right")));
    }

    [Fact]
    public void Attribute_factory_rejects_invalid_source()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => AuthorizationAttributeOperand.Attribute(
                (AuthorizationAttributeSource)999,
                "value"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Attribute_factory_rejects_invalid_name(
        string? name)
    {
        Assert.ThrowsAny<ArgumentException>(
            () => AuthorizationAttributeOperand.Subject(
                name!));
    }
}
