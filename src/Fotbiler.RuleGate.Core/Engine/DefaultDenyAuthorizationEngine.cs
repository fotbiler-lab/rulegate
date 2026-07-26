using Fotbiler.RuleGate.Abstractions.Authorization;
using Fotbiler.RuleGate.Abstractions.Constants;

namespace Fotbiler.RuleGate.Core.Engine;

public sealed class DefaultDenyAuthorizationEngine
    : IAuthorizationEngine
{
    public ValueTask<AuthorizationDecision> EvaluateAsync(
        AuthorizationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        cancellationToken.ThrowIfCancellationRequested();

        var decision = AuthorizationDecision.Deny(
            new AuthorizationFailure(
                AuthorizationFailureCodes.NoMatchingPolicy));

        return ValueTask.FromResult(decision);
    }
}
