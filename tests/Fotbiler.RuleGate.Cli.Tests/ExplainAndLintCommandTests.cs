using System.Globalization;
using System.Text.Json;
using Fotbiler.RuleGate.Cli.Linting;
using Fotbiler.RuleGate.Manifest.Loading;
using Fotbiler.RuleGate.Manifest.Validation;

namespace Fotbiler.RuleGate.Cli.Tests;

public sealed class ExplainAndLintCommandTests
{
    private const string ExplainManifest = """
        schemaVersion: 1

        application:
          id: explain-tests
          name: Explain Tests

        policies:
          - id: documents.read
            resourceType: document
            action: read
            requirement:
              id: read-requirements
              all:
                - id: reader-permission
                  permission: DOC.READ
                - id: matching-classification
                  attribute:
                    source: resource
                    name: classification
                    operator: equal
                    valueType: string
                    value: internal

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

    private const string ExplainFixture = """
        schemaVersion: 1
        manifest: rulegate.yaml

        tests:
          - id: denied-secret-request
            description: This description must not be included.
            request:
              subject:
                id: secret-user-42
                permissions: [DOC.READ]
              resource:
                type: document
                id: secret-document-99
                attributes:
                  - name: classification
                    valueType: string
                    value: top-secret-value
              action: read
              context:
                evaluationTime: '2026-07-31T09:00:00Z'
            expect:
              outcome: deny

          - id: no-matching-policy
            request:
              subject:
                id: unmatched-user
              resource:
                type: unknown
              action: unknown
              context:
                evaluationTime: '2026-07-31T09:00:00Z'
            expect:
              outcome: deny

          - id: indeterminate-type-mismatch
            request:
              subject:
                id: mismatched-user
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
        """;

    private const string CleanManifest = """
        schemaVersion: 1

        application:
          id: lint-clean
          name: Lint Clean

        policies:
          - id: documents.read
            resourceType: document
            action: read
            requirement:
              all:
                - permission: DOC.READ
                - role: DOCUMENT.READER
        """;

    private const string LintFindingManifest = """
        schemaVersion: 1

        application:
          id: lint-findings
          name: Lint Findings

        policies:
          - id: documents.read
            resourceType: document
            action: read
            requirement:
              id: documents.read
              all:
                - id: duplicated-id
                  permission: DOC.READ
                - permission: DOC.READ
                - not:
                    permission: DOC.READ
                - attribute:
                    source: resource
                    name: status
                    operator: equal
                    valueType: string
                    value: draft
                - attribute:
                    source: resource
                    name: status
                    operator: equal
                    valueType: string
                    value: approved
                - id: duplicated-id
                  role: DOCUMENT.READER
                - all:
                    - role: DOCUMENT.EDITOR
                - any:
                    - role: DOCUMENT.APPROVER
                    - all:
                        - role: DOCUMENT.APPROVER
                        - permission: DOC.APPROVE
                    - attribute:
                        source: subject
                        name: suspended
                        operator: notEqual
                        valueType: boolean
                        value: true
                - not:
                    not:
                      not:
                        not:
                          not:
                            not:
                              not:
                                not:
                                  role: DOCUMENT.BLOCKED
        """;

    [Fact]
    public async Task Explain_ProducesStableRedactedTree()
    {
        using var directory =
            new TemporaryDirectory();

        directory.WriteFile(
            "rulegate.yaml",
            ExplainManifest);

        var fixturePath =
            directory.WriteFile(
                "authorization.tests.yaml",
                ExplainFixture);

        var first =
            await RunAsync(
                "explain",
                fixturePath,
                "--test",
                "denied-secret-request",
                "--format",
                "json");

        var second =
            await RunAsync(
                "explain",
                fixturePath,
                "--test",
                "denied-secret-request",
                "--format",
                "json");

        Assert.Equal(0, first.ExitCode);
        Assert.Empty(first.StandardError);
        Assert.Equal(
            first.StandardOutput,
            second.StandardOutput);

        Assert.DoesNotContain(
            "secret-user-42",
            first.StandardOutput,
            StringComparison.Ordinal);

        Assert.DoesNotContain(
            "secret-document-99",
            first.StandardOutput,
            StringComparison.Ordinal);

        Assert.DoesNotContain(
            "top-secret-value",
            first.StandardOutput,
            StringComparison.Ordinal);

        Assert.DoesNotContain(
            "This description",
            first.StandardOutput,
            StringComparison.Ordinal);

        using var document =
            JsonDocument.Parse(
                first.StandardOutput);

        var root = document.RootElement;

        Assert.True(
            root.GetProperty(
                    "sensitiveValuesRedacted")
                .GetBoolean());

        Assert.Equal(
            "deny",
            root.GetProperty("outcome")
                .GetString());

        Assert.Equal(
            "all",
            root.GetProperty("requirements")[0]
                .GetProperty("kind")
                .GetString());

        Assert.Equal(
            "resource",
            root.GetProperty("requirements")[0]
                .GetProperty("children")[1]
                .GetProperty("attributeSource")
                .GetString());

        Assert.Equal(
            "classification",
            root.GetProperty("requirements")[0]
                .GetProperty("children")[1]
                .GetProperty("attributeName")
                .GetString());
    }

    [Fact]
    public async Task Explain_RequiresExactTestIdentifier()
    {
        using var directory =
            new TemporaryDirectory();

        directory.WriteFile(
            "rulegate.yaml",
            ExplainManifest);

        var fixturePath =
            directory.WriteFile(
                "authorization.tests.yaml",
                ExplainFixture);

        var result =
            await RunAsync(
                "explain",
                fixturePath,
                "--test",
                "unknown");

        Assert.Equal(1, result.ExitCode);
        Assert.Contains(
            "RGEXPLAIN_TEST_NOT_FOUND",
            result.StandardError,
            StringComparison.Ordinal);
        Assert.Empty(result.StandardOutput);
    }

    [Fact]
    public async Task Explain_PreservesDefaultDenyWithoutRequirementTree()
    {
        using var directory =
            new TemporaryDirectory();

        directory.WriteFile(
            "rulegate.yaml",
            ExplainManifest);

        var fixturePath =
            directory.WriteFile(
                "authorization.tests.yaml",
                ExplainFixture);

        var result =
            await RunAsync(
                "explain",
                fixturePath,
                "--test",
                "no-matching-policy",
                "--format",
                "json");

        Assert.Equal(0, result.ExitCode);

        using var document =
            JsonDocument.Parse(
                result.StandardOutput);

        var root = document.RootElement;

        Assert.Equal(
            "deny",
            root.GetProperty("outcome")
                .GetString());
        Assert.Equal(
            JsonValueKind.Null,
            root.GetProperty("policyId")
                .ValueKind);
        Assert.Empty(
            root.GetProperty("requirements")
                .EnumerateArray());
        Assert.Equal(
            "RULEGATE_NO_MATCHING_POLICY",
            root.GetProperty("failureCodes")[0]
                .GetString());
    }

    [Fact]
    public async Task Explain_PreservesIndeterminateRequirementOutcome()
    {
        using var directory =
            new TemporaryDirectory();

        directory.WriteFile(
            "rulegate.yaml",
            ExplainManifest);

        var fixturePath =
            directory.WriteFile(
                "authorization.tests.yaml",
                ExplainFixture);

        var result =
            await RunAsync(
                "explain",
                fixturePath,
                "--test",
                "indeterminate-type-mismatch",
                "--format",
                "json");

        Assert.Equal(0, result.ExitCode);

        using var document =
            JsonDocument.Parse(
                result.StandardOutput);

        var root = document.RootElement;

        Assert.Equal(
            "indeterminate",
            root.GetProperty("outcome")
                .GetString());
        Assert.Equal(
            "indeterminate",
            root.GetProperty("requirements")[0]
                .GetProperty("outcome")
                .GetString());
        Assert.Equal(
            "RULEGATE_ATTRIBUTE_TYPE_MISMATCH",
            root.GetProperty("failureCodes")[0]
                .GetString());
    }

    [Fact]
    public async Task Explain_RequiresTestOption()
    {
        var result =
            await RunAsync("explain");

        Assert.Equal(2, result.ExitCode);
        Assert.Contains(
            "--test",
            result.StandardError,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task Lint_ReturnsSuccessForCleanManifest()
    {
        using var directory =
            new TemporaryDirectory();

        var manifestPath =
            directory.WriteFile(
                "rulegate.yaml",
                CleanManifest);

        var result =
            await RunAsync(
                "lint",
                manifestPath);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains(
            "no lint findings",
            result.StandardOutput,
            StringComparison.Ordinal);
        Assert.Empty(result.StandardError);
    }

    [Fact]
    public async Task Lint_ReportsStableFindingCategories()
    {
        using var directory =
            new TemporaryDirectory();

        var manifestPath =
            directory.WriteFile(
                "rulegate.yaml",
                LintFindingManifest);

        var result =
            await RunAsync(
                "lint",
                manifestPath,
                "--format",
                "json");

        Assert.Equal(1, result.ExitCode);
        Assert.Empty(result.StandardError);

        using var document =
            JsonDocument.Parse(
                result.StandardOutput);

        var root = document.RootElement;

        Assert.True(
            root.GetProperty("isValid")
                .GetBoolean());
        Assert.False(
            root.GetProperty("isClean")
                .GetBoolean());

        var codes =
            root.GetProperty("findings")
                .EnumerateArray()
                .Select(
                    static item =>
                        item.GetProperty("code")
                            .GetString())
                .ToHashSet(
                    StringComparer.Ordinal);

        Assert.Contains(
            ManifestLintCodes.DuplicateRequirement,
            codes);
        Assert.Contains(
            ManifestLintCodes.ContradictoryRequirement,
            codes);
        Assert.Contains(
            ManifestLintCodes.UnreachableRequirement,
            codes);
        Assert.Contains(
            ManifestLintCodes.ExcessiveDepth,
            codes);
        Assert.Contains(
            ManifestLintCodes.UnnecessaryComplexity,
            codes);
        Assert.Contains(
            ManifestLintCodes.DuplicateRequirementId,
            codes);
        Assert.Contains(
            ManifestLintCodes.IdentifierCollision,
            codes);
        Assert.Contains(
            ManifestLintCodes.RiskyNegativeOperator,
            codes);
    }

    [Fact]
    public async Task Lint_FailsClosedForInvalidManifest()
    {
        using var directory =
            new TemporaryDirectory();

        var manifestPath =
            directory.WriteFile(
                "rulegate.yaml",
                """
                schemaVersion: 999
                application: null
                policies: []
                """);

        var result =
            await RunAsync(
                "lint",
                manifestPath);

        Assert.Equal(1, result.ExitCode);
        Assert.Contains(
            ManifestValidationCodes
                .UnsupportedSchemaVersion,
            result.StandardError,
            StringComparison.Ordinal);
        Assert.Empty(result.StandardOutput);
    }

    [Fact]
    public void LintFindingOrder_IsDeterministic()
    {
        var loadResult =
            new RuleGateManifestYamlLoader()
                .LoadFromText(
                    LintFindingManifest);

        Assert.True(loadResult.IsSuccess);

        var linter = new ManifestLinter();

        var first =
            linter.Analyze(
                loadResult.Manifest!);
        var second =
            linter.Analyze(
                loadResult.Manifest!);

        Assert.Equal(first, second);
    }

    private static async Task<CliResult> RunAsync(
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
                    "rulegate-explain-lint-tests-" +
                    Guid.NewGuid().ToString("N"));

            Directory.CreateDirectory(Path);
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
