namespace Fotbiler.RuleGate.Abstractions.Policies;

public sealed class PolicyReloadResult
{
    private PolicyReloadResult(
        bool isActivated,
        PolicySnapshotInfo activeSnapshot,
        IReadOnlyList<PolicyReloadDiagnostic> diagnostics)
    {
        IsActivated = isActivated;
        ActiveSnapshot = activeSnapshot;
        Diagnostics = diagnostics;
    }

    public bool IsSuccess => Diagnostics.Count == 0;

    public bool IsActivated { get; }

    public PolicySnapshotInfo ActiveSnapshot { get; }

    public IReadOnlyList<PolicyReloadDiagnostic> Diagnostics { get; }

    public static PolicyReloadResult Activated(
        PolicySnapshotInfo activeSnapshot)
    {
        ArgumentNullException.ThrowIfNull(activeSnapshot);

        return new PolicyReloadResult(
            isActivated: true,
            activeSnapshot,
            Array.Empty<PolicyReloadDiagnostic>());
    }

    public static PolicyReloadResult Rejected(
        PolicySnapshotInfo activeSnapshot,
        IEnumerable<PolicyReloadDiagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(activeSnapshot);
        ArgumentNullException.ThrowIfNull(diagnostics);

        var items = diagnostics.ToArray();

        if (items.Length == 0)
        {
            throw new ArgumentException(
                "A rejected policy reload must contain at least one diagnostic.",
                nameof(diagnostics));
        }

        if (items.Any(static diagnostic => diagnostic is null))
        {
            throw new ArgumentException(
                "Policy reload diagnostics cannot contain null values.",
                nameof(diagnostics));
        }

        var ordered = items
            .OrderBy(static item => item.SourceName, StringComparer.Ordinal)
            .ThenBy(static item => item.Path, StringComparer.Ordinal)
            .ThenBy(static item => item.Code, StringComparer.Ordinal)
            .ThenBy(static item => item.Message, StringComparer.Ordinal)
            .ToArray();

        return new PolicyReloadResult(
            isActivated: false,
            activeSnapshot,
            Array.AsReadOnly(ordered));
    }
}
