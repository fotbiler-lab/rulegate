using Fotbiler.RuleGate.Abstractions.Policies;
using Fotbiler.RuleGate.Manifest.Mapping;
using Fotbiler.RuleGate.Manifest.Validation;

namespace Fotbiler.RuleGate.Manifest.Tests;

public sealed class ManifestMappingResultTests
{
    [Fact]
    public void Success_ExposesReadOnlyPolicies()
    {
        var result = ManifestMappingResult.Success(
        [
            new PolicyDefinition(
                id: "sample-read",
                resourceType: "sample-resource",
                action: "read",
                requirement:
                    new PermissionRequirementDefinition(
                        "sample.read"))
        ]);

        var policies =
            Assert.IsAssignableFrom<
                IList<PolicyDefinition>>(
                result.Policies);

        Assert.Throws<NotSupportedException>(
            () => policies.Add(
                new PolicyDefinition(
                    id: "another-read",
                    resourceType: "another-resource",
                    action: "read",
                    requirement:
                        new PermissionRequirementDefinition(
                            "another.read"))));
    }

    [Fact]
    public void Failure_RequiresAtLeastOneError()
    {
        Assert.Throws<ArgumentException>(
            () => ManifestMappingResult.Failure(
                []));
    }

    [Fact]
    public void Failure_DoesNotExposePolicies()
    {
        var result = ManifestMappingResult.Failure(
        [
            new ManifestValidationError(
                code: "sample.error",
                path: "sample",
                message: "Sample error.")
        ]);

        Assert.False(result.IsSuccess);
        Assert.Empty(result.Policies);
        Assert.Single(result.Errors);
    }
}
