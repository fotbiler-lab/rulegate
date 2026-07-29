namespace Fotbiler.RuleGate.AspNetCore.Enrichment;

public interface IRuleGateResourceAttributeProvider
{
    int Order => 0;

    RuleGateAttributeCollisionBehavior
        CollisionBehavior =>
            RuleGateAttributeCollisionBehavior.Fail;

    ValueTask<RuleGateAttributeProviderResult>
        ProvideAttributesAsync(
            RuleGateAttributeProviderContext context,
            CancellationToken cancellationToken = default);
}
