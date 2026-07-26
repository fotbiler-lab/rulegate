using Fotbiler.RuleGate.Abstractions.Authorization;

namespace Fotbiler.RuleGate.AspNetCore.Authorization;

public interface IRuleGateAuthorizationResourceFactory
{
    AuthorizationResource Create(
        object? resource);
}
