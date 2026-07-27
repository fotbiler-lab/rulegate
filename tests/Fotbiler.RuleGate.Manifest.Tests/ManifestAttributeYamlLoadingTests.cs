using Fotbiler.RuleGate.Manifest.Loading;
using Fotbiler.RuleGate.Manifest.Models;

namespace Fotbiler.RuleGate.Manifest.Tests;

public sealed class ManifestAttributeYamlLoadingTests
{
    private readonly RuleGateManifestYamlLoader _loader =
        new();

    [Fact]
    public void
        LoadFromText_loads_attribute_requirement()
    {
        const string yaml = """
            schemaVersion: 1

            application:
              id: attribute-example
              name: Attribute Example

            policies:
              - id: document-read
                resourceType: document
                action: read
                requirement:
                  attribute:
                    source: subject
                    name: department
                    operator: equal
                    valueType: string
                    value: finance
            """;

        var result =
            _loader.LoadFromText(yaml);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Errors);

        var requirement =
            Assert.Single(
                result.Manifest!.Policies!)
                !.Requirement!;

        var attribute =
            Assert.IsType<
                ManifestAttributeRequirement>(
                requirement.Attribute);

        Assert.Equal(
            "subject",
            attribute.Source);

        Assert.Equal(
            "department",
            attribute.Name);

        Assert.Equal(
            "equal",
            attribute.Operator);

        Assert.Equal(
            "string",
            attribute.ValueType);

        Assert.True(attribute.HasValue);

        Assert.Equal(
            "finance",
            attribute.Value);
    }

    [Theory]
    [InlineData("3")]
    [InlineData("3.25")]
    [InlineData("true")]
    [InlineData(
        "2026-07-27T10:30:00+03:00")]
    public void
        LoadFromText_preserves_non_null_scalars_as_strings(
            string scalar)
    {
        var yaml = $$"""
            schemaVersion: 1

            application:
              id: scalar-example
              name: Scalar Example

            policies:
              - id: scalar-read
                resourceType: sample
                action: read
                requirement:
                  attribute:
                    source: resource
                    name: sampleValue
                    operator: equal
                    valueType: string
                    value: {{scalar}}
            """;

        var result =
            _loader.LoadFromText(yaml);

        Assert.True(result.IsSuccess);

        var attribute =
            result.Manifest!
                .Policies![0]!
                .Requirement!
                .Attribute!;

        Assert.True(attribute.HasValue);

        Assert.Equal(
            scalar,
            Assert.IsType<string>(
                attribute.Value));
    }

    [Fact]
    public void
        LoadFromText_distinguishes_missing_value_from_explicit_null()
    {
        const string explicitNullYaml = """
            schemaVersion: 1

            application:
              id: explicit-null
              name: Explicit Null

            policies:
              - id: null-read
                resourceType: sample
                action: read
                requirement:
                  attribute:
                    source: resource
                    name: parentId
                    operator: equal
                    valueType: nullValue
                    value: null
            """;

        const string missingValueYaml = """
            schemaVersion: 1

            application:
              id: missing-value
              name: Missing Value

            policies:
              - id: null-read
                resourceType: sample
                action: read
                requirement:
                  attribute:
                    source: resource
                    name: parentId
                    operator: equal
                    valueType: nullValue
            """;

        var explicitNull =
            _loader.LoadFromText(
                explicitNullYaml);

        var missingValue =
            _loader.LoadFromText(
                missingValueYaml);

        Assert.True(explicitNull.IsSuccess);
        Assert.True(missingValue.IsSuccess);

        var explicitAttribute =
            explicitNull.Manifest!
                .Policies![0]!
                .Requirement!
                .Attribute!;

        var missingAttribute =
            missingValue.Manifest!
                .Policies![0]!
                .Requirement!
                .Attribute!;

        Assert.True(
            explicitAttribute.HasValue);

        Assert.Null(
            explicitAttribute.Value);

        Assert.False(
            missingAttribute.HasValue);

        Assert.Null(
            missingAttribute.Value);
    }

    [Fact]
    public void
        LoadFromText_treats_empty_scalar_as_present_null()
    {
        const string yaml = """
            schemaVersion: 1

            application:
              id: empty-scalar
              name: Empty Scalar

            policies:
              - id: null-read
                resourceType: sample
                action: read
                requirement:
                  attribute:
                    source: resource
                    name: parentId
                    operator: equal
                    valueType: nullValue
                    value:
            """;

        var result =
            _loader.LoadFromText(yaml);

        Assert.True(result.IsSuccess);

        var attribute =
            result.Manifest!
                .Policies![0]!
                .Requirement!
                .Attribute!;

        Assert.True(attribute.HasValue);
        Assert.Null(attribute.Value);
    }
}
