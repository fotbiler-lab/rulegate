using Fotbiler.RuleGate.Cli.Testing;

namespace Fotbiler.RuleGate.Cli.Output;

internal sealed class TextPolicyTestReporter :
    IPolicyTestReporter
{
    public void Write(
        PolicyTestReport report,
        TextWriter output,
        TextWriter error)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);

        if (!report.IsValid)
        {
            error.WriteLine(
                "RuleGate policy tests could not run.");
            error.WriteLine();
            error.WriteLine(
                $"Fixture: {report.Fixture}");

            if (report.Manifest is not null)
            {
                error.WriteLine(
                    $"Manifest: {report.Manifest}");
            }

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
            return;
        }

        foreach (var result in report.Tests)
        {
            output.WriteLine(
                $"{(result.Passed ? "PASS" : "FAIL")} " +
                $"{result.Id}");

            if (result.Passed)
            {
                output.WriteLine(
                    $"  Outcome: {result.ActualOutcome}");
                continue;
            }

            output.WriteLine(
                $"  Expected: {FormatResult(
                    result.ExpectedOutcome,
                    result.ExpectedFailureCodes)}");

            output.WriteLine(
                $"  Actual:   {FormatResult(
                    result.ActualOutcome,
                    result.ActualFailureCodes)}");
        }

        output.WriteLine();
        output.WriteLine(
            $"Summary: {report.PassedTestCount} passed, " +
            $"{report.FailedTestCount} failed, " +
            $"{report.SelectedTestCount} selected " +
            $"of {report.TotalTestCount} total.");
    }

    private static string FormatResult(
        string outcome,
        IReadOnlyList<string>? failureCodes)
    {
        return failureCodes is null ||
               failureCodes.Count == 0
            ? outcome
            : $"{outcome} [{string.Join(", ", failureCodes)}]";
    }
}
