using System.Globalization;
using System.Text.Json;
using Fotbiler.RuleGate.Abstractions.Constants;
using Fotbiler.RuleGate.Cli;
using Fotbiler.RuleGate.Cli.Testing;
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

    private const string PolicyTestManifest = """
        schemaVersion: 1

        application:
          id: policy-test-cli-tests
          name: Policy Test CLI Tests

        policies:
          - id: documents.read
            resourceType: document
            action: read
            requirement:
              permission: DOC.READ

          - id: documents.score
            resourceType: document
            action: score
            requirement:
              attribute:
                source: subject
                name: clearance
                operator: greaterThanOrEqual
                valueType: number
                value: 3
        """;

    private const string ValidPolicyTests = """
        schemaVersion: 1
        manifest: rulegate.yaml

        tests:
          - id: reader-is-allowed
            request:
              subject:
                id: user-1
                permissions: [DOC.READ]
              resource:
                type: document
                id: document-1
              action: read
              context:
                evaluationTime: '2026-07-31T09:00:00Z'
            expect:
              outcome: allow

          - id: missing-permission-is-denied
            request:
              subject:
                id: user-2
              resource:
                type: document
                id: document-1
              action: read
              context:
                evaluationTime: '2026-07-31T09:00:00Z'
            expect:
              outcome: deny
              failureCodes:
                - RULEGATE_MISSING_PERMISSION

          - id: incompatible-attribute-is-indeterminate
            request:
              subject:
                id: user-3
                attributes:
                  - name: clearance
                    valueType: string
                    value: high
              resource:
                type: document
              action: score
              context:
                evaluationTime: '2026-07-31T09:00:00Z'
            expect:
              outcome: indeterminate
              failureCodes:
                - RULEGATE_ATTRIBUTE_TYPE_MISMATCH
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
            "generate",
            result.StandardOutput,
            StringComparison.Ordinal);

        Assert.Contains(
            "test",
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
            "Validate, test, generate, and manage RuleGate policies.",
            result.StandardOutput,
            StringComparison.Ordinal);

        Assert.Contains(
            "--version",
            result.StandardOutput,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task Help_UsesInstalledCommandName()
    {
        var result =
            await RunAsync("--help");

        Assert.Equal(
            0,
            result.ExitCode);

        Assert.Contains(
            "rulegate [command] [options]",
            result.StandardOutput,
            StringComparison.Ordinal);

        Assert.DoesNotContain(
            "Fotbiler.RuleGate.Cli [command]",
            result.StandardOutput,
            StringComparison.Ordinal);

        Assert.Empty(
            result.StandardError);
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

        Assert.Contains(
            "Default policy tests: authorization.tests.yaml",
            result.StandardOutput,
            StringComparison.Ordinal);

        Assert.Contains(
            "Supported policy test schema version: 1",
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

    [Fact]
    public async Task Test_RunsAllowDenyAndIndeterminateExpectations()
    {
        using var directory =
            new TemporaryDirectory();

        directory.WriteFile(
            RuleGateManifestDefaults.FileName,
            PolicyTestManifest);

        var fixturePath =
            directory.WriteFile(
                "authorization.tests.yaml",
                ValidPolicyTests);

        var result =
            await RunAsync(
                "test",
                fixturePath);

        Assert.Equal(
            0,
            result.ExitCode);

        Assert.Contains(
            "PASS reader-is-allowed",
            result.StandardOutput,
            StringComparison.Ordinal);

        Assert.Contains(
            "PASS missing-permission-is-denied",
            result.StandardOutput,
            StringComparison.Ordinal);

        Assert.Contains(
            "PASS incompatible-attribute-is-indeterminate",
            result.StandardOutput,
            StringComparison.Ordinal);

        Assert.Contains(
            "Summary: 3 passed, 0 failed, 3 selected of 3 total.",
            result.StandardOutput,
            StringComparison.Ordinal);

        Assert.Empty(
            result.StandardError);
    }

    [Fact]
    public async Task Test_FilterAndJsonOutputAreMachineReadable()
    {
        using var directory =
            new TemporaryDirectory();

        directory.WriteFile(
            RuleGateManifestDefaults.FileName,
            PolicyTestManifest);

        var fixturePath =
            directory.WriteFile(
                "authorization.tests.yaml",
                ValidPolicyTests);

        var result =
            await RunAsync(
                "test",
                fixturePath,
                "--filter",
                "INDETERMINATE",
                "--format",
                "json");

        Assert.Equal(
            0,
            result.ExitCode);

        Assert.Empty(
            result.StandardError);

        using var document =
            JsonDocument.Parse(
                result.StandardOutput);

        var root = document.RootElement;

        Assert.True(
            root.GetProperty("isSuccess")
                .GetBoolean());

        Assert.Equal(
            3,
            root.GetProperty("totalTestCount")
                .GetInt32());

        Assert.Equal(
            1,
            root.GetProperty("selectedTestCount")
                .GetInt32());

        Assert.Equal(
            "indeterminate",
            root.GetProperty("tests")[0]
                .GetProperty("actualOutcome")
                .GetString());

        Assert.Equal(
            AuthorizationFailureCodes
                .AttributeTypeMismatch,
            root.GetProperty("tests")[0]
                .GetProperty("actualFailureCodes")[0]
                .GetString());
    }

    [Fact]
    public async Task Test_ReturnsFailureForMismatchedExpectation()
    {
        using var directory =
            new TemporaryDirectory();

        directory.WriteFile(
            RuleGateManifestDefaults.FileName,
            PolicyTestManifest);

        var fixturePath =
            directory.WriteFile(
                "authorization.tests.yaml",
                ValidPolicyTests.Replace(
                    "outcome: allow",
                    "outcome: deny",
                    StringComparison.Ordinal));

        var result =
            await RunAsync(
                "test",
                fixturePath);

        Assert.Equal(
            1,
            result.ExitCode);

        Assert.Contains(
            "FAIL reader-is-allowed",
            result.StandardOutput,
            StringComparison.Ordinal);

        Assert.Contains(
            "Expected: deny",
            result.StandardOutput,
            StringComparison.Ordinal);

        Assert.Contains(
            "Actual:   allow",
            result.StandardOutput,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task Test_ReturnsFixtureDiagnosticsWithoutStartingEvaluation()
    {
        using var directory =
            new TemporaryDirectory();

        var fixturePath =
            directory.WriteFile(
                "invalid.tests.yaml",
                """
                schemaVersion: 1
                manifest: rulegate.yaml
                tests:
                  - id: missing-context
                    request:
                      subject:
                        id: user-1
                      resource:
                        type: document
                      action: read
                    expect:
                      outcome: allow
                """);

        var result =
            await RunAsync(
                "test",
                fixturePath);

        Assert.Equal(
            1,
            result.ExitCode);

        Assert.Contains(
            "RGTEST_INVALID_FIXTURE",
            result.StandardError,
            StringComparison.Ordinal);

        Assert.Contains(
            "tests[0].request.context",
            result.StandardError,
            StringComparison.Ordinal);

        Assert.Empty(
            result.StandardOutput);
    }

    [Fact]
    public async Task Test_ReturnsFailureWhenFilterMatchesNoTests()
    {
        using var directory =
            new TemporaryDirectory();

        directory.WriteFile(
            RuleGateManifestDefaults.FileName,
            PolicyTestManifest);

        var fixturePath =
            directory.WriteFile(
                "authorization.tests.yaml",
                ValidPolicyTests);

        var result =
            await RunAsync(
                "test",
                fixturePath,
                "--filter",
                "not-present");

        Assert.Equal(
            1,
            result.ExitCode);

        Assert.Contains(
            "RGTEST_FILTER_NO_MATCH",
            result.StandardError,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task Test_DenyExpectationMayOmitFailureCodeAssertion()
    {
        using var directory =
            new TemporaryDirectory();

        directory.WriteFile(
            RuleGateManifestDefaults.FileName,
            PolicyTestManifest);

        var fixturePath =
            directory.WriteFile(
                "authorization.tests.yaml",
                ValidPolicyTests.Replace(
                    "              failureCodes:\n" +
                    "                - RULEGATE_MISSING_PERMISSION\n",
                    string.Empty,
                    StringComparison.Ordinal));

        var result =
            await RunAsync(
                "test",
                fixturePath);

        Assert.Equal(
            0,
            result.ExitCode);

        Assert.Contains(
            "PASS missing-permission-is-denied",
            result.StandardOutput,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task Test_ReturnsFailureWhenExpectedFailureCodesDiffer()
    {
        using var directory =
            new TemporaryDirectory();

        directory.WriteFile(
            RuleGateManifestDefaults.FileName,
            PolicyTestManifest);

        var fixturePath =
            directory.WriteFile(
                "authorization.tests.yaml",
                ValidPolicyTests.Replace(
                    AuthorizationFailureCodes
                        .MissingPermission,
                    AuthorizationFailureCodes
                        .MissingRole,
                    StringComparison.Ordinal));

        var result =
            await RunAsync(
                "test",
                fixturePath);

        Assert.Equal(
            1,
            result.ExitCode);

        Assert.Contains(
            AuthorizationFailureCodes.MissingRole,
            result.StandardOutput,
            StringComparison.Ordinal);

        Assert.Contains(
            AuthorizationFailureCodes
                .MissingPermission,
            result.StandardOutput,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task Test_ReturnsManifestDiagnosticsAsJson()
    {
        using var directory =
            new TemporaryDirectory();

        directory.WriteFile(
            RuleGateManifestDefaults.FileName,
            """
            schemaVersion: 999
            application:
              id: invalid
              name: Invalid
            policies: []
            """);

        var fixturePath =
            directory.WriteFile(
                "authorization.tests.yaml",
                ValidPolicyTests);

        var result =
            await RunAsync(
                "test",
                fixturePath,
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

        var root = document.RootElement;

        Assert.False(
            root.GetProperty("isValid")
                .GetBoolean());

        Assert.Equal(
            ManifestValidationCodes
                .UnsupportedSchemaVersion,
            root.GetProperty("errors")[0]
                .GetProperty("code")
                .GetString());
    }

    [Fact]
    public async Task Test_RejectsDuplicateFixtureKeys()
    {
        using var directory =
            new TemporaryDirectory();

        var fixturePath =
            directory.WriteFile(
                "authorization.tests.yaml",
                """
                schemaVersion: 1
                schemaVersion: 1
                manifest: rulegate.yaml
                tests: []
                """);

        var result =
            await RunAsync(
                "test",
                fixturePath);

        Assert.Equal(
            1,
            result.ExitCode);

        Assert.Contains(
            PolicyTestDiagnosticCodes.InvalidYaml,
            result.StandardError,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task Test_RejectsUnsupportedTypedAttributeValue()
    {
        using var directory =
            new TemporaryDirectory();

        var fixturePath =
            directory.WriteFile(
                "authorization.tests.yaml",
                ValidPolicyTests.Replace(
                    "valueType: string",
                    "valueType: object",
                    StringComparison.Ordinal));

        var result =
            await RunAsync(
                "test",
                fixturePath);

        Assert.Equal(
            1,
            result.ExitCode);

        Assert.Contains(
            "valueType must be",
            result.StandardError,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task TestFixture_ParsesZEvaluationTimeAsUtc()
    {
        using var directory =
            new TemporaryDirectory();

        var fixturePath =
            directory.WriteFile(
                "authorization.tests.yaml",
                ValidPolicyTests);

        var compilation =
            await new PolicyTestFixtureCompiler()
                .CompileFromFileAsync(
                    fixturePath,
                    CancellationToken.None);

        Assert.True(
            compilation.IsSuccess);

        Assert.Equal(
            TimeSpan.Zero,
            compilation.Suite!.Tests[0]
                .Request.Context.EvaluationTime.Offset);

        Assert.Equal(
            new DateTimeOffset(
                2026,
                7,
                31,
                9,
                0,
                0,
                TimeSpan.Zero),
            compilation.Suite.Tests[0]
                .Request.Context.EvaluationTime);
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
