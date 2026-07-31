namespace Fotbiler.RuleGate.Abstractions.Policies;

public interface IPolicySource
{
    string Name { get; }

    ValueTask<PolicySourceLoadResult> LoadAsync(
        CancellationToken cancellationToken = default);
}
