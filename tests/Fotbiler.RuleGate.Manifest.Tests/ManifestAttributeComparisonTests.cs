using Fotbiler.RuleGate.Abstractions.Attributes;
using Fotbiler.RuleGate.Abstractions.Policies;
using Fotbiler.RuleGate.Manifest.Configuration;
using Fotbiler.RuleGate.Manifest.Loading;
using Fotbiler.RuleGate.Manifest.Mapping;
using Fotbiler.RuleGate.Manifest.Models;
using Fotbiler.RuleGate.Manifest.Validation;

namespace Fotbiler.RuleGate.Manifest.Tests;

public sealed class ManifestAttributeComparisonTests
{
    private readonly RuleGateManifestMapper _mapper =
        new(new RuleGateManifestValidator());

    [Fact]
    public void LoadFromText_loads_attribute_operands()
    {
        const string yaml = """
            schemaVersion: 1

            application:
              id: ownership-example
              name: Ownership Example

            policies:
              - id: document-update
                resourceType: document
                action: update
                requirement:
                  attributeComparison:
                    left:
                      source: resource
                      name: ownerId
                    operator: equal
                    right:
                      source: subject
                      name: id
                    stringComparison: ordinal
            """;

        var result =
            new RuleGateManifestYamlLoader()
                .LoadFromText(yaml);

        Assert.True(result.IsSuccess);

        var comparison = result.Manifest!
            .Policies![0]!
            .Requirement!
            .AttributeComparison!;

        Assert.Equal("resource", comparison.Left!.Source);
        Assert.Equal("ownerId", comparison.Left.Name);
        Assert.Equal("equal", comparison.Operator);
        Assert.Equal("subject", comparison.Right!.Source);
        Assert.Equal("id", comparison.Right.Name);
        Assert.Equal(
            "ordinal",
            comparison.StringComparison);
    }

    [Fact]
    public void LoadFromText_loads_typed_literal_operand()
    {
        const string yaml = """
            schemaVersion: 1
            application:
              id: scope-example
              name: Scope Example
            policies:
              - id: organization-scope
                resourceType: document
                action: read
                requirement:
                  attributeComparison:
                    left:
                      source: subject
                      name: organizationId
                    operator: in
                    right:
                      valueType: stringCollection
                      value:
                        - org-1
                        - org-2
            """;

        var result =
            new RuleGateManifestYamlLoader()
                .LoadFromText(yaml);

        Assert.True(result.IsSuccess);

        var right = result.Manifest!
            .Policies![0]!
            .Requirement!
            .AttributeComparison!
            .Right!;

        Assert.True(right.HasValue);
        Assert.Equal("stringCollection", right.ValueType);
        Assert.Equal(
            ["org-1", "org-2"],
            Assert.IsAssignableFrom<
                IEnumerable<object>>(right.Value));
    }

    [Fact]
    public void Map_maps_attribute_to_attribute_comparison()
    {
        var requirement = Map(
            Comparison(
                Attribute("resource", "ownerId"),
                "equal",
                Attribute("subject", "id")),
            id: "resource-owner");

        Assert.Equal("resource-owner", requirement.Id);
        Assert.Equal(
            AuthorizationAttributeOperator.Equal,
            requirement.Operator);
        Assert.Equal(
            AuthorizationAttributeOperandKind.Resource,
            requirement.Left.Kind);
        Assert.Equal("ownerId", requirement.Left.Name);
        Assert.Equal(
            AuthorizationAttributeOperandKind.Subject,
            requirement.Right.Kind);
        Assert.Equal("id", requirement.Right.Name);
    }

    [Fact]
    public void Map_maps_collection_literal_operand()
    {
        var requirement = Map(
            Comparison(
                Attribute("subject", "organizationId"),
                "in",
                Literal(
                    "stringCollection",
                    new[] { "org-1", "org-2" })));

        Assert.True(requirement.Right.IsLiteral);

        var value = Assert.IsType<
            AuthorizationAttributeValue>(
                requirement.Right.LiteralValue);

        Assert.Equal(
            AuthorizationAttributeValueKind.Collection,
            value.Kind);
        Assert.Equal(
            AuthorizationAttributeValueKind.String,
            value.CollectionElementKind);
        Assert.Equal(2, value.CollectionItems.Count);
    }

    [Fact]
    public void Validate_accepts_literal_to_attribute()
    {
        var result = Validate(
            Comparison(
                Literal("number", "3"),
                "lessThanOrEqual",
                Attribute("resource", "limit")));

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Validate_requires_both_operands(
        bool omitLeft)
    {
        var comparison = Comparison(
            Attribute("subject", "id"),
            "equal",
            Attribute("resource", "ownerId"));

        if (omitLeft)
        {
            comparison.Left = null;
        }
        else
        {
            comparison.Right = null;
        }

        var result = Validate(comparison);

        AssertError(
            result,
            omitLeft
                ? ManifestValidationCodes
                    .AttributeComparisonLeftRequired
                : ManifestValidationCodes
                    .AttributeComparisonRightRequired);
    }

    [Theory]
    [InlineData("exists")]
    [InlineData("isNull")]
    [InlineData("isEmpty")]
    public void Validate_rejects_unary_operator(
        string @operator)
    {
        var result = Validate(
            Comparison(
                Attribute("subject", "id"),
                @operator,
                Attribute("resource", "ownerId")));

        AssertError(
            result,
            ManifestValidationCodes
                .AttributeComparisonOperatorNotBinary);
    }

    [Fact]
    public void Validate_rejects_mixed_operand_shape()
    {
        var left = Attribute("subject", "id");
        left.ValueType = "string";
        left.Value = "user-1";

        var result = Validate(
            Comparison(
                left,
                "equal",
                Attribute("resource", "ownerId")));

        AssertError(
            result,
            ManifestValidationCodes
                .AttributeComparisonOperandKindInvalid);
    }

    [Fact]
    public void Validate_rejects_invalid_attribute_source()
    {
        var result = Validate(
            Comparison(
                Attribute("identity", "id"),
                "equal",
                Attribute("resource", "ownerId")));

        AssertError(
            result,
            ManifestValidationCodes
                .AttributeComparisonOperandSourceInvalid);
    }

    [Fact]
    public void Validate_rejects_missing_attribute_name()
    {
        var result = Validate(
            Comparison(
                Attribute("subject", name: null),
                "equal",
                Attribute("resource", "ownerId")));

        AssertError(
            result,
            ManifestValidationCodes
                .AttributeComparisonOperandNameRequired);
    }

    [Fact]
    public void Validate_rejects_missing_literal_value()
    {
        var result = Validate(
            Comparison(
                Attribute("subject", "id"),
                "equal",
                new ManifestAttributeComparisonOperand
                {
                    ValueType = "string"
                }));

        AssertError(
            result,
            ManifestValidationCodes
                .AttributeComparisonOperandValueRequired);
    }

    [Fact]
    public void Validate_rejects_incompatible_literals()
    {
        var result = Validate(
            Comparison(
                Literal("number", "3"),
                "equal",
                Literal("string", "3")));

        AssertError(
            result,
            ManifestValidationCodes
                .AttributeComparisonOperandTypeIncompatible);
    }

    [Fact]
    public void Validate_rejects_string_comparison_for_number()
    {
        var comparison = Comparison(
            Literal("number", "3"),
            "equal",
            Attribute("resource", "limit"));

        comparison.StringComparison =
            "ordinalIgnoreCase";

        var result = Validate(comparison);

        AssertError(
            result,
            ManifestValidationCodes
                .AttributeComparisonStringComparisonNotAllowed);
    }

    private AttributeComparisonRequirementDefinition Map(
        ManifestAttributeComparisonRequirement comparison,
        string? id = null)
    {
        var result = _mapper.Map(
            CreateManifest(
                new ManifestRequirement
                {
                    Id = id,
                    AttributeComparison = comparison
                }));

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Errors);

        return Assert.IsType<
            AttributeComparisonRequirementDefinition>(
                Assert.Single(result.Policies)
                    .Requirement);
    }

    private static ManifestValidationResult Validate(
        ManifestAttributeComparisonRequirement comparison)
    {
        return new RuleGateManifestValidator()
            .Validate(
                CreateManifest(
                    new ManifestRequirement
                    {
                        AttributeComparison = comparison
                    }));
    }

    private static ManifestAttributeComparisonRequirement
        Comparison(
            ManifestAttributeComparisonOperand left,
            string @operator,
            ManifestAttributeComparisonOperand right)
    {
        return new ManifestAttributeComparisonRequirement
        {
            Left = left,
            Operator = @operator,
            Right = right
        };
    }

    private static ManifestAttributeComparisonOperand
        Attribute(
            string source,
            string? name)
    {
        return new ManifestAttributeComparisonOperand
        {
            Source = source,
            Name = name
        };
    }

    private static ManifestAttributeComparisonOperand Literal(
        string valueType,
        object? value)
    {
        return new ManifestAttributeComparisonOperand
        {
            ValueType = valueType,
            Value = value
        };
    }

    private static RuleGateManifest CreateManifest(
        ManifestRequirement requirement)
    {
        return new RuleGateManifest
        {
            SchemaVersion =
                RuleGateManifestDefaults
                    .SupportedSchemaVersion,
            Application = new ManifestApplication
            {
                Id = "attribute-comparison-tests",
                Name = "Attribute Comparison Tests"
            },
            Policies =
            [
                new ManifestPolicy
                {
                    Id = "document-read",
                    ResourceType = "document",
                    Action = "read",
                    Requirement = requirement
                }
            ]
        };
    }

    private static void AssertError(
        ManifestValidationResult result,
        string code)
    {
        Assert.Contains(
            result.Errors,
            error => error.Code == code);
    }
}
