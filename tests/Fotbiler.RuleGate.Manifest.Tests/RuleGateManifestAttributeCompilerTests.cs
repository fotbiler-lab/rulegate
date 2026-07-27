using Fotbiler.RuleGate.Abstractions.Attributes;
using Fotbiler.RuleGate.Abstractions.Policies;
using Fotbiler.RuleGate.Manifest.Compilation;
using Fotbiler.RuleGate.Manifest.Validation;

namespace Fotbiler.RuleGate.Manifest.Tests;

public sealed class RuleGateManifestAttributeCompilerTests
{
    private readonly RuleGateManifestCompiler _compiler =
        new();

    [Fact]
    public void
        CompileFromText_compiles_string_attribute()
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
                  id: finance-department
                  attribute:
                    source: subject
                    name: department
                    operator: equal
                    valueType: string
                    value: finance
            """;

        var result =
            _compiler.CompileFromText(yaml);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.LoadErrors);
        Assert.Empty(result.ValidationErrors);

        var requirement =
            Assert.IsType<
                AttributeRequirementDefinition>(
                Assert.Single(result.Policies)
                    .Requirement);

        Assert.Equal(
            "finance-department",
            requirement.Id);

        Assert.Equal(
            AuthorizationAttributeSource.Subject,
            requirement.Source);

        Assert.Equal(
            AuthorizationAttributeOperator.Equal,
            requirement.Operator);

        Assert.Equal(
            AuthorizationAttributeValueKind.String,
            requirement.ExpectedValue.Kind);

        Assert.Equal(
            "finance",
            requirement.ExpectedValue.Value);
    }

    [Fact]
    public void
        CompileFromText_compiles_nested_attribute()
    {
        const string yaml = """
            schemaVersion: 1

            application:
              id: nested-attribute
              name: Nested Attribute

            policies:
              - id: document-read
                resourceType: document
                action: read
                requirement:
                  all:
                    - permission: document.read
                    - attribute:
                        source: resource
                        name: classification
                        operator: lessThanOrEqual
                        valueType: number
                        value: 3
            """;

        var result =
            _compiler.CompileFromText(yaml);

        Assert.True(result.IsSuccess);

        var all =
            Assert.IsType<AllRequirementDefinition>(
                Assert.Single(result.Policies)
                    .Requirement);

        Assert.IsType<
            PermissionRequirementDefinition>(
                all.Requirements[0]);

        var attribute =
            Assert.IsType<
                AttributeRequirementDefinition>(
                all.Requirements[1]);

        Assert.Equal(
            3m,
            Assert.IsType<decimal>(
                attribute.ExpectedValue.Value));
    }

    [Fact]
    public void
        CompileFromText_compiles_explicit_null()
    {
        const string yaml = """
            schemaVersion: 1

            application:
              id: null-attribute
              name: Null Attribute

            policies:
              - id: parentless-document
                resourceType: document
                action: read
                requirement:
                  attribute:
                    source: resource
                    name: parentId
                    operator: equal
                    valueType: nullValue
                    value: null
            """;

        var result =
            _compiler.CompileFromText(yaml);

        Assert.True(result.IsSuccess);

        var attribute =
            Assert.IsType<
                AttributeRequirementDefinition>(
                Assert.Single(result.Policies)
                    .Requirement);

        Assert.Equal(
            AuthorizationAttributeValueKind.Null,
            attribute.ExpectedValue.Kind);

        Assert.Null(attribute.ExpectedValue.Value);
    }

    [Fact]
    public void
        CompileFromText_returns_attribute_validation_errors()
    {
        const string yaml = """
            schemaVersion: 1

            application:
              id: invalid-attribute
              name: Invalid Attribute

            policies:
              - id: document-read
                resourceType: document
                action: read
                requirement:
                  attribute:
                    source: subject
                    name: department
                    operator: greaterThan
                    valueType: string
                    value: finance
            """;

        var result =
            _compiler.CompileFromText(yaml);

        Assert.False(result.IsSuccess);
        Assert.Empty(result.Policies);
        Assert.Empty(result.LoadErrors);

        Assert.Contains(
            result.ValidationErrors,
            error =>
                error.Code ==
                    ManifestValidationCodes
                        .AttributeOperatorValueTypeInvalid &&
                error.Path ==
                    "policies[0].requirement.attribute.operator");
    }
    [Fact]
    public void
        CompileFromText_rejects_yaml_null_token_as_missing_value_type()
    {
        const string yaml = """
            schemaVersion: 1

            application:
              id: invalid-null-type
              name: Invalid Null Type

            policies:
              - id: parentless-document
                resourceType: document
                action: read
                requirement:
                  attribute:
                    source: resource
                    name: parentId
                    operator: equal
                    valueType: null
                    value: null
            """;

        var result =
            _compiler.CompileFromText(yaml);

        Assert.False(result.IsSuccess);
        Assert.Empty(result.LoadErrors);
        Assert.Empty(result.Policies);

        Assert.Contains(
            result.ValidationErrors,
            error =>
                error.Code ==
                    ManifestValidationCodes
                        .AttributeValueTypeRequired &&
                error.Path ==
                    "policies[0].requirement.attribute.valueType");
    }

}
