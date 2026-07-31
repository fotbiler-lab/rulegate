using Fotbiler.RuleGate.Cli.Linting;

namespace Fotbiler.RuleGate.Cli.Output;

internal sealed class TextManifestLintReporter :
    IManifestLintReporter
{
    public void Write(
        ManifestLintReport report,
        TextWriter output,
        TextWriter error)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);

        if (!report.IsValid)
        {
            error.WriteLine(
                "RuleGate manifest could not be linted.");

            foreach (var diagnostic in report.Errors)
            {
                error.WriteLine();
                error.WriteLine(
                    $"{diagnostic.Category.ToUpperInvariant()} " +
                    diagnostic.Code);

                if (diagnostic.Path is not null)
                {
                    error.WriteLine(
                        $"  Path: {diagnostic.Path}");
                }

                error.WriteLine(
                    $"  {diagnostic.Message}");
            }

            return;
        }

        if (report.IsClean)
        {
            output.WriteLine(
                "RuleGate manifest has no lint findings.");
            output.WriteLine(
                $"Policies: {report.PolicyCount}");
            return;
        }

        foreach (var finding in report.Findings)
        {
            output.WriteLine(
                $"{finding.Severity.ToUpperInvariant()} " +
                finding.Code);
            output.WriteLine(
                $"  Path: {finding.Path}");
            output.WriteLine(
                $"  {finding.Message}");
            output.WriteLine();
        }

        output.WriteLine(
            $"{report.Findings.Count} lint finding(s).");
    }
}
