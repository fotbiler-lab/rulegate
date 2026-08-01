using Fotbiler.RuleGate.Cli.ExitCodes;
using Fotbiler.RuleGate.Cli.Output;
using Fotbiler.RuleGate.Manifest.Configuration;
using Fotbiler.RuleGate.Manifest.Loading;
using Fotbiler.RuleGate.Manifest.Validation;

namespace Fotbiler.RuleGate.Cli.Linting;

internal sealed class ManifestLintRunner
{
    private readonly RuleGateManifestYamlLoader _loader;
    private readonly RuleGateManifestValidator _validator;
    private readonly ManifestLinter _linter;

    public ManifestLintRunner(
        RuleGateManifestYamlLoader loader,
        RuleGateManifestValidator validator,
        ManifestLinter linter)
    {
        ArgumentNullException.ThrowIfNull(loader);
        ArgumentNullException.ThrowIfNull(validator);
        ArgumentNullException.ThrowIfNull(linter);

        _loader = loader;
        _validator = validator;
        _linter = linter;
    }

    public async Task<int> RunAsync(
        string? path,
        ValidationOutputFormat outputFormat,
        TextWriter output,
        TextWriter error,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);

        var requestedPath =
            string.IsNullOrWhiteSpace(path)
                ? RuleGateManifestDefaults.FileName
                : path;

        var fullPath =
            Path.GetFullPath(requestedPath);

        var loadResult =
            await _loader.LoadFromFileAsync(
                fullPath,
                cancellationToken);

        ManifestLintReport report;

        if (!loadResult.IsSuccess)
        {
            report =
                new ManifestLintReport(
                    IsValid: false,
                    IsClean: false,
                    Manifest: fullPath,
                    PolicyCount: 0,
                    Errors:
                        loadResult.Errors
                            .Select(
                                static item =>
                                    new ManifestLintDiagnostic(
                                        Category: "load",
                                        Code: item.Code,
                                        Message: item.Message,
                                        Line: item.Line,
                                        Column: item.Column))
                            .ToArray(),
                    Findings:
                        Array.Empty<ManifestLintFinding>());
        }
        else
        {
            var validation =
                _validator.Validate(
                    loadResult.Manifest);

            if (!validation.IsValid)
            {
                report =
                    new ManifestLintReport(
                        IsValid: false,
                        IsClean: false,
                        Manifest: fullPath,
                        PolicyCount: 0,
                        Errors:
                            validation.Errors
                                .Select(
                                    static item =>
                                        new ManifestLintDiagnostic(
                                            Category: "validation",
                                            Code: item.Code,
                                            Message: item.Message,
                                            Path: item.Path))
                                .ToArray(),
                        Findings:
                            Array.Empty<ManifestLintFinding>());
            }
            else
            {
                var findings =
                    _linter.Analyze(
                        loadResult.Manifest);

                report =
                    new ManifestLintReport(
                        IsValid: true,
                        IsClean: findings.Count == 0,
                        Manifest: fullPath,
                        PolicyCount:
                            loadResult.Manifest.Policies!.Count,
                        Errors:
                            Array.Empty<
                                ManifestLintDiagnostic>(),
                        Findings: findings);
            }
        }

        WriteReport(
            report,
            outputFormat,
            output,
            error);

        return report.IsValid && report.IsClean
            ? RuleGateExitCodes.Success
            : RuleGateExitCodes.LintFailed;
    }

    private static void WriteReport(
        ManifestLintReport report,
        ValidationOutputFormat outputFormat,
        TextWriter output,
        TextWriter error)
    {
        IManifestLintReporter reporter =
            outputFormat switch
            {
                ValidationOutputFormat.Text =>
                    new TextManifestLintReporter(),

                ValidationOutputFormat.Json =>
                    new JsonManifestLintReporter(),

                _ => throw new ArgumentOutOfRangeException(
                    nameof(outputFormat),
                    outputFormat,
                    "Unsupported lint output format.")
            };

        reporter.Write(
            report,
            output,
            error);
    }
}
