using Fotbiler.RuleGate.Manifest.Loading;
using Fotbiler.RuleGate.Manifest.Models;

namespace Fotbiler.RuleGate.Manifest.Tests;

public sealed class ManifestLoadResultTests
{
    [Fact]
    public void Success_StoresManifestWithoutErrors()
    {
        var manifest =
            new RuleGateManifest();

        var result =
            ManifestLoadResult.Success(manifest);

        Assert.True(result.IsSuccess);
        Assert.Same(manifest, result.Manifest);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Failure_RequiresAtLeastOneError()
    {
        Assert.Throws<ArgumentException>(
            () => ManifestLoadResult.Failure());
    }

    [Fact]
    public void Failure_ExposesReadOnlyErrors()
    {
        var result = ManifestLoadResult.Failure(
            new ManifestLoadError(
                "sample.error",
                "Sample error."));

        var errors =
            Assert.IsAssignableFrom<
                IList<ManifestLoadError>>(
                result.Errors);

        Assert.Throws<NotSupportedException>(
            () => errors.Add(
                new ManifestLoadError(
                    "another.error",
                    "Another error.")));
    }

    [Fact]
    public void Error_RejectsNonPositiveLocations()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new ManifestLoadError(
                "sample.error",
                "Sample error.",
                line: 0));

        Assert.Throws<ArgumentOutOfRangeException>(
            () => new ManifestLoadError(
                "sample.error",
                "Sample error.",
                column: 0));
    }
}
