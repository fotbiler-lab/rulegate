using Fotbiler.RuleGate.Abstractions.Policies;

namespace Fotbiler.RuleGate.Core.Policies;

public sealed class InMemoryPolicySource : IPolicySource
{
    private readonly IReadOnlyList<PolicyDefinition> _policies;

    public InMemoryPolicySource(
        IEnumerable<PolicyDefinition> policies,
        string name = "in-memory")
    {
        ArgumentNullException.ThrowIfNull(policies);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var items = policies.ToArray();

        if (items.Any(static policy => policy is null))
        {
            throw new ArgumentException(
                "In-memory policies cannot contain null values.",
                nameof(policies));
        }

        Name = name;
        _policies = Array.AsReadOnly(items);
    }

    public string Name { get; }

    public ValueTask<PolicySourceLoadResult> LoadAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return ValueTask.FromResult(
            PolicySourceLoadResult.Success(_policies));
    }
}
