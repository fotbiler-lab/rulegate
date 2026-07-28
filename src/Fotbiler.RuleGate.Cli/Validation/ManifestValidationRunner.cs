using Fotbiler.RuleGate.Cli.ExitCodes;
using Fotbiler.RuleGate.Cli.Output;
using Fotbiler.RuleGate.Manifest.Compilation;
using Fotbiler.RuleGate.Manifest.Configuration;

namespace Fotbiler.RuleGate.Cli.Validation;

internal sealed class ManifestValidationRunner
{
    private readonly RuleGateManifestCompiler _compiler;

    public ManifestValidationRunner(
        RuleGateManifestCompiler compiler)
    {
        ArgumentNullException.ThrowIfNull(compiler);

        _compiler = compiler;
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

        var compilation =
            await _compiler.CompileFromFileAsync(
                fullPath,
                cancellationToken);

        var report =
            ManifestValidationReport.Create(
                fullPath,
                compilation);

        IValidationReporter reporter =
            outputFormat switch
            {
                ValidationOutputFormat.Text =>
                    new TextValidationReporter(),

                ValidationOutputFormat.Json =>
                    new JsonValidationReporter(),

                _ =>
                    throw new ArgumentOutOfRangeException(
                        nameof(outputFormat),
                        outputFormat,
                        "Unsupported validation output format.")
            };

        reporter.Write(
            report,
            output,
            error);

        return report.IsValid
            ? RuleGateExitCodes.Success
            : RuleGateExitCodes.ManifestInvalid;
    }
}
