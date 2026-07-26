namespace Fotbiler.RuleGate.Abstractions.Policies;

public interface IPolicyProvider
{
    ValueTask<PolicyDefinition?> FindAsync(
        string resourceType,
        string action,
        CancellationToken cancellationToken = default);
}
