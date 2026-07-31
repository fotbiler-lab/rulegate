using Fotbiler.RuleGate.Abstractions.Policies;
using Fotbiler.RuleGate.Manifest.Compilation;

namespace Fotbiler.RuleGate.Manifest.PolicySources;

public static class ManifestCompilationPolicySourceExtensions
{
    public static PolicySourceLoadResult ToPolicySourceLoadResult(
        this ManifestCompilationResult compilation)
    {
        ArgumentNullException.ThrowIfNull(compilation);

        if (compilation.IsSuccess)
        {
            return PolicySourceLoadResult.Success(
                compilation.Policies);
        }

        var diagnostics = compilation.LoadErrors
            .Select(
                static error =>
                    new PolicySourceDiagnostic(
                        error.Code,
                        error.Message,
                        CreateLoadPath(
                            error.Line,
                            error.Column)))
            .Concat(
                compilation.ValidationErrors.Select(
                    static error =>
                        new PolicySourceDiagnostic(
                            error.Code,
                            error.Message,
                            error.Path)))
            .ToArray();

        return PolicySourceLoadResult.Failure(
            diagnostics);
    }

    private static string? CreateLoadPath(
        long? line,
        long? column)
    {
        if (line is null)
        {
            return null;
        }

        return column is null
            ? $"line:{line.Value}"
            : $"line:{line.Value},column:{column.Value}";
    }
}
