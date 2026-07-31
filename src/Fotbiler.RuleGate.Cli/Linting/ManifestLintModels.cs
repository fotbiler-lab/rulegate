namespace Fotbiler.RuleGate.Cli.Linting;

internal static class ManifestLintCodes
{
    public const string DuplicateRequirement =
        "RGLINT001";

    public const string ContradictoryRequirement =
        "RGLINT002";

    public const string UnreachableRequirement =
        "RGLINT003";

    public const string ExcessiveDepth =
        "RGLINT004";

    public const string UnnecessaryComplexity =
        "RGLINT005";

    public const string DuplicateRequirementId =
        "RGLINT006";

    public const string IdentifierCollision =
        "RGLINT007";

    public const string RiskyNegativeOperator =
        "RGLINT008";

    public const string ExcessiveComplexity =
        "RGLINT009";
}

internal sealed record ManifestLintFinding(
    string Code,
    string Severity,
    string Path,
    string Message);

internal sealed record ManifestLintDiagnostic(
    string Category,
    string Code,
    string Message,
    string? Path = null,
    long? Line = null,
    long? Column = null);

internal sealed record ManifestLintReport(
    bool IsValid,
    bool IsClean,
    string Manifest,
    int PolicyCount,
    IReadOnlyList<ManifestLintDiagnostic> Errors,
    IReadOnlyList<ManifestLintFinding> Findings);
