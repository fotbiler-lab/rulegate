using Fotbiler.RuleGate.Manifest.Compilation;

namespace Fotbiler.RuleGate.Cli.Validation;

internal sealed record ManifestValidationDiagnostic(
    string Category,
    string Code,
    string Message,
    string? Path,
    long? Line,
    long? Column);

internal sealed record ManifestValidationReport(
    bool IsValid,
    string File,
    int PolicyCount,
    IReadOnlyList<ManifestValidationDiagnostic> Errors)
{
    public static ManifestValidationReport Create(
        string file,
        ManifestCompilationResult compilation)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(file);
        ArgumentNullException.ThrowIfNull(compilation);

        var loadDiagnostics =
            compilation.LoadErrors.Select(
                static error =>
                    new ManifestValidationDiagnostic(
                        Category: "load",
                        Code: error.Code,
                        Message: error.Message,
                        Path: null,
                        Line: error.Line,
                        Column: error.Column));

        var validationDiagnostics =
            compilation.ValidationErrors.Select(
                static error =>
                    new ManifestValidationDiagnostic(
                        Category: "validation",
                        Code: error.Code,
                        Message: error.Message,
                        Path: error.Path,
                        Line: null,
                        Column: null));

        var diagnostics =
            loadDiagnostics
                .Concat(validationDiagnostics)
                .ToArray();

        return new ManifestValidationReport(
            IsValid: compilation.IsSuccess,
            File: file,
            PolicyCount:
                compilation.IsSuccess
                    ? compilation.Policies.Count
                    : 0,
            Errors: diagnostics);
    }
}
