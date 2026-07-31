using Fotbiler.RuleGate.Cli.Explanation;

namespace Fotbiler.RuleGate.Cli.Output;

internal sealed class TextPolicyExplanationReporter :
    IPolicyExplanationReporter
{
    public void Write(
        PolicyExplanationReport report,
        TextWriter output,
        TextWriter error)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);

        if (!report.IsValid)
        {
            error.WriteLine(
                "RuleGate policy explanation could not run.");

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

        output.WriteLine(
            $"Decision: {report.Outcome}");
        output.WriteLine(
            $"Policy: {report.PolicyId ?? "<no matching policy>"}");

        if (report.FailureCodes.Count != 0)
        {
            output.WriteLine(
                "Failure codes: " +
                string.Join(
                    ", ",
                    report.FailureCodes));
        }

        output.WriteLine(
            "Sensitive request values: redacted");

        if (report.Requirements.Count == 0)
        {
            return;
        }

        output.WriteLine();
        output.WriteLine("Requirements:");

        foreach (var requirement in report.Requirements)
        {
            WriteRequirement(
                requirement,
                output,
                depth: 0);
        }
    }

    private static void WriteRequirement(
        PolicyExplanationRequirement requirement,
        TextWriter output,
        int depth)
    {
        var indentation =
            new string(
                ' ',
                (depth + 1) * 2);

        var identifier =
            requirement.RequirementId is null
                ? string.Empty
                : $" [{requirement.RequirementId}]";

        var attribute =
            FormatAttribute(requirement);

        output.WriteLine(
            $"{indentation}- {requirement.Kind}" +
            identifier +
            attribute +
            $": {requirement.Outcome}");

        if (requirement.FailureCodes.Count != 0)
        {
            output.WriteLine(
                $"{indentation}  Failure codes: " +
                string.Join(
                    ", ",
                    requirement.FailureCodes));
        }

        foreach (var child in requirement.Children)
        {
            WriteRequirement(
                child,
                output,
                depth + 1);
        }
    }

    private static string FormatAttribute(
        PolicyExplanationRequirement requirement)
    {
        if (requirement.AttributeSource is null ||
            requirement.AttributeName is null)
        {
            return string.Empty;
        }

        var result =
            $" ({requirement.AttributeSource}." +
            $"{requirement.AttributeName}";

        if (requirement.ComparedAttributeSource is not null &&
            requirement.ComparedAttributeName is not null)
        {
            result +=
                " ↔ " +
                $"{requirement.ComparedAttributeSource}." +
                requirement.ComparedAttributeName;
        }

        return result + ")";
    }
}
