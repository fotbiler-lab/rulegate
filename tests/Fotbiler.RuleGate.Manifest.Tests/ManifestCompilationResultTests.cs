using Fotbiler.RuleGate.Abstractions.Policies;
using Fotbiler.RuleGate.Manifest.Compilation;
using Fotbiler.RuleGate.Manifest.Loading;
using Fotbiler.RuleGate.Manifest.Validation;

namespace Fotbiler.RuleGate.Manifest.Tests;

public sealed class ManifestCompilationResultTests
{
    [Fact]
    public void Success_ExposesReadOnlyPolicies()
    {
        var result =
            ManifestCompilationResult.Success(
            [
                CreatePolicy()
            ]);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.LoadErrors);
        Assert.Empty(result.ValidationErrors);

        var policies =
            Assert.IsAssignableFrom<
                IList<PolicyDefinition>>(
                result.Policies);

        Assert.Throws<NotSupportedException>(
            () => policies.Add(CreatePolicy()));
    }

    [Fact]
    public void LoadFailure_ExposesOnlyLoadErrors()
    {
        var result =
            ManifestCompilationResult.LoadFailure(
            [
                new ManifestLoadError(
                    ManifestLoadCodes.InvalidYaml,
                    "Invalid YAML.")
            ]);

        Assert.False(result.IsSuccess);
        Assert.Empty(result.Policies);
        Assert.Single(result.LoadErrors);
        Assert.Empty(result.ValidationErrors);
    }

    [Fact]
    public void ValidationFailure_ExposesOnlyValidationErrors()
    {
        var result =
            ManifestCompilationResult
                .ValidationFailure(
                [
                    new ManifestValidationError(
                        ManifestValidationCodes
                            .ApplicationRequired,
                        "application",
                        "Application is required.")
                ]);

        Assert.False(result.IsSuccess);
        Assert.Empty(result.Policies);
        Assert.Empty(result.LoadErrors);
        Assert.Single(result.ValidationErrors);
    }

    [Fact]
    public void FailureFactories_RequireAtLeastOneError()
    {
        Assert.Throws<ArgumentException>(
            () =>
                ManifestCompilationResult
                    .LoadFailure(
                        Array.Empty<
                            ManifestLoadError>()));

        Assert.Throws<ArgumentException>(
            () =>
                ManifestCompilationResult
                    .ValidationFailure(
                        Array.Empty<
                            ManifestValidationError>()));
    }

    private static PolicyDefinition CreatePolicy()
    {
        return new PolicyDefinition(
            id: "sample-read",
            resourceType: "sample-resource",
            action: "read",
            requirement:
                new PermissionRequirementDefinition(
                    "sample.read"));
    }
}
