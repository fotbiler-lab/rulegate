using Fotbiler.RuleGate.Abstractions.Authorization;

namespace Fotbiler.RuleGate.Cli.Testing;

internal static class PolicyTestDefaults
{
    public const string FileName =
        "authorization.tests.yaml";

    public const int SupportedSchemaVersion = 1;
}

internal static class PolicyTestDiagnosticCodes
{
    public const string FileNotFound =
        "RGTEST_FILE_NOT_FOUND";

    public const string FileReadFailed =
        "RGTEST_FILE_READ_FAILED";

    public const string EmptyContent =
        "RGTEST_EMPTY_CONTENT";

    public const string InvalidYaml =
        "RGTEST_INVALID_YAML";

    public const string InvalidFixture =
        "RGTEST_INVALID_FIXTURE";

    public const string FilterNoMatch =
        "RGTEST_FILTER_NO_MATCH";
}

internal sealed class PolicyTestFixture
{
    public int SchemaVersion { get; set; }

    public string? Manifest { get; set; }

    public List<PolicyTestCaseFixture?>? Tests { get; set; }
}

internal sealed class PolicyTestCaseFixture
{
    public string? Id { get; set; }

    public string? Description { get; set; }

    public PolicyTestRequestFixture? Request { get; set; }

    public PolicyTestExpectationFixture? Expect { get; set; }
}

internal sealed class PolicyTestRequestFixture
{
    public PolicyTestSubjectFixture? Subject { get; set; }

    public PolicyTestResourceFixture? Resource { get; set; }

    public string? Action { get; set; }

    public PolicyTestContextFixture? Context { get; set; }
}

internal sealed class PolicyTestSubjectFixture
{
    public string? Id { get; set; }

    public List<string?>? Roles { get; set; }

    public List<string?>? Permissions { get; set; }

    public List<PolicyTestAttributeFixture?>? Attributes { get; set; }
}

internal sealed class PolicyTestResourceFixture
{
    public string? Type { get; set; }

    public string? Id { get; set; }

    public List<PolicyTestAttributeFixture?>? Attributes { get; set; }
}

internal sealed class PolicyTestContextFixture
{
    public string? EvaluationTime { get; set; }

    public List<PolicyTestAttributeFixture?>? Attributes { get; set; }
}

internal sealed class PolicyTestAttributeFixture
{
    public string? Name { get; set; }

    public string? ValueType { get; set; }

    public string? Value { get; set; }

    public List<string?>? Values { get; set; }
}

internal sealed class PolicyTestExpectationFixture
{
    public string? Outcome { get; set; }

    public List<string?>? FailureCodes { get; set; }
}

internal sealed record PolicyTestCase(
    string Id,
    string? Description,
    AuthorizationRequest Request,
    string ExpectedOutcome,
    IReadOnlyList<string>? ExpectedFailureCodes);

internal sealed record PolicyTestSuite(
    string FixturePath,
    string ManifestPath,
    IReadOnlyList<PolicyTestCase> Tests);

internal sealed record PolicyTestDiagnostic(
    string Category,
    string Code,
    string Message,
    string? Path = null,
    long? Line = null,
    long? Column = null);

internal sealed record PolicyTestFixtureCompilation(
    PolicyTestSuite? Suite,
    IReadOnlyList<PolicyTestDiagnostic> Errors)
{
    public bool IsSuccess =>
        Suite is not null &&
        Errors.Count == 0;
}

internal sealed record PolicyTestCaseResult(
    string Id,
    string? Description,
    bool Passed,
    string ExpectedOutcome,
    string ActualOutcome,
    IReadOnlyList<string>? ExpectedFailureCodes,
    IReadOnlyList<string> ActualFailureCodes,
    string? PolicyId);

internal sealed record PolicyTestReport(
    bool IsValid,
    bool IsSuccess,
    string Fixture,
    string? Manifest,
    string? Filter,
    int TotalTestCount,
    int SelectedTestCount,
    int PassedTestCount,
    int FailedTestCount,
    IReadOnlyList<PolicyTestDiagnostic> Errors,
    IReadOnlyList<PolicyTestCaseResult> Tests);
