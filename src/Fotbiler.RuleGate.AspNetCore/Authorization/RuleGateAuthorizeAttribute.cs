using Microsoft.AspNetCore.Authorization;

namespace Fotbiler.RuleGate.AspNetCore.Authorization;

[AttributeUsage(
    AttributeTargets.Class |
    AttributeTargets.Method,
    AllowMultiple = true,
    Inherited = true)]
public sealed class RuleGateAuthorizeAttribute
    : AuthorizeAttribute,
      IRuleGateAuthorizationMetadata
{
    public RuleGateAuthorizeAttribute(
        string resourceType,
        string action,
        string? resourceIdRouteValue = null)
    {
        var metadata =
            new RuleGateAuthorizationMetadata(
                resourceType,
                action,
                resourceIdRouteValue);

        ResourceType =
            metadata.ResourceType;

        Action =
            metadata.Action;

        ResourceIdRouteValue =
            metadata.ResourceIdRouteValue;

        Policy =
            metadata.PolicyName;
    }

    public string ResourceType { get; }

    public string Action { get; }

    public string? ResourceIdRouteValue { get; }
}
