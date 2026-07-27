using Fotbiler.RuleGate.Abstractions.Attributes;
using Fotbiler.RuleGate.Abstractions.Policies;
using Fotbiler.RuleGate.Manifest.Configuration;
using Fotbiler.RuleGate.Manifest.Mapping;
using Fotbiler.RuleGate.Manifest.Models;
using Fotbiler.RuleGate.Manifest.Validation;

namespace Fotbiler.RuleGate.Manifest.Tests;

public sealed class ManifestAttributeMappingTests
{
    private readonly RuleGateManifestMapper _mapper =
        new(new RuleGateManifestValidator());

    [Fact]
    public void Map_maps_string_attribute()
    {
        var requirement =
            Map(
                CreateAttribute(
                    source: "subject",
                    name: "department",
                    @operator: "equal",
                    valueType: "string",
                    value: "finance"),
                id: "finance-department");

        Assert.Equal(
            AuthorizationAttributeSource.Subject,
            requirement.Source);

        Assert.Equal(
            AuthorizationAttributeOperator.Equal,
            requirement.Operator);

        Assert.Equal(
            "department",
            requirement.Name);

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

    [Fact]
    public void Map_maps_number_as_decimal()
    {
        var requirement =
            Map(
                CreateAttribute(
                    source: "resource",
                    name: "classification",
                    @operator: "lessThanOrEqual",
                    valueType: "number",
                    value: "3.25"));

        Assert.Equal(
            AuthorizationAttributeSource.Resource,
            requirement.Source);

        Assert.Equal(
            AuthorizationAttributeOperator
                .LessThanOrEqual,
            requirement.Operator);

        Assert.Equal(
            AuthorizationAttributeValueKind.Number,
            requirement.ExpectedValue.Kind);

        Assert.Equal(
            3.25m,
            Assert.IsType<decimal>(
                requirement.ExpectedValue.Value));
    }

    [Fact]
    public void Map_maps_boolean_attribute()
    {
        var requirement =
            Map(
                CreateAttribute(
                    source: "context",
                    name: "trustedNetwork",
                    @operator: "notEqual",
                    valueType: "boolean",
                    value: "false"));

        Assert.Equal(
            AuthorizationAttributeSource.Context,
            requirement.Source);

        Assert.Equal(
            AuthorizationAttributeOperator.NotEqual,
            requirement.Operator);

        Assert.Equal(
            false,
            requirement.ExpectedValue.Value);
    }

    [Fact]
    public void Map_maps_explicit_null_attribute()
    {
        var requirement =
            Map(
                CreateAttribute(
                    source: "resource",
                    name: "parentId",
                    @operator: "equal",
                    valueType: "nullValue",
                    value: null));

        Assert.Equal(
            AuthorizationAttributeValueKind.Null,
            requirement.ExpectedValue.Kind);

        Assert.Null(
            requirement.ExpectedValue.Value);
    }

    [Fact]
    public void Map_maps_date_time_offset_attribute()
    {
        const string text =
            "2026-07-27T10:30:00+03:00";

        var requirement =
            Map(
                CreateAttribute(
                    source: "context",
                    name: "requestTime",
                    @operator: "greaterThanOrEqual",
                    valueType: "dateTimeOffset",
                    value: text));

        Assert.Equal(
            AuthorizationAttributeValueKind
                .DateTimeOffset,
            requirement.ExpectedValue.Kind);

        var value =
            Assert.IsType<DateTimeOffset>(
                requirement.ExpectedValue.Value);

        Assert.Equal(
            DateTimeOffset.Parse(text),
            value);
    }

    [Fact]
    public void
        Map_maps_attribute_inside_logical_requirement()
    {
        var attribute =
            CreateAttribute(
                source: "resource",
                name: "classification",
                @operator: "lessThanOrEqual",
                valueType: "number",
                value: "3");

        var manifest =
            CreateManifest(
                new ManifestRequirement
                {
                    All =
                    [
                        new ManifestRequirement
                        {
                            Permission =
                                "document.read"
                        },
                        new ManifestRequirement
                        {
                            Attribute = attribute
                        }
                    ]
                });

        var result = _mapper.Map(manifest);

        Assert.True(result.IsSuccess);

        var all =
            Assert.IsType<AllRequirementDefinition>(
                Assert.Single(result.Policies)
                    .Requirement);

        Assert.IsType<
            PermissionRequirementDefinition>(
                all.Requirements[0]);

        Assert.IsType<
            AttributeRequirementDefinition>(
                all.Requirements[1]);
    }

    private AttributeRequirementDefinition Map(
        ManifestAttributeRequirement attribute,
        string? id = null)
    {
        var result =
            _mapper.Map(
                CreateManifest(
                    new ManifestRequirement
                    {
                        Id = id,
                        Attribute = attribute
                    }));

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Errors);

        return Assert.IsType<
            AttributeRequirementDefinition>(
                Assert.Single(result.Policies)
                    .Requirement);
    }

    private static ManifestAttributeRequirement
        CreateAttribute(
            string source,
            string name,
            string @operator,
            string valueType,
            object? value)
    {
        return new ManifestAttributeRequirement
        {
            Source = source,
            Name = name,
            Operator = @operator,
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

            Application =
                new ManifestApplication
                {
                    Id = "attribute-mapping",
                    Name = "Attribute Mapping"
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
}
