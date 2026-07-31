namespace Fotbiler.RuleGate.Abstractions.Policies;

public sealed class PolicySourceLoadResult
{
    private PolicySourceLoadResult(
        IReadOnlyList<PolicyDefinition> policies,
        IReadOnlyList<PolicySourceDiagnostic> diagnostics)
    {
        Policies = policies;
        Diagnostics = diagnostics;
    }

    public bool IsSuccess => Diagnostics.Count == 0;

    public IReadOnlyList<PolicyDefinition> Policies { get; }

    public IReadOnlyList<PolicySourceDiagnostic> Diagnostics { get; }

    public static PolicySourceLoadResult Success(
        IEnumerable<PolicyDefinition> policies)
    {
        ArgumentNullException.ThrowIfNull(policies);

        var items = policies.ToArray();

        if (items.Any(static policy => policy is null))
        {
            throw new ArgumentException(
                "Loaded policies cannot contain null values.",
                nameof(policies));
        }

        return new PolicySourceLoadResult(
            Array.AsReadOnly(items),
            Array.Empty<PolicySourceDiagnostic>());
    }

    public static PolicySourceLoadResult Failure(
        IEnumerable<PolicySourceDiagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);

        var items = diagnostics.ToArray();

        if (items.Length == 0)
        {
            throw new ArgumentException(
                "A policy source failure must contain at least one diagnostic.",
                nameof(diagnostics));
        }

        if (items.Any(static diagnostic => diagnostic is null))
        {
            throw new ArgumentException(
                "Policy source diagnostics cannot contain null values.",
                nameof(diagnostics));
        }

        var ordered = items
            .OrderBy(static item => item.Path, StringComparer.Ordinal)
            .ThenBy(static item => item.Code, StringComparer.Ordinal)
            .ThenBy(static item => item.Message, StringComparer.Ordinal)
            .ToArray();

        return new PolicySourceLoadResult(
            Array.Empty<PolicyDefinition>(),
            Array.AsReadOnly(ordered));
    }
}
