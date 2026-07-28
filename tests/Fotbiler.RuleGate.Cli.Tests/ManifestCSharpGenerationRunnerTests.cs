using Fotbiler.RuleGate.Cli.Generation.CSharp;
using Fotbiler.RuleGate.Manifest.Compilation;

namespace Fotbiler.RuleGate.Cli.Tests;

public sealed class ManifestCSharpGenerationRunnerTests
{
    private const string ValidManifest =
        """
        schemaVersion: 1

        application:
          id: generation-tests
          name: Generation Tests

        policies:
          - id: orders.update
            resourceType: order
            action: update
            requirement:
              all:
                - id: update-permission
                  permission: orders.update
                - any:
                    - role: order.editor
                    - role: order.administrator

          - id: documents.read
            resourceType: document
            action: read
            requirement:
              all:
                - id: read-permission
                  permission: documents.read
                - any:
                    - role: document.reader
                    - role: document.administrator
        """;

    private const string CollisionManifest =
        """
        schemaVersion: 1

        application:
          id: generation-collision-tests
          name: Generation Collision Tests

        policies:
          - id: orders.read
            resourceType: order
            action: read
            requirement:
              all:
                - id: read-dot-permission
                  permission: orders.read

          - id: orders-read
            resourceType: order
            action: inspect
            requirement:
              all:
                - id: read-dash-permission
                  permission: orders.inspect
        """;

    private const string InvalidManifest =
        """
        schemaVersion: 1

        application:
          id: invalid-generation-tests
          name: Invalid Generation Tests

        policies:
          - id: invalid-policy
            resourceType: document
            action: read
        """;

    [Fact]
    public async Task GenerateAsync_UsesCompiledPoliciesAsGeneratorInput()
    {
        using var manifest =
            await TemporaryManifest.CreateAsync(
                ValidManifest);

        var runner =
            CreateRunner();

        ManifestCSharpGenerationResult result =
            await runner.GenerateAsync(
                manifest.Path,
                "Sample.Authorization",
                CancellationToken.None);

        Assert.True(
            result.Compilation.IsSuccess);

        Assert.True(
            result.IsSuccess);

        Assert.Empty(
            result.Compilation.LoadErrors);

        Assert.Empty(
            result.Compilation.ValidationErrors);

        string source =
            Assert.IsType<string>(
                result.Source);

        Assert.Equal(
            Path.GetFullPath(
                manifest.Path),
            result.ManifestPath);

        Assert.Contains(
            "public const string DocumentsRead = \"documents.read\";",
            source,
            StringComparison.Ordinal);

        Assert.Contains(
            "public const string OrdersUpdate = \"orders.update\";",
            source,
            StringComparison.Ordinal);

        Assert.Contains(
            "public const string Document = \"document\";",
            source,
            StringComparison.Ordinal);

        Assert.Contains(
            "public const string Order = \"order\";",
            source,
            StringComparison.Ordinal);

        Assert.Contains(
            "public const string Read = \"read\";",
            source,
            StringComparison.Ordinal);

        Assert.Contains(
            "public const string Update = \"update\";",
            source,
            StringComparison.Ordinal);

        Assert.True(
            source.IndexOf(
                "DocumentsRead",
                StringComparison.Ordinal)
            <
            source.IndexOf(
                "OrdersUpdate",
                StringComparison.Ordinal));
    }

    [Fact]
    public async Task GenerateAsync_FailsClosedWhenManifestCompilationFails()
    {
        using var manifest =
            await TemporaryManifest.CreateAsync(
                InvalidManifest);

        var runner =
            CreateRunner();

        ManifestCSharpGenerationResult result =
            await runner.GenerateAsync(
                manifest.Path,
                "Sample.Authorization",
                CancellationToken.None);

        Assert.False(
            result.Compilation.IsSuccess);

        Assert.False(
            result.IsSuccess);

        Assert.Null(
            result.Generation);

        Assert.Null(
            result.Source);

        Assert.NotEmpty(
            result.Compilation.ValidationErrors);
    }

    [Fact]
    public async Task GenerateAsync_FailsClosedWhenGeneratedIdentifiersCollide()
    {
        using var manifest =
            await TemporaryManifest.CreateAsync(
                CollisionManifest);

        var runner =
            CreateRunner();

        ManifestCSharpGenerationResult result =
            await runner.GenerateAsync(
                manifest.Path,
                "Sample.Authorization",
                CancellationToken.None);

        Assert.True(
            result.Compilation.IsSuccess);

        Assert.False(
            result.IsSuccess);

        CSharpGenerationResult generation =
            Assert.IsType<CSharpGenerationResult>(
                result.Generation);

        Assert.Null(
            result.Source);

        CSharpGenerationDiagnostic diagnostic =
            Assert.Single(
                generation.Diagnostics);

        Assert.Equal(
            "RGCG004",
            diagnostic.Code);

        Assert.Contains(
            "OrdersRead",
            diagnostic.Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task GenerateAsync_UsesDefaultManifestFileName()
    {
        using var directory =
            TemporaryDirectory.Create();

        var manifestPath =
            Path.Combine(
                directory.Path,
                "rulegate.yaml");

        await File.WriteAllTextAsync(
            manifestPath,
            ValidManifest,
            CancellationToken.None);

        var runner =
            CreateRunner();

        var previousDirectory =
            Environment.CurrentDirectory;

        try
        {
            Environment.CurrentDirectory =
                directory.Path;

            ManifestCSharpGenerationResult result =
                await runner.GenerateAsync(
                    null,
                    "Sample.Authorization",
                    CancellationToken.None);

            Assert.True(
                result.IsSuccess);

            Assert.Equal(
                Path.GetFullPath(
                    manifestPath),
                result.ManifestPath);
        }
        finally
        {
            Environment.CurrentDirectory =
                previousDirectory;
        }
    }

    private static ManifestCSharpGenerationRunner
        CreateRunner() =>
        new(
            new RuleGateManifestCompiler(),
            new CSharpCodeGenerator());

    private sealed class TemporaryManifest :
        IDisposable
    {
        private TemporaryManifest(
            string path)
        {
            Path = path;
        }

        public string Path
        {
            get;
        }

        public static async Task<TemporaryManifest>
            CreateAsync(
                string contents)
        {
            var path =
                System.IO.Path.Combine(
                    System.IO.Path.GetTempPath(),
                    $"rulegate-generation-{Guid.NewGuid():N}.yaml");

            await File.WriteAllTextAsync(
                path,
                contents,
                CancellationToken.None);

            return new TemporaryManifest(
                path);
        }

        public void Dispose()
        {
            if (File.Exists(Path))
            {
                File.Delete(Path);
            }
        }
    }

    private sealed class TemporaryDirectory :
        IDisposable
    {
        private TemporaryDirectory(
            string path)
        {
            Path = path;
        }

        public string Path
        {
            get;
        }

        public static TemporaryDirectory Create()
        {
            var path =
                System.IO.Path.Combine(
                    System.IO.Path.GetTempPath(),
                    $"rulegate-generation-{Guid.NewGuid():N}");

            Directory.CreateDirectory(
                path);

            return new TemporaryDirectory(
                path);
        }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(
                    Path,
                    recursive: true);
            }
        }
    }
}
