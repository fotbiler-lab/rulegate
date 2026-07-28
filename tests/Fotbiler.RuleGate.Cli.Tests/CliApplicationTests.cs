using System.Globalization;
using System.Text.Json;
using Fotbiler.RuleGate.Cli;
using Fotbiler.RuleGate.Manifest.Configuration;
using Fotbiler.RuleGate.Manifest.Loading;
using Fotbiler.RuleGate.Manifest.Validation;

namespace Fotbiler.RuleGate.Cli.Tests;

public sealed class CliApplicationTests
{
    private const string ValidManifest = """
        schemaVersion: 1

        application:
          id: cli-tests
          name: CLI Tests

        policies:
          - id: documents.read
            resourceType: document
            action: read
            requirement:
              permission: DOC.READ
        """;

    [Fact]
    public async Task NoArguments_ShowsRootHelp()
    {
        var result =
            await RunAsync();

        Assert.Equal(
            0,
            result.ExitCode);

        Assert.Contains(
            "validate",
            result.StandardOutput,
            StringComparison.Ordinal);

        Assert.Contains(
            "info",
            result.StandardOutput,
            StringComparison.Ordinal);

        Assert.Empty(
            result.StandardError);
    }

    [Fact]
    public async Task Help_ShowsCommands()
    {
        var result =
            await RunAsync("--help");

        Assert.Equal(
            0,
            result.ExitCode);

        Assert.Contains(
            "Validate and manage Fotbiler RuleGate policies.",
            result.StandardOutput,
            StringComparison.Ordinal);

        Assert.Contains(
            "--version",
            result.StandardOutput,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task Version_ReturnsVersionInformation()
    {
        var result =
            await RunAsync("--version");

        Assert.Equal(
            0,
            result.ExitCode);

        Assert.False(
            string.IsNullOrWhiteSpace(
                result.StandardOutput));

        Assert.Empty(
            result.StandardError);
    }

    [Fact]
    public async Task Info_ReturnsSafeRuntimeInformation()
    {
        var result =
            await RunAsync("info");

        Assert.Equal(
            0,
            result.ExitCode);

        Assert.Contains(
            "Fotbiler RuleGate CLI",
            result.StandardOutput,
            StringComparison.Ordinal);

        Assert.Contains(
            $"Default manifest: {RuleGateManifestDefaults.FileName}",
            result.StandardOutput,
            StringComparison.Ordinal);

        Assert.Contains(
            "Supported schema version: 1",
            result.StandardOutput,
            StringComparison.Ordinal);

        Assert.Empty(
            result.StandardError);
    }

    [Fact]
    public async Task Validate_CompilesExplicitValidManifest()
    {
        using var directory =
            new TemporaryDirectory();

        var path =
            directory.WriteFile(
                "custom.yaml",
                ValidManifest);

        var result =
            await RunAsync(
                "validate",
                path);

        Assert.Equal(
            0,
            result.ExitCode);

        Assert.Contains(
            "RuleGate manifest is valid.",
            result.StandardOutput,
            StringComparison.Ordinal);

        Assert.Contains(
            "Policies: 1",
            result.StandardOutput,
            StringComparison.Ordinal);

        Assert.Empty(
            result.StandardError);
    }

    [Fact]
    public async Task Validate_DiscoversDefaultManifest()
    {
        using var directory =
            new TemporaryDirectory();

        directory.WriteFile(
            RuleGateManifestDefaults.FileName,
            ValidManifest);

        var originalDirectory =
            Environment.CurrentDirectory;

        try
        {
            Environment.CurrentDirectory =
                directory.Path;

            var result =
                await RunAsync(
                    "validate");

            Assert.Equal(
                0,
                result.ExitCode);

            Assert.Contains(
                RuleGateManifestDefaults.FileName,
                result.StandardOutput,
                StringComparison.Ordinal);
        }
        finally
        {
            Environment.CurrentDirectory =
                originalDirectory;
        }
    }

    [Fact]
    public async Task Validate_ReturnsValidationExitCode()
    {
        using var directory =
            new TemporaryDirectory();

        var path =
            directory.WriteFile(
                "invalid.yaml",
                """
                schemaVersion: 999

                application:
                  id: cli-tests
                  name: CLI Tests

                policies: []
                """);

        var result =
            await RunAsync(
                "validate",
                path);

        Assert.Equal(
            1,
            result.ExitCode);

        Assert.Contains(
            ManifestValidationCodes
                .UnsupportedSchemaVersion,
            result.StandardError,
            StringComparison.Ordinal);

        Assert.Empty(
            result.StandardOutput);
    }

    [Fact]
    public async Task Validate_ReturnsLoadExitCode()
    {
        using var directory =
            new TemporaryDirectory();

        var path =
            System.IO.Path.Combine(
                directory.Path,
                "missing.yaml");

        var result =
            await RunAsync(
                "validate",
                path);

        Assert.Equal(
            1,
            result.ExitCode);

        Assert.Contains(
            ManifestLoadCodes.FileNotFound,
            result.StandardError,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task Validate_JsonOutputIsMachineReadable()
    {
        using var directory =
            new TemporaryDirectory();

        var path =
            directory.WriteFile(
                "invalid.yaml",
                """
                schemaVersion: 999

                application:
                  id: cli-tests
                  name: CLI Tests

                policies: []
                """);

        var result =
            await RunAsync(
                "validate",
                path,
                "--format",
                "json");

        Assert.Equal(
            1,
            result.ExitCode);

        Assert.Empty(
            result.StandardError);

        using var document =
            JsonDocument.Parse(
                result.StandardOutput);

        var root =
            document.RootElement;

        Assert.False(
            root.GetProperty(
                    "isValid")
                .GetBoolean());

        Assert.Equal(
            0,
            root.GetProperty(
                    "policyCount")
                .GetInt32());

        var error =
            root.GetProperty(
                    "errors")[0];

        Assert.Equal(
            "validation",
            error.GetProperty(
                    "category")
                .GetString());

        Assert.Equal(
            ManifestValidationCodes
                .UnsupportedSchemaVersion,
            error.GetProperty(
                    "code")
                .GetString());
    }

    [Fact]
    public async Task InvalidFormat_ReturnsUsageExitCode()
    {
        var result =
            await RunAsync(
                "validate",
                "--format",
                "xml");

        Assert.Equal(
            2,
            result.ExitCode);

        Assert.Contains(
            "error:",
            result.StandardError,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task UnknownCommand_ReturnsUsageExitCode()
    {
        var result =
            await RunAsync(
                "validte");

        Assert.Equal(
            2,
            result.ExitCode);

        Assert.Contains(
            "error:",
            result.StandardError,
            StringComparison.Ordinal);

        Assert.Contains(
            "rulegate --help",
            result.StandardError,
            StringComparison.Ordinal);
    }

    private static async Task<CliResult>
        RunAsync(
            params string[] arguments)
    {
        using var output =
            new StringWriter(
                CultureInfo.InvariantCulture);

        using var error =
            new StringWriter(
                CultureInfo.InvariantCulture);

        var exitCode =
            await RuleGateCliApplication.RunAsync(
                arguments,
                output,
                error);

        return new CliResult(
            exitCode,
            output.ToString(),
            error.ToString());
    }

    private sealed record CliResult(
        int ExitCode,
        string StandardOutput,
        string StandardError);

    private sealed class TemporaryDirectory :
        IDisposable
    {
        public TemporaryDirectory()
        {
            Path =
                System.IO.Path.Combine(
                    System.IO.Path.GetTempPath(),
                    "rulegate-cli-tests-" +
                    Guid.NewGuid()
                        .ToString("N"));

            Directory.CreateDirectory(
                Path);
        }

        public string Path { get; }

        public string WriteFile(
            string fileName,
            string content)
        {
            var path =
                System.IO.Path.Combine(
                    Path,
                    fileName);

            File.WriteAllText(
                path,
                content);

            return path;
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
