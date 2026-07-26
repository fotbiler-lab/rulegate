using Microsoft.AspNetCore.Authorization;

namespace Fotbiler.RuleGate.AspNetCore.Authorization;

public sealed class RuleGateAuthorizationRequirement
    : IAuthorizationRequirement
{
    public RuleGateAuthorizationRequirement(
        string action)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            action);

        Action = action;
    }

    public RuleGateAuthorizationRequirement(
        string resourceType,
        string action)
    {
        var policyName =
            new RuleGatePolicyName(
                resourceType,
                action);

        ResourceType =
            policyName.ResourceType;

        Action =
            policyName.Action;
    }

    public string? ResourceType { get; }

    public string Action { get; }
}
