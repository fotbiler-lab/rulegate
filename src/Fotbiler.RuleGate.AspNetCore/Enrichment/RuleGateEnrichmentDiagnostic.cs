using Fotbiler.RuleGate.Abstractions.Policies;

namespace Fotbiler.RuleGate.AspNetCore.Enrichment;

public sealed class RuleGateEnrichmentDiagnostic
{
    public RuleGateEnrichmentDiagnostic(
        AuthorizationAttributeSource attributeSource,
        string providerName,
        int order,
        RuleGateAttributeCollisionBehavior
            collisionBehavior,
        RuleGateEnrichmentOutcome outcome,
        int attributeCount,
        TimeSpan duration)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            providerName);

        ArgumentOutOfRangeException.ThrowIfNegative(
            attributeCount);

        AttributeSource = attributeSource;
        ProviderName = providerName;
        Order = order;
        CollisionBehavior = collisionBehavior;
        Outcome = outcome;
        AttributeCount = attributeCount;
        Duration = duration;
    }

    public AuthorizationAttributeSource
        AttributeSource
    { get; }

    public string ProviderName { get; }

    public int Order { get; }

    public RuleGateAttributeCollisionBehavior
        CollisionBehavior
    { get; }

    public RuleGateEnrichmentOutcome Outcome { get; }

    public int AttributeCount { get; }

    public TimeSpan Duration { get; }
}
