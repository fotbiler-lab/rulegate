namespace Fotbiler.RuleGate.AspNetCore.Authorization;

public interface IRuleGateAuthorizationMetadata
{
    string ResourceType { get; }

    string Action { get; }

    string? ResourceIdRouteValue { get; }
}
