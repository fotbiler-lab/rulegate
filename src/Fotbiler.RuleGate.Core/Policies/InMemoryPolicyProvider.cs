using System.Collections.Frozen;
using Fotbiler.RuleGate.Abstractions.Policies;

namespace Fotbiler.RuleGate.Core.Policies;

public sealed class InMemoryPolicyProvider
    : IPolicyProvider
{
    private readonly FrozenDictionary<
        PolicyRoute,
        PolicyDefinition> _policies;

    public InMemoryPolicyProvider(
        IEnumerable<PolicyDefinition> policies)
    {
        ArgumentNullException.ThrowIfNull(policies);

        var policiesByRoute =
            new Dictionary<PolicyRoute, PolicyDefinition>();

        var policyIds =
            new HashSet<string>(StringComparer.Ordinal);

        foreach (var policy in policies)
        {
            ArgumentNullException.ThrowIfNull(policy);

            if (!policyIds.Add(policy.Id))
            {
                throw new InvalidOperationException(
                    $"Multiple policies are registered with identifier '{policy.Id}'.");
            }

            var route = new PolicyRoute(
                policy.ResourceType,
                policy.Action);

            if (!policiesByRoute.TryAdd(
                    route,
                    policy))
            {
                throw new InvalidOperationException(
                    $"Multiple policies are registered for resource type '{policy.ResourceType}' and action '{policy.Action}'.");
            }
        }

        _policies =
            policiesByRoute.ToFrozenDictionary();
    }

    public ValueTask<PolicyDefinition?> FindAsync(
        string resourceType,
        string action,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            resourceType);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            action);

        cancellationToken.ThrowIfCancellationRequested();

        PolicyDefinition? result =
            _policies.TryGetValue(
                new PolicyRoute(resourceType, action),
                out var policy)
                ? policy
                : null;

        return ValueTask.FromResult(result);
    }

    private readonly record struct PolicyRoute(
        string ResourceType,
        string Action);
}
