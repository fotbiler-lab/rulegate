using Fotbiler.RuleGate.Abstractions.Authorization;

namespace Fotbiler.RuleGate.AspNetCore.Authorization;

public sealed class RuleGateAuthorizationResourceFactory
    : IRuleGateAuthorizationResourceFactory
{
    public AuthorizationResource Create(
        object? resource)
    {
        if (resource is AuthorizationResource
            authorizationResource)
        {
            return authorizationResource;
        }

        throw new InvalidOperationException(
            "The ASP.NET Core authorization resource must be a RuleGate AuthorizationResource.");
    }
}
