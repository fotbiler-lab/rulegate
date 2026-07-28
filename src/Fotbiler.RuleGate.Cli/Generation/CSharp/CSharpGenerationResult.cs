namespace Fotbiler.RuleGate.Cli.Generation.CSharp;

internal sealed record CSharpGenerationResult(
    string? Source,
    IReadOnlyList<CSharpGenerationDiagnostic> Diagnostics)
{
    public bool IsSuccess =>
        Source is not null
        && Diagnostics.Count == 0;

    public static CSharpGenerationResult Success(
        string source)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            source);

        return new CSharpGenerationResult(
            source,
            Array.Empty<CSharpGenerationDiagnostic>());
    }

    public static CSharpGenerationResult Failure(
        IEnumerable<CSharpGenerationDiagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(
            diagnostics);

        CSharpGenerationDiagnostic[] materializedDiagnostics =
        [
            .. diagnostics
                .OrderBy(
                    static diagnostic => diagnostic.Code,
                    StringComparer.Ordinal)
                .ThenBy(
                    static diagnostic => diagnostic.Message,
                    StringComparer.Ordinal)
        ];

        if (materializedDiagnostics.Length == 0)
        {
            throw new ArgumentException(
                "At least one diagnostic is required.",
                nameof(diagnostics));
        }

        return new CSharpGenerationResult(
            null,
            materializedDiagnostics);
    }
}
