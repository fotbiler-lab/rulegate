using Fotbiler.RuleGate.Abstractions.Authorization;
using Fotbiler.RuleGate.Abstractions.Diagnostics;
using Fotbiler.RuleGate.Abstractions.Evaluation;
using Fotbiler.RuleGate.Cli.Evaluation;
using Fotbiler.RuleGate.Cli.ExitCodes;
using Fotbiler.RuleGate.Cli.Output;
using Fotbiler.RuleGate.Cli.Testing;
using Fotbiler.RuleGate.Core.Engine;
using Fotbiler.RuleGate.Core.Policies;
using Fotbiler.RuleGate.Manifest.Compilation;

namespace Fotbiler.RuleGate.Cli.Explanation;

internal sealed class PolicyExplanationRunner
{
    private readonly PolicyTestFixtureCompiler
        _fixtureCompiler;

    private readonly RuleGateManifestCompiler
        _manifestCompiler;

    public PolicyExplanationRunner(
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
        string testId,
        ValidationOutputFormat outputFormat,
        TextWriter output,
        TextWriter error,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            testId);
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

        if (!fixtureCompilation.IsSuccess)
        {
            return WriteFailure(
                fullPath,
                manifest: null,
                testId,
                fixtureCompilation.Errors,
                outputFormat,
                output,
                error);
        }

        var suite = fixtureCompilation.Suite!;
        var manifestCompilation =
            await _manifestCompiler
                .CompileFromFileAsync(
                    suite.ManifestPath,
                    cancellationToken);

        if (!manifestCompilation.IsSuccess)
        {
            return WriteFailure(
                suite.FixturePath,
                suite.ManifestPath,
                testId,
                CreateManifestDiagnostics(
                    manifestCompilation),
                outputFormat,
                output,
                error);
        }

        var test =
            suite.Tests.SingleOrDefault(
                item => string.Equals(
                    item.Id,
                    testId,
                    StringComparison.Ordinal));

        if (test is null)
        {
            return WriteFailure(
                suite.FixturePath,
                suite.ManifestPath,
                testId,
                [
                    new PolicyTestDiagnostic(
                        Category: "selection",
                        Code:
                            PolicyExplanationDiagnosticCodes
                                .TestNotFound,
                        Message:
                            $"Policy test identifier '{testId}' was not found.")
                ],
                outputFormat,
                output,
                error);
        }

        var sink =
            new CapturingAuthorizationDiagnosticsSink();

        var engine =
            new PolicyAuthorizationEngine(
                new InMemoryPolicyProvider(
                    manifestCompilation.Policies),
                RequirementEvaluationDispatcherFactory
                    .Create(),
                sink);

        AuthorizationDecision decision =
            await engine.EvaluateAsync(
                test.Request,
                cancellationToken);

        var diagnostic = sink.Diagnostic;
        var rootDiagnostic =
            diagnostic?.RequirementEvaluations
                .FirstOrDefault(
                    static item =>
                        item.ParentEvaluationId is null);

        var outcome =
            rootDiagnostic is null
                ? decision.IsAllowed
                    ? "allow"
                    : "deny"
                : FormatOutcome(
                    rootDiagnostic.Outcome);

        var report =
            new PolicyExplanationReport(
                IsValid: true,
                Fixture: suite.FixturePath,
                Manifest: suite.ManifestPath,
                TestId: test.Id,
                Outcome: outcome,
                PolicyId: diagnostic?.PolicyId,
                FailureCodes:
                    decision.Failures
                        .Select(
                            static item => item.Code)
                        .Distinct(
                            StringComparer.Ordinal)
                        .Order(
                            StringComparer.Ordinal)
                        .ToArray(),
                SensitiveValuesRedacted: true,
                Errors:
                    Array.Empty<PolicyTestDiagnostic>(),
                Requirements:
                    BuildRequirementTree(
                        diagnostic?.RequirementEvaluations ??
                        Array.Empty<RequirementEvaluationDiagnostic>()));

        WriteReport(
            report,
            outputFormat,
            output,
            error);

        return RuleGateExitCodes.Success;
    }

    private static int WriteFailure(
        string fixture,
        string? manifest,
        string testId,
        IReadOnlyList<PolicyTestDiagnostic> errors,
        ValidationOutputFormat outputFormat,
        TextWriter output,
        TextWriter error)
    {
        WriteReport(
            new PolicyExplanationReport(
                IsValid: false,
                Fixture: fixture,
                Manifest: manifest,
                TestId: testId,
                Outcome: null,
                PolicyId: null,
                FailureCodes:
                    Array.Empty<string>(),
                SensitiveValuesRedacted: true,
                Errors: errors,
                Requirements:
                    Array.Empty<
                        PolicyExplanationRequirement>()),
            outputFormat,
            output,
            error);

        return RuleGateExitCodes.ExplanationFailed;
    }

    private static IReadOnlyList<
        PolicyExplanationRequirement>
        BuildRequirementTree(
            IReadOnlyList<
                RequirementEvaluationDiagnostic> diagnostics)
    {
        var childrenByParent =
            diagnostics
                .Where(
                    static item =>
                        item.ParentEvaluationId is not null)
                .GroupBy(
                    static item =>
                        item.ParentEvaluationId!.Value)
                .ToDictionary(
                    static group => group.Key,
                    static group => group.ToArray());

        return diagnostics
            .Where(
                static item =>
                    item.ParentEvaluationId is null)
            .Select(
                item => CreateRequirement(
                    item,
                    childrenByParent))
            .ToArray();
    }

    private static PolicyExplanationRequirement
        CreateRequirement(
            RequirementEvaluationDiagnostic diagnostic,
            IReadOnlyDictionary<
                Guid,
                RequirementEvaluationDiagnostic[]>
                childrenByParent)
    {
        childrenByParent.TryGetValue(
            diagnostic.EvaluationId,
            out var children);

        return new PolicyExplanationRequirement(
            Kind:
                ToCamelCase(
                    diagnostic.RequirementKind.ToString()),
            Outcome:
                FormatOutcome(
                    diagnostic.Outcome),
            RequirementId: diagnostic.RequirementId,
            FailureCodes:
                diagnostic.FailureCodes
                    .Distinct(
                        StringComparer.Ordinal)
                    .Order(
                        StringComparer.Ordinal)
                    .ToArray(),
            AttributeSource:
                diagnostic.AttributeSource is null
                    ? null
                    : ToCamelCase(
                        diagnostic.AttributeSource
                            .Value.ToString()),
            AttributeName: diagnostic.AttributeName,
            ComparedAttributeSource:
                diagnostic.ComparedAttributeSource is null
                    ? null
                    : ToCamelCase(
                        diagnostic.ComparedAttributeSource
                            .Value.ToString()),
            ComparedAttributeName:
                diagnostic.ComparedAttributeName,
            Children:
                (children ?? [])
                    .Select(
                        item => CreateRequirement(
                            item,
                            childrenByParent))
                    .ToArray());
    }

    private static string FormatOutcome(
        RequirementEvaluationOutcome outcome)
    {
        return outcome switch
        {
            RequirementEvaluationOutcome.Satisfied =>
                "allow",

            RequirementEvaluationOutcome.NotSatisfied =>
                "deny",

            RequirementEvaluationOutcome.Indeterminate =>
                "indeterminate",

            _ => throw new ArgumentOutOfRangeException(
                nameof(outcome),
                outcome,
                "Unsupported requirement evaluation outcome.")
        };
    }

    private static string ToCamelCase(
        string value)
    {
        return string.IsNullOrEmpty(value)
            ? value
            : char.ToLowerInvariant(value[0]) +
              value[1..];
    }

    private static IReadOnlyList<PolicyTestDiagnostic>
        CreateManifestDiagnostics(
            ManifestCompilationResult compilation)
    {
        return compilation.LoadErrors
            .Select(
                static item =>
                    new PolicyTestDiagnostic(
                        Category: "manifest",
                        Code: item.Code,
                        Message: item.Message,
                        Line: item.Line,
                        Column: item.Column))
            .Concat(
                compilation.ValidationErrors.Select(
                    static item =>
                        new PolicyTestDiagnostic(
                            Category: "manifest",
                            Code: item.Code,
                            Message: item.Message,
                            Path: item.Path)))
            .ToArray();
    }

    private static void WriteReport(
        PolicyExplanationReport report,
        ValidationOutputFormat outputFormat,
        TextWriter output,
        TextWriter error)
    {
        IPolicyExplanationReporter reporter =
            outputFormat switch
            {
                ValidationOutputFormat.Text =>
                    new TextPolicyExplanationReporter(),

                ValidationOutputFormat.Json =>
                    new JsonPolicyExplanationReporter(),

                _ => throw new ArgumentOutOfRangeException(
                    nameof(outputFormat),
                    outputFormat,
                    "Unsupported explanation output format.")
            };

        reporter.Write(
            report,
            output,
            error);
    }
}
