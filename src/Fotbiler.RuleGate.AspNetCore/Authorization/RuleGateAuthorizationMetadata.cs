namespace Fotbiler.RuleGate.AspNetCore.Authorization;

public sealed class RuleGateAuthorizationMetadata
    : IRuleGateAuthorizationMetadata
{
    public RuleGateAuthorizationMetadata(
        string resourceType,
        string action,
        string? resourceIdRouteValue = null)
    {
        var policyName =
            new RuleGatePolicyName(
                resourceType,
                action);

        if (resourceIdRouteValue is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(
                resourceIdRouteValue);
        }

        ResourceType =
            policyName.ResourceType;

        Action =
            policyName.Action;

        ResourceIdRouteValue =
            resourceIdRouteValue;

        PolicyName =
            policyName.ToString();
    }

    public string ResourceType { get; }

    public string Action { get; }

    public string? ResourceIdRouteValue { get; }

    public string PolicyName { get; }
}
