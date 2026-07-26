using Fotbiler.RuleGate.Abstractions.Policies;
using Fotbiler.RuleGate.Manifest.Compilation;
using Fotbiler.RuleGate.Manifest.Loading;
using Fotbiler.RuleGate.Manifest.Validation;

namespace Fotbiler.RuleGate.Manifest.Tests;

public sealed class RuleGateManifestCompilerTests
{
    private readonly RuleGateManifestCompiler _compiler =
        new();

    [Fact]
    public void CompileFromText_CompilesValidManifest()
    {
        const string yaml = """
            schemaVersion: 1

            application:
              id: rulegate-example
              name: RuleGate Example

            policies:
              - id: sample-read
                resourceType: sample-resource
                action: read
                requirement:
                  permission: sample.read
            """;

        var result =
            _compiler.CompileFromText(yaml);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.LoadErrors);
        Assert.Empty(result.ValidationErrors);

        var policy =
            Assert.Single(result.Policies);

        Assert.Equal("sample-read", policy.Id);
        Assert.Equal(
            "sample-resource",
            policy.ResourceType);
        Assert.Equal("read", policy.Action);

        var requirement =
            Assert.IsType<
                PermissionRequirementDefinition>(
                policy.Requirement);

        Assert.Equal(
            "sample.read",
            requirement.Permission);
    }

    [Fact]
    public void CompileFromText_CompilesNestedRequirements()
    {
        const string yaml = """
            schemaVersion: 1

            application:
              id: rulegate-example
              name: RuleGate Example

            policies:
              - id: sample-read
                resourceType: sample-resource
                action: read
                requirement:
                  all:
                    - permission: sample.read
                    - any:
                        - role: sample.editor
                        - not:
                            role: sample.blocked
            """;

        var result =
            _compiler.CompileFromText(yaml);

        Assert.True(result.IsSuccess);

        var all =
            Assert.IsType<AllRequirementDefinition>(
                Assert.Single(result.Policies)
                    .Requirement);

        Assert.Equal(2, all.Requirements.Count);

        var any =
            Assert.IsType<AnyRequirementDefinition>(
                all.Requirements[1]);

        var not =
            Assert.IsType<NotRequirementDefinition>(
                any.Requirements[1]);

        Assert.Equal(
            "sample.blocked",
            Assert.IsType<RoleRequirementDefinition>(
                not.Requirement)
                .Role);
    }

    [Fact]
    public void CompileFromText_ReturnsLoadErrorsForMalformedYaml()
    {
        const string yaml = """
            schemaVersion: [1
            """;

        var result =
            _compiler.CompileFromText(yaml);

        Assert.False(result.IsSuccess);
        Assert.Empty(result.Policies);
        Assert.Empty(result.ValidationErrors);

        Assert.Equal(
            ManifestLoadCodes.InvalidYaml,
            Assert.Single(result.LoadErrors).Code);
    }

    [Fact]
    public void CompileFromText_ReturnsValidationErrorsForInvalidManifest()
    {
        const string yaml = """
            schemaVersion: 999

            application:
              id: rulegate-example
              name: RuleGate Example

            policies: []
            """;

        var result =
            _compiler.CompileFromText(yaml);

        Assert.False(result.IsSuccess);
        Assert.Empty(result.Policies);
        Assert.Empty(result.LoadErrors);

        Assert.Equal(
            ManifestValidationCodes
                .UnsupportedSchemaVersion,
            Assert.Single(
                result.ValidationErrors).Code);
    }

    [Fact]
    public void CompileFromText_AllowsEmptyPolicies()
    {
        const string yaml = """
            schemaVersion: 1

            application:
              id: rulegate-example
              name: RuleGate Example

            policies: []
            """;

        var result =
            _compiler.CompileFromText(yaml);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Policies);
        Assert.Empty(result.LoadErrors);
        Assert.Empty(result.ValidationErrors);
    }

    [Fact]
    public async Task CompileFromFileAsync_CompilesExistingFile()
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            $"rulegate-{Guid.NewGuid():N}.yaml");

        try
        {
            await File.WriteAllTextAsync(
                path,
                """
                schemaVersion: 1

                application:
                  id: rulegate-example
                  name: RuleGate Example

                policies:
                  - id: sample-read
                    resourceType: sample-resource
                    action: read
                    requirement:
                      permission: sample.read
                """);

            var result =
                await _compiler
                    .CompileFromFileAsync(path);

            Assert.True(result.IsSuccess);
            Assert.Single(result.Policies);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task CompileFromFileAsync_ReturnsFileNotFoundError()
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            $"missing-{Guid.NewGuid():N}.yaml");

        var result =
            await _compiler
                .CompileFromFileAsync(path);

        Assert.False(result.IsSuccess);
        Assert.Empty(result.ValidationErrors);

        Assert.Equal(
            ManifestLoadCodes.FileNotFound,
            Assert.Single(result.LoadErrors).Code);
    }

    [Fact]
    public async Task CompileFromFileAsync_HonorsCancellation()
    {
        using var cancellation =
            new CancellationTokenSource();

        cancellation.Cancel();

        await Assert.ThrowsAsync<
            OperationCanceledException>(
            async () =>
                await _compiler
                    .CompileFromFileAsync(
                        "rulegate.yaml",
                        cancellation.Token));
    }
}
