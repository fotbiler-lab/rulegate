using Fotbiler.RuleGate.Abstractions.Authorization;

namespace Fotbiler.RuleGate.AspNetCore.Authorization;

public interface IRuleGateAuthorizationResourceFactory
{
    AuthorizationResource Create(
        object? resource);

    AuthorizationResource Create(
        object? resource,
        RuleGateAuthorizationRequirement requirement)
    {
        ArgumentNullException.ThrowIfNull(requirement);

        return Create(resource);
    }
}
