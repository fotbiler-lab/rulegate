using Fotbiler.RuleGate.Abstractions.Attributes;

namespace Fotbiler.RuleGate.AspNetCore.Enrichment;

public sealed class RuleGateAttributeProviderResult
{
    private RuleGateAttributeProviderResult(
        RuleGateAttributeProviderResultStatus status,
        AuthorizationAttributes attributes)
    {
        Status = status;
        Attributes = attributes;
    }

    public RuleGateAttributeProviderResultStatus Status { get; }

    public AuthorizationAttributes Attributes { get; }

    public bool IsSuccessful =>
        Status ==
        RuleGateAttributeProviderResultStatus.Succeeded;

    public static RuleGateAttributeProviderResult Success(
        AuthorizationAttributes? attributes = null)
    {
        return new RuleGateAttributeProviderResult(
            RuleGateAttributeProviderResultStatus.Succeeded,
            attributes ?? AuthorizationAttributes.Empty);
    }

    public static RuleGateAttributeProviderResult
        MissingRequiredData()
    {
        return new RuleGateAttributeProviderResult(
            RuleGateAttributeProviderResultStatus
                .MissingRequiredData,
            AuthorizationAttributes.Empty);
    }

    public static RuleGateAttributeProviderResult Fail()
    {
        return new RuleGateAttributeProviderResult(
            RuleGateAttributeProviderResultStatus.Failed,
            AuthorizationAttributes.Empty);
    }
}
