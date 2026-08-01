using System.Text;
using Fotbiler.RuleGate.Manifest.Compilation;
using Fotbiler.RuleGate.Manifest.Loading;

namespace Fotbiler.RuleGate.Manifest.Tests;

public sealed class RuleGateManifestYamlSecurityTests
{
    private const string ValidYaml = """
        schemaVersion: 1
        application:
          id: security-tests
          name: Security Tests
        policies:
          - id: sample-read
            resourceType: sample
            action: read
            requirement:
              permission: sample.read
        """;

    private readonly RuleGateManifestYamlLoader _loader =
        new();

    [Fact]
    public void LoadFromText_AcceptsSingleExplicitDocument()
    {
        const string yaml = """
            ---
            schemaVersion: 1
            application:
              id: explicit-document
              name: Explicit Document
            policies: []
            """;

        var result =
            _loader.LoadFromText(yaml);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void LoadFromText_RejectsMultipleDocuments()
    {
        const string yaml = """
            schemaVersion: 1
            application:
              id: first
              name: First
            policies: []
            ---
            schemaVersion: 1
            application:
              id: second
              name: Second
            policies: []
            """;

        AssertInvalidYaml(
            _loader.LoadFromText(yaml),
            "exactly one YAML document");
    }

    [Fact]
    public void LoadFromText_RejectsAnchors()
    {
        const string yaml = """
            schemaVersion: 1
            application:
              id: anchors
              name: Anchors
            policies:
              - id: sample-read
                resourceType: sample
                action: read
                requirement: &shared
                  permission: sample.read
            """;

        AssertInvalidYaml(
            _loader.LoadFromText(yaml),
            "anchors are not supported");
    }

    [Fact]
    public void LoadFromText_RejectsAliases()
    {
        const string yaml = """
            schemaVersion: 1
            application:
              id: aliases
              name: Aliases
            policies:
              - id: first
                resourceType: sample
                action: read
                requirement: &shared
                  permission: sample.read
              - id: second
                resourceType: sample
                action: write
                requirement: *shared
            """;

        var result =
            _loader.LoadFromText(yaml);

        Assert.False(result.IsSuccess);

        var error =
            Assert.Single(result.Errors);

        Assert.Equal(
            ManifestLoadCodes.InvalidYaml,
            error.Code);

        Assert.Contains(
            "anchors are not supported",
            error.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LoadFromText_RejectsSelfReferentialAliases()
    {
        const string yaml = """
            schemaVersion: 1
            application:
              id: recursive
              name: Recursive
            policies:
              - id: recursive
                resourceType: sample
                action: read
                requirement: &loop
                  not: *loop
            """;

        var compiler =
            new RuleGateManifestCompiler();

        var result =
            compiler.CompileFromText(yaml);

        Assert.False(result.IsSuccess);
        Assert.Empty(result.Policies);
        Assert.Empty(result.ValidationErrors);

        Assert.Equal(
            ManifestLoadCodes.InvalidYaml,
            Assert.Single(result.LoadErrors).Code);
    }

    [Fact]
    public void LoadFromText_RejectsLocalTags()
    {
        const string yaml = """
            --- !rulegate
            schemaVersion: 1
            application:
              id: local-tag
              name: Local Tag
            policies: []
            """;

        AssertInvalidYaml(
            _loader.LoadFromText(yaml),
            "Explicit YAML tags");
    }

    [Fact]
    public void LoadFromText_RejectsGlobalTags()
    {
        const string yaml = """
            --- !!map
            schemaVersion: 1
            application:
              id: global-tag
              name: Global Tag
            policies: []
            """;

        AssertInvalidYaml(
            _loader.LoadFromText(yaml),
            "Explicit YAML tags");
    }

    [Fact]
    public void LoadFromText_RejectsInvalidUnicode()
    {
        var yaml =
            ValidYaml +
            "\n# invalid-surrogate: \uD800";

        AssertInvalidYaml(
            _loader.LoadFromText(yaml),
            "invalid Unicode");
    }

    [Fact]
    public async Task
        LoadFromFileAsync_AcceptsUtf8Bom()
    {
        var path =
            CreateTemporaryPath();

        try
        {
            var bytes =
                Encoding.UTF8
                    .GetPreamble()
                    .Concat(
                        Encoding.UTF8.GetBytes(
                            ValidYaml))
                    .ToArray();

            await File.WriteAllBytesAsync(
                path,
                bytes);

            var result =
                await _loader.LoadFromFileAsync(
                    path);

            Assert.True(result.IsSuccess);
            Assert.Empty(result.Errors);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public async Task
        LoadFromFileAsync_RejectsInvalidUtf8()
    {
        var path =
            CreateTemporaryPath();

        try
        {
            var prefix =
                Encoding.UTF8.GetBytes(
                    ValidYaml);

            var bytes =
                new byte[prefix.Length + 1];

            Buffer.BlockCopy(
                prefix,
                0,
                bytes,
                0,
                prefix.Length);

            bytes[^1] = 0xFF;

            await File.WriteAllBytesAsync(
                path,
                bytes);

            AssertInvalidYaml(
                await _loader.LoadFromFileAsync(
                    path),
                "valid UTF-8");
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public async Task
        LoadFromFileAsync_RejectsUtf16()
    {
        var path =
            CreateTemporaryPath();

        try
        {
            var bytes =
                Encoding.Unicode
                    .GetPreamble()
                    .Concat(
                        Encoding.Unicode.GetBytes(
                            ValidYaml))
                    .ToArray();

            await File.WriteAllBytesAsync(
                path,
                bytes);

            AssertInvalidYaml(
                await _loader.LoadFromFileAsync(
                    path),
                "valid UTF-8");
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Fact]
    public async Task
        LoadFromFileAsync_AcceptsValidUnicodeUtf8()
    {
        var path =
            CreateTemporaryPath();

        try
        {
            const string yaml = """
                schemaVersion: 1
                application:
                  id: unicode-🚀
                  name: Güvenli Yetkilendirme 🔐
                policies: []
                """;

            await File.WriteAllTextAsync(
                path,
                yaml,
                new UTF8Encoding(
                    encoderShouldEmitUTF8Identifier:
                        false,
                    throwOnInvalidBytes: true));

            var result =
                await _loader.LoadFromFileAsync(
                    path);

            Assert.True(result.IsSuccess);
            Assert.Equal(
                "unicode-🚀",
                result.Manifest!
                    .Application!
                    .Id);
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    private static void AssertInvalidYaml(
        ManifestLoadResult result,
        string expectedMessage)
    {
        Assert.False(result.IsSuccess);

        var error =
            Assert.Single(result.Errors);

        Assert.Equal(
            ManifestLoadCodes.InvalidYaml,
            error.Code);

        Assert.Contains(
            expectedMessage,
            error.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    private static string CreateTemporaryPath()
    {
        return Path.Combine(
            Path.GetTempPath(),
            $"rulegate-yaml-security-{Guid.NewGuid():N}.yaml");
    }

    private static void DeleteIfExists(
        string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
