namespace Fotbiler.RuleGate.Abstractions.Policies;

public sealed record PolicySnapshotInfo
{
    public PolicySnapshotInfo(
        long version,
        int policyCount,
        IEnumerable<string> sourceNames)
    {
        if (version < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(version));
        }

        if (policyCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(policyCount));
        }

        ArgumentNullException.ThrowIfNull(sourceNames);

        var names = sourceNames.ToArray();

        if (names.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException(
                "Policy source names cannot contain empty values.",
                nameof(sourceNames));
        }

        Version = version;
        PolicyCount = policyCount;
        SourceNames = Array.AsReadOnly(names);
    }

    public long Version { get; }

    public int PolicyCount { get; }

    public IReadOnlyList<string> SourceNames { get; }
}
