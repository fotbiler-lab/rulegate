using Fotbiler.RuleGate.Manifest.PolicySources;

namespace Fotbiler.RuleGate.Manifest.Tests;

public sealed class PolicySourceTests
{
    private const string ValidManifest = """
        schemaVersion: 1
        application:
          id: source-test
          name: Source Test
        policies:
          - id: document-read
            resourceType: document
            action: read
            requirement:
              permission: document.read
        """;

    [Fact]
    public async Task YamlFileSource_LoadsValidatedPolicies()
    {
        var path = await CreateTemporaryManifestAsync(
            ValidManifest);

        try
        {
            var source = new YamlFilePolicySource(
                path,
                new YamlPolicyFileOptions
                {
                    ReloadOnChange = true,
                });

            var result = await source.LoadAsync();

            Assert.True(result.IsSuccess);
            var policy = Assert.Single(result.Policies);
            Assert.Equal("document-read", policy.Id);
            Assert.True(source.ReloadOnChange);
            Assert.Equal(
                Path.GetFullPath(path),
                source.FullPath);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task YamlFileSource_ReturnsManifestDiagnostics()
    {
        var path = await CreateTemporaryManifestAsync(
            "schemaVersion: [invalid");

        try
        {
            var source = new YamlFilePolicySource(path);

            var result = await source.LoadAsync();

            Assert.False(result.IsSuccess);
            var diagnostic = Assert.Single(
                result.Diagnostics);
            Assert.Equal(
                "MANIFEST_YAML_INVALID",
                diagnostic.Code);
            Assert.StartsWith(
                "line:",
                diagnostic.Path,
                StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task YamlFileSource_MissingFileFailsClosed()
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            $"rulegate-missing-{Guid.NewGuid():N}.yaml");
        var source = new YamlFilePolicySource(path);

        var result = await source.LoadAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal(
            "MANIFEST_FILE_NOT_FOUND",
            Assert.Single(result.Diagnostics).Code);
    }

    [Fact]
    public async Task EmbeddedResourceSource_LoadsValidatedPolicies()
    {
        var source = new EmbeddedResourcePolicySource(
            typeof(PolicySourceTests).Assembly,
            "RuleGate.Tests.embedded-rulegate.yaml");

        var result = await source.LoadAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(
            "embedded-document-read",
            Assert.Single(result.Policies).Id);
    }

    [Fact]
    public async Task EmbeddedResourceSource_MissingResourceFailsClosed()
    {
        var source = new EmbeddedResourcePolicySource(
            typeof(PolicySourceTests).Assembly,
            "RuleGate.Tests.missing.yaml");

        var result = await source.LoadAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal(
            ManifestPolicySourceCodes
                .EmbeddedResourceNotFound,
            Assert.Single(result.Diagnostics).Code);
    }

    [Fact]
    public async Task Sources_HonorCancellation()
    {
        var path = await CreateTemporaryManifestAsync(
            ValidManifest);

        try
        {
            using var cancellation =
                new CancellationTokenSource();
            cancellation.Cancel();

            await Assert.ThrowsAsync<
                OperationCanceledException>(
                async () =>
                    await new YamlFilePolicySource(path)
                        .LoadAsync(cancellation.Token));

            await Assert.ThrowsAsync<
                OperationCanceledException>(
                async () =>
                    await new EmbeddedResourcePolicySource(
                            typeof(PolicySourceTests).Assembly,
                            "RuleGate.Tests.embedded-rulegate.yaml")
                        .LoadAsync(cancellation.Token));
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static async Task<string>
        CreateTemporaryManifestAsync(
            string content)
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            $"rulegate-source-{Guid.NewGuid():N}.yaml");

        await File.WriteAllTextAsync(path, content);

        return path;
    }
}
