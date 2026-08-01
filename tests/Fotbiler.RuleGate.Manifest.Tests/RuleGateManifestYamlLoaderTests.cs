using System.Text;
using Fotbiler.RuleGate.Manifest.Loading;

namespace Fotbiler.RuleGate.Manifest.Tests;

public sealed class RuleGateManifestYamlLoaderTests
{
    private readonly RuleGateManifestYamlLoader _loader =
        new();

    [Fact]
    public void LoadFromText_ParsesValidManifest()
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

        var result = _loader.LoadFromText(yaml);

        Assert.True(result.IsSuccess);

        var manifest = Assert.IsType<
            Manifest.Models.RuleGateManifest>(
            result.Manifest);

        Assert.Equal(1, manifest.SchemaVersion);

        Assert.Equal(
            "rulegate-example",
            manifest.Application!.Id);

        var policy =
            Assert.Single(manifest.Policies!);

        Assert.Equal("sample-read", policy!.Id);
        Assert.Equal(
            "sample.read",
            policy.Requirement!.Permission);
    }

    [Fact]
    public void LoadFromText_ParsesNestedRequirements()
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

        var result = _loader.LoadFromText(yaml);

        Assert.True(result.IsSuccess);

        var requirement =
            result.Manifest!
                .Policies![0]!
                .Requirement!;

        Assert.Equal(2, requirement.All!.Count);
        Assert.Equal(
            2,
            requirement.All[1]!.Any!.Count);

        Assert.Equal(
            "sample.blocked",
            requirement
                .All[1]!
                .Any![1]!
                .Not!
                .Role);
    }

    [Fact]
    public void LoadFromText_DoesNotPerformSchemaValidation()
    {
        const string yaml = """
            schemaVersion: 999

            application:
              id: rulegate-example
              name: RuleGate Example

            policies: []
            """;

        var result = _loader.LoadFromText(yaml);

        Assert.True(result.IsSuccess);

        Assert.Equal(
            999,
            result.Manifest!.SchemaVersion);
    }

    [Fact]
    public void LoadFromText_ReturnsErrorForEmptyContent()
    {
        var result =
            _loader.LoadFromText("   ");

        Assert.False(result.IsSuccess);
        Assert.Null(result.Manifest);

        Assert.Equal(
            ManifestLoadCodes.EmptyContent,
            Assert.Single(result.Errors).Code);
    }

    [Fact]
    public void LoadFromText_RejectsNullContent()
    {
        Assert.Throws<ArgumentNullException>(
            () => _loader.LoadFromText(null!));
    }

    [Fact]
    public void LoadFromText_ReturnsErrorForNullRoot()
    {
        var result =
            _loader.LoadFromText("null");

        Assert.False(result.IsSuccess);

        Assert.Equal(
            ManifestLoadCodes.RootRequired,
            Assert.Single(result.Errors).Code);
    }

    [Fact]
    public void LoadFromText_ReturnsErrorForMalformedYaml()
    {
        const string yaml = """
            schemaVersion: [1
            """;

        var result = _loader.LoadFromText(yaml);

        Assert.False(result.IsSuccess);

        var error = Assert.Single(result.Errors);

        Assert.Equal(
            ManifestLoadCodes.InvalidYaml,
            error.Code);

        Assert.NotNull(error.Line);
        Assert.NotNull(error.Column);
    }

    [Fact]
    public void LoadFromText_RejectsUnknownProperty()
    {
        const string yaml = """
            schemaVersion: 1
            unexpectedProperty: true
            """;

        var result = _loader.LoadFromText(yaml);

        Assert.False(result.IsSuccess);

        Assert.Equal(
            ManifestLoadCodes.InvalidYaml,
            Assert.Single(result.Errors).Code);
    }

    [Fact]
    public void LoadFromText_RejectsDuplicateKey()
    {
        const string yaml = """
            schemaVersion: 1
            schemaVersion: 2
            """;

        var result = _loader.LoadFromText(yaml);

        Assert.False(result.IsSuccess);

        Assert.Equal(
            ManifestLoadCodes.InvalidYaml,
            Assert.Single(result.Errors).Code);
    }

    [Fact]
    public async Task LoadFromFileAsync_ParsesExistingFile()
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

                policies: []
                """);

            var result =
                await _loader.LoadFromFileAsync(path);

            Assert.True(result.IsSuccess);

            Assert.Equal(
                "rulegate-example",
                result.Manifest!.Application!.Id);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task LoadFromFileAsync_ReturnsFileNotFoundError()
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            $"missing-{Guid.NewGuid():N}.yaml");

        var result =
            await _loader.LoadFromFileAsync(path);

        Assert.False(result.IsSuccess);

        Assert.Equal(
            ManifestLoadCodes.FileNotFound,
            Assert.Single(result.Errors).Code);
    }

    [Fact]
    public async Task LoadFromFileAsync_HonorsCancellation()
    {
        using var cancellation =
            new CancellationTokenSource();

        cancellation.Cancel();

        await Assert.ThrowsAsync<
            OperationCanceledException>(
            async () =>
                await _loader.LoadFromFileAsync(
                    "rulegate.yaml",
                    cancellation.Token));
    }

    [Fact]
    public void
        LoadFromText_AcceptsContentAtMaximumSize()
    {
        var yaml =
            CreateValidYamlWithUtf8ByteCount(
                MaximumManifestContentByteCount);

        var result =
            _loader.LoadFromText(yaml);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void
        LoadFromText_RejectsContentAboveMaximumSize()
    {
        var yaml =
            CreateValidYamlWithUtf8ByteCount(
                MaximumManifestContentByteCount + 1);

        var result =
            _loader.LoadFromText(yaml);

        Assert.False(result.IsSuccess);

        Assert.Equal(
            ManifestLoadCodes.ContentTooLarge,
            Assert.Single(result.Errors).Code);
    }

    [Fact]
    public void
        LoadFromText_UsesUtf8ByteCount()
    {
        const string prefix = """
            schemaVersion: 1
            application:
              id: utf8-size
              name: UTF8 Size
            policies: []
            #
            """;

        var yaml =
            prefix +
            new string(
                '€',
                350_000);

        Assert.True(
            yaml.Length <
            MaximumManifestContentByteCount);

        Assert.True(
            Encoding.UTF8.GetByteCount(yaml) >
            MaximumManifestContentByteCount);

        var result =
            _loader.LoadFromText(yaml);

        Assert.False(result.IsSuccess);

        Assert.Equal(
            ManifestLoadCodes.ContentTooLarge,
            Assert.Single(result.Errors).Code);
    }

    [Fact]
    public async Task
        LoadFromFileAsync_AcceptsFileAtMaximumSize()
    {
        var path =
            Path.GetTempFileName();

        try
        {
            var yaml =
                CreateValidYamlWithUtf8ByteCount(
                    MaximumManifestContentByteCount);

            await File.WriteAllTextAsync(
                path,
                yaml,
                new UTF8Encoding(
                    encoderShouldEmitUTF8Identifier: false));

            var result =
                await _loader.LoadFromFileAsync(
                    path);

            Assert.True(result.IsSuccess);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task
        LoadFromFileAsync_RejectsOversizedFile()
    {
        var path =
            Path.GetTempFileName();

        try
        {
            await File.WriteAllTextAsync(
                path,
                new string(
                    'a',
                    MaximumManifestContentByteCount + 1),
                new UTF8Encoding(
                    encoderShouldEmitUTF8Identifier: false));

            var result =
                await _loader.LoadFromFileAsync(
                    path);

            Assert.False(result.IsSuccess);

            Assert.Equal(
                ManifestLoadCodes.ContentTooLarge,
                Assert.Single(result.Errors).Code);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private const int
        MaximumManifestContentByteCount =
            1_048_576;

    private static string
        CreateValidYamlWithUtf8ByteCount(
            int byteCount)
    {
        const string manifest = """
            schemaVersion: 1
            application:
              id: resource-limit
              name: Resource Limit
            policies: []
            #
            """;

        var currentByteCount =
            Encoding.UTF8.GetByteCount(
                manifest);

        if (currentByteCount > byteCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(byteCount));
        }

        return manifest +
            new string(
                'x',
                byteCount - currentByteCount);
    }

}
