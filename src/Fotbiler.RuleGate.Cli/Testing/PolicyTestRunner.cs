using Fotbiler.RuleGate.Abstractions.Authorization;
using Fotbiler.RuleGate.Abstractions.Constants;
using Fotbiler.RuleGate.Abstractions.Evaluation;
using Fotbiler.RuleGate.Cli.ExitCodes;
using Fotbiler.RuleGate.Cli.Output;
using Fotbiler.RuleGate.Core.Evaluation;
using Fotbiler.RuleGate.Core.Evaluation.Evaluators;
using Fotbiler.RuleGate.Core.Policies;
using Fotbiler.RuleGate.Manifest.Compilation;

namespace Fotbiler.RuleGate.Cli.Testing;

internal sealed class PolicyTestRunner
{
    private readonly PolicyTestFixtureCompiler
        _fixtureCompiler;

    private readonly RuleGateManifestCompiler
        _manifestCompiler;

    public PolicyTestRunner(
        PolicyTestFixtureCompiler fixtureCompiler,
        RuleGateManifestCompiler manifestCompiler)
    {
        ArgumentNullException.ThrowIfNull(
            fixtureCompiler);
        ArgumentNullException.ThrowIfNull(
            manifestCompiler);

        _fixtureCompiler = fixtureCompiler;
        _manifestCompiler = manifestCompiler;
    }

    public async Task<int> RunAsync(
        string? path,
        string? filter,
        ValidationOutputFormat outputFormat,
        TextWriter output,
        TextWriter error,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);

        var requestedPath =
            string.IsNullOrWhiteSpace(path)
                ? PolicyTestDefaults.FileName
                : path;

        var fullPath =
            Path.GetFullPath(requestedPath);

        var fixtureCompilation =
            await _fixtureCompiler
                .CompileFromFileAsync(
                    fullPath,
                    cancellationToken);

        PolicyTestReport report;

        if (!fixtureCompilation.IsSuccess)
        {
            report = new PolicyTestReport(
                IsValid: false,
                IsSuccess: false,
                Fixture: fullPath,
                Manifest: null,
                Filter: NormalizeFilter(filter),
                TotalTestCount: 0,
                SelectedTestCount: 0,
                PassedTestCount: 0,
                FailedTestCount: 0,
                Errors: fixtureCompilation.Errors,
                Tests:
                    Array.Empty<PolicyTestCaseResult>());

            WriteReport(
                report,
                outputFormat,
                output,
                error);

            return RuleGateExitCodes.TestFailed;
        }

        var suite = fixtureCompilation.Suite!;
        var manifestCompilation =
            await _manifestCompiler
                .CompileFromFileAsync(
                    suite.ManifestPath,
                    cancellationToken);

        if (!manifestCompilation.IsSuccess)
        {
            report = new PolicyTestReport(
                IsValid: false,
                IsSuccess: false,
                Fixture: suite.FixturePath,
                Manifest: suite.ManifestPath,
                Filter: NormalizeFilter(filter),
                TotalTestCount: suite.Tests.Count,
                SelectedTestCount: 0,
                PassedTestCount: 0,
                FailedTestCount: 0,
                Errors:
                    CreateManifestDiagnostics(
                        manifestCompilation),
                Tests:
                    Array.Empty<PolicyTestCaseResult>());

            WriteReport(
                report,
                outputFormat,
                output,
                error);

            return RuleGateExitCodes.TestFailed;
        }

        var normalizedFilter =
            NormalizeFilter(filter);

        var selectedTests =
            suite.Tests
                .Where(
                    test =>
                        normalizedFilter is null ||
                        test.Id.Contains(
                            normalizedFilter,
                            StringComparison
                                .OrdinalIgnoreCase))
                .ToArray();

        if (selectedTests.Length == 0)
        {
            report = new PolicyTestReport(
                IsValid: false,
                IsSuccess: false,
                Fixture: suite.FixturePath,
                Manifest: suite.ManifestPath,
                Filter: normalizedFilter,
                TotalTestCount: suite.Tests.Count,
                SelectedTestCount: 0,
                PassedTestCount: 0,
                FailedTestCount: 0,
                Errors:
                [
                    new PolicyTestDiagnostic(
                        Category: "filter",
                        Code:
                            PolicyTestDiagnosticCodes
                                .FilterNoMatch,
                        Message:
                            "No policy test identifiers matched the filter.")
                ],
                Tests:
                    Array.Empty<PolicyTestCaseResult>());

            WriteReport(
                report,
                outputFormat,
                output,
                error);

            return RuleGateExitCodes.TestFailed;
        }

        var provider =
            new InMemoryPolicyProvider(
                manifestCompilation.Policies);

        var dispatcher = CreateDispatcher();
        var results =
            new List<PolicyTestCaseResult>(
                selectedTests.Length);

        foreach (var test in selectedTests)
        {
            cancellationToken.ThrowIfCancellationRequested();

            results.Add(
                await EvaluateAsync(
                    test,
                    provider,
                    dispatcher,
                    cancellationToken));
        }

        var passedCount =
            results.Count(
                static result => result.Passed);

        var failedCount =
            results.Count - passedCount;

        report = new PolicyTestReport(
            IsValid: true,
            IsSuccess: failedCount == 0,
            Fixture: suite.FixturePath,
            Manifest: suite.ManifestPath,
            Filter: normalizedFilter,
            TotalTestCount: suite.Tests.Count,
            SelectedTestCount: selectedTests.Length,
            PassedTestCount: passedCount,
            FailedTestCount: failedCount,
            Errors:
                Array.Empty<PolicyTestDiagnostic>(),
            Tests: results.AsReadOnly());

        WriteReport(
            report,
            outputFormat,
            output,
            error);

        return report.IsSuccess
            ? RuleGateExitCodes.Success
            : RuleGateExitCodes.TestFailed;
    }

    private static async ValueTask<PolicyTestCaseResult>
        EvaluateAsync(
            PolicyTestCase test,
            InMemoryPolicyProvider provider,
            RequirementEvaluationDispatcher dispatcher,
            CancellationToken cancellationToken)
    {
        var policy =
            await provider.FindAsync(
                test.Request.Resource.Type,
                test.Request.Action,
                cancellationToken);

        RequirementEvaluationResult evaluation;

        if (policy is null)
        {
            evaluation =
                RequirementEvaluationResult.NotSatisfied(
                    new AuthorizationFailure(
                        AuthorizationFailureCodes
                            .NoMatchingPolicy));
        }
        else
        {
            evaluation =
                await dispatcher.EvaluateAsync(
                    policy.Requirement,
                    new RequirementEvaluationContext(
                        test.Request),
                    cancellationToken);
        }

        var actualOutcome =
            evaluation.Outcome switch
            {
                RequirementEvaluationOutcome.Satisfied =>
                    "allow",

                RequirementEvaluationOutcome.NotSatisfied =>
                    "deny",

                RequirementEvaluationOutcome.Indeterminate =>
                    "indeterminate",

                _ => throw new ArgumentOutOfRangeException(
                    nameof(evaluation),
                    evaluation.Outcome,
                    "Unsupported requirement evaluation outcome.")
            };

        var actualFailureCodes =
            evaluation.Failures
                .Select(
                    static failure => failure.Code)
                .Distinct(
                    StringComparer.Ordinal)
                .Order(
                    StringComparer.Ordinal)
                .ToArray();

        var passed =
            string.Equals(
                test.ExpectedOutcome,
                actualOutcome,
                StringComparison.Ordinal) &&
            (test.ExpectedFailureCodes is null ||
             test.ExpectedFailureCodes.SequenceEqual(
                 actualFailureCodes,
                 StringComparer.Ordinal));

        return new PolicyTestCaseResult(
            Id: test.Id,
            Description: test.Description,
            Passed: passed,
            ExpectedOutcome: test.ExpectedOutcome,
            ActualOutcome: actualOutcome,
            ExpectedFailureCodes:
                test.ExpectedFailureCodes,
            ActualFailureCodes:
                actualFailureCodes,
            PolicyId: policy?.Id);
    }

    private static RequirementEvaluationDispatcher
        CreateDispatcher()
    {
        return new RequirementEvaluationDispatcher(
        [
            new PermissionRequirementEvaluator(),
            new RoleRequirementEvaluator(),
            new AttributeRequirementEvaluator(),
            new AttributeComparisonRequirementEvaluator(),
            new TimeWindowRequirementEvaluator(),
            new DateTimeWindowRequirementEvaluator(),
            new ContextAgeRequirementEvaluator(),
            new ContextRequirementEvaluator(),
            new AllRequirementEvaluator(),
            new AnyRequirementEvaluator(),
            new NotRequirementEvaluator()
        ]);
    }

    private static IReadOnlyList<PolicyTestDiagnostic>
        CreateManifestDiagnostics(
            ManifestCompilationResult compilation)
    {
        var loadErrors =
            compilation.LoadErrors.Select(
                static item =>
                    new PolicyTestDiagnostic(
                        Category: "manifest",
                        Code: item.Code,
                        Message: item.Message,
                        Line: item.Line,
                        Column: item.Column));

        var validationErrors =
            compilation.ValidationErrors.Select(
                static item =>
                    new PolicyTestDiagnostic(
                        Category: "manifest",
                        Code: item.Code,
                        Message: item.Message,
                        Path: item.Path));

        return loadErrors
            .Concat(validationErrors)
            .ToArray();
    }

    private static string? NormalizeFilter(
        string? filter)
    {
        return string.IsNullOrWhiteSpace(filter)
            ? null
            : filter;
    }

    private static void WriteReport(
        PolicyTestReport report,
        ValidationOutputFormat outputFormat,
        TextWriter output,
        TextWriter error)
    {
        IPolicyTestReporter reporter =
            outputFormat switch
            {
                ValidationOutputFormat.Text =>
                    new TextPolicyTestReporter(),

                ValidationOutputFormat.Json =>
                    new JsonPolicyTestReporter(),

                _ => throw new ArgumentOutOfRangeException(
                    nameof(outputFormat),
                    outputFormat,
                    "Unsupported policy test output format.")
            };

        reporter.Write(
            report,
            output,
            error);
    }
}
