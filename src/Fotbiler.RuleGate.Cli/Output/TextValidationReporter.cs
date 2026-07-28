using Fotbiler.RuleGate.Cli.Validation;

namespace Fotbiler.RuleGate.Cli.Output;

internal sealed class TextValidationReporter :
    IValidationReporter
{
    public void Write(
        ManifestValidationReport report,
        TextWriter output,
        TextWriter error)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);

        if (report.IsValid)
        {
            output.WriteLine(
                "RuleGate manifest is valid.");
            output.WriteLine();
            output.WriteLine(
                $"File: {report.File}");
            output.WriteLine(
                $"Policies: {report.PolicyCount}");

            return;
        }

        error.WriteLine(
            "RuleGate manifest is invalid.");
        error.WriteLine();
        error.WriteLine(
            $"File: {report.File}");

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

            if (diagnostic.Line is not null)
            {
                error.WriteLine(
                    $"  Line: {diagnostic.Line}");
            }

            if (diagnostic.Column is not null)
            {
                error.WriteLine(
                    $"  Column: {diagnostic.Column}");
            }

            error.WriteLine(
                $"  {diagnostic.Message}");
        }

        error.WriteLine();
        error.WriteLine(
            $"{report.Errors.Count} error(s) found.");
    }
}
