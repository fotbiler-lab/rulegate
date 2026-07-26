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

    public string Action { get; }
}
