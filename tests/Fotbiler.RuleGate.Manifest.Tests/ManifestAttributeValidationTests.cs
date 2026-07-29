using Fotbiler.RuleGate.Manifest.Configuration;
using Fotbiler.RuleGate.Manifest.Models;
using Fotbiler.RuleGate.Manifest.Validation;

namespace Fotbiler.RuleGate.Manifest.Tests;

public sealed class ManifestAttributeValidationTests
{
    public static TheoryData<string, object?>
        ValidValues
    {
        get
        {
            return new TheoryData<string, object?>
            {
                { "string", "finance" },
                { "boolean", "true" },
                { "number", "3.25" },
                {
                    "dateTimeOffset",
                    "2026-07-27T10:30:00+03:00"
                },
                {
                    "dateTimeOffset",
                    "2026-07-27T07:30:00Z"
                },
                {
                    "dateTimeOffset",
                    "2026-07-27T10:30:00.1234567+03:00"
                },
                {
                    "stringCollection",
                    new[]
                    {
                        "finance",
                        "operations"
                    }
                },
                {
                    "booleanCollection",
                    new object[]
                    {
                        "true",
                        false
                    }
                },
                {
                    "numberCollection",
                    new object[]
                    {
                        "3.25",
                        4
                    }
                },
                {
                    "dateTimeOffsetCollection",
                    new[]
                    {
                        "2026-07-27T10:30:00+03:00",
                        "2026-07-27T07:30:00Z"
                    }
                },
                { "nullValue", null }
            };
        }
    }

    public static TheoryData<string, object?>
        InvalidValues
    {
        get
        {
            return new TheoryData<string, object?>
            {
                { "boolean", "yes" },
                { "number", "1e3" },
                { "dateTimeOffset", "not-a-date" },
                {
                    "dateTimeOffset",
                    "2026-07-27T10:30:00"
                },
                {
                    "dateTimeOffset",
                    "2026-07-27"
                },
                {
                    "stringCollection",
                    new object[]
                    {
                        "finance",
                        1
                    }
                },
                {
                    "numberCollection",
                    new object[]
                    {
                        1,
                        "not-a-number"
                    }
                },
                { "nullValue", "null" }
            };
        }
    }

    [Theory]
    [MemberData(nameof(ValidValues))]
    public void
        Validate_accepts_supported_attribute_values(
            string valueType,
            object? value)
    {
        var attribute =
            CreateAttribute(
                valueType: valueType,
                value: value);

        if (valueType.EndsWith(
                "Collection",
                StringComparison.Ordinal))
        {
            attribute.Operator = "containsAny";
        }

        var result = Validate(attribute);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_requires_attribute_source()
    {
        var attribute = CreateAttribute();
        attribute.Source = null;

        AssertError(
            Validate(attribute),
            ManifestValidationCodes
                .AttributeSourceRequired,
            AttributePath("source"));
    }

    [Fact]
    public void Validate_rejects_unknown_attribute_source()
    {
        var attribute = CreateAttribute();
        attribute.Source = "tenant";

        AssertError(
            Validate(attribute),
            ManifestValidationCodes
                .AttributeSourceInvalid,
            AttributePath("source"));
    }

    [Fact]
    public void Validate_requires_attribute_name()
    {
        var attribute = CreateAttribute();
        attribute.Name = " ";

        AssertError(
            Validate(attribute),
            ManifestValidationCodes
                .AttributeNameRequired,
            AttributePath("name"));
    }

    [Fact]
    public void Validate_requires_attribute_operator()
    {
        var attribute = CreateAttribute();
        attribute.Operator = null;

        AssertError(
            Validate(attribute),
            ManifestValidationCodes
                .AttributeOperatorRequired,
            AttributePath("operator"));
    }

    [Fact]
    public void Validate_rejects_unknown_attribute_operator()
    {
        var attribute = CreateAttribute();
        attribute.Operator = "matches";

        AssertError(
            Validate(attribute),
            ManifestValidationCodes
                .AttributeOperatorInvalid,
            AttributePath("operator"));
    }

    [Theory]
    [InlineData("contains", "string")]
    [InlineData("startsWith", "string")]
    [InlineData("endsWith", "string")]
    [InlineData("containsAny", "stringCollection")]
    [InlineData("containsAll", "stringCollection")]
    [InlineData("in", "stringCollection")]
    [InlineData("notIn", "stringCollection")]
    [InlineData("intersects", "stringCollection")]
    public void Validate_accepts_advanced_operator(
        string @operator,
        string valueType)
    {
        object value =
            valueType == "string"
                ? "finance"
                : new[]
                {
                    "finance",
                    "operations"
                };

        var attribute =
            CreateAttribute(
                valueType,
                value);

        attribute.Operator = @operator;

        var result = Validate(attribute);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData("isEmpty")]
    [InlineData("isNotEmpty")]
    [InlineData("exists")]
    [InlineData("notExists")]
    [InlineData("isNull")]
    [InlineData("isNotNull")]
    public void Validate_accepts_value_less_operator(
        string @operator)
    {
        var attribute =
            new ManifestAttributeRequirement
            {
                Source = "subject",
                Name = "department",
                Operator = @operator
            };

        var result = Validate(attribute);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_rejects_value_for_value_less_operator()
    {
        var attribute = CreateAttribute();
        attribute.Operator = "exists";

        var result = Validate(attribute);

        AssertError(
            result,
            ManifestValidationCodes
                .AttributeValueTypeNotAllowed,
            AttributePath("valueType"));

        AssertError(
            result,
            ManifestValidationCodes
                .AttributeValueNotAllowed,
            AttributePath("value"));
    }

    [Theory]
    [InlineData("ordinal")]
    [InlineData("ordinalIgnoreCase")]
    public void Validate_accepts_string_comparison(
        string comparison)
    {
        var attribute = CreateAttribute();
        attribute.StringComparison = comparison;

        var result = Validate(attribute);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_rejects_unknown_string_comparison()
    {
        var attribute = CreateAttribute();
        attribute.StringComparison = "currentCulture";

        AssertError(
            Validate(attribute),
            ManifestValidationCodes
                .AttributeStringComparisonInvalid,
            AttributePath("stringComparison"));
    }

    [Fact]
    public void Validate_rejects_string_comparison_for_number()
    {
        var attribute =
            CreateAttribute(
                valueType: "number",
                value: "3");

        attribute.StringComparison =
            "ordinalIgnoreCase";

        AssertError(
            Validate(attribute),
            ManifestValidationCodes
                .AttributeStringComparisonNotAllowed,
            AttributePath("stringComparison"));
    }

    [Fact]
    public void Validate_rejects_oversized_collection()
    {
        var values =
            Enumerable.Range(
                    start: 0,
                    count: 257)
                .Cast<object>()
                .ToArray();

        AssertError(
            Validate(
                CreateAttribute(
                    valueType: "numberCollection",
                    value: values)),
            ManifestValidationCodes
                .AttributeValueInvalid,
            AttributePath("value"));
    }

    [Fact]
    public void Validate_requires_attribute_value_type()
    {
        var attribute = CreateAttribute();
        attribute.ValueType = null;

        AssertError(
            Validate(attribute),
            ManifestValidationCodes
                .AttributeValueTypeRequired,
            AttributePath("valueType"));
    }

    [Fact]
    public void Validate_rejects_unknown_attribute_value_type()
    {
        var attribute = CreateAttribute();
        attribute.ValueType = "integer";

        AssertError(
            Validate(attribute),
            ManifestValidationCodes
                .AttributeValueTypeInvalid,
            AttributePath("valueType"));
    }

    [Fact]
    public void Validate_requires_explicit_attribute_value()
    {
        var attribute =
            new ManifestAttributeRequirement
            {
                Source = "subject",
                Name = "department",
                Operator = "equal",
                ValueType = "string"
            };

        AssertError(
            Validate(attribute),
            ManifestValidationCodes
                .AttributeValueRequired,
            AttributePath("value"));
    }

    [Theory]
    [MemberData(nameof(InvalidValues))]
    public void
        Validate_rejects_value_incompatible_with_declared_type(
            string valueType,
            object? value)
    {
        AssertError(
            Validate(
                CreateAttribute(
                    valueType: valueType,
                    value: value)),
            ManifestValidationCodes
                .AttributeValueInvalid,
            AttributePath("value"));
    }

    [Theory]
    [InlineData("string")]
    [InlineData("boolean")]
    [InlineData("nullValue")]
    public void
        Validate_rejects_ordering_for_non_orderable_types(
            string valueType)
    {
        object? value = valueType switch
        {
            "string" => "finance",
            "boolean" => "true",
            "nullValue" => null,
            _ => throw new InvalidOperationException()
        };

        var attribute =
            CreateAttribute(
                valueType: valueType,
                value: value);

        attribute.Operator = "greaterThan";

        AssertError(
            Validate(attribute),
            ManifestValidationCodes
                .AttributeOperatorValueTypeInvalid,
            AttributePath("operator"));
    }

    [Fact]
    public void
        Validate_requires_exactly_one_requirement_kind()
    {
        var requirement =
            new ManifestRequirement
            {
                Permission = "document.read",
                Attribute = CreateAttribute()
            };

        var result =
            new RuleGateManifestValidator()
                .Validate(
                    CreateManifest(requirement));

        AssertError(
            result,
            ManifestValidationCodes
                .RequirementKindInvalid,
            "policies[0].requirement");
    }

    private static ManifestAttributeRequirement
        CreateAttribute(
            string valueType = "string",
            object? value = null)
    {
        var attribute =
            new ManifestAttributeRequirement
            {
                Source = "subject",
                Name = "department",
                Operator = "equal",
                ValueType = valueType
            };

        attribute.Value =
            valueType == "string" &&
            value is null
                ? "finance"
                : value;

        return attribute;
    }

    private static ManifestValidationResult Validate(
        ManifestAttributeRequirement attribute)
    {
        return new RuleGateManifestValidator()
            .Validate(
                CreateManifest(
                    new ManifestRequirement
                    {
                        Attribute = attribute
                    }));
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
                    Id = "attribute-tests",
                    Name = "Attribute Tests"
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
        string code,
        string path)
    {
        Assert.Contains(
            result.Errors,
            error =>
                error.Code == code &&
                error.Path == path);
    }

    private static string AttributePath(
        string member)
    {
        return
            $"policies[0].requirement.attribute.{member}";
    }
}
