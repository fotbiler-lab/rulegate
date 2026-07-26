namespace Fotbiler.RuleGate.Abstractions.Authorization;

public interface IAuthorizationEngine
{
    ValueTask<AuthorizationDecision> EvaluateAsync(
        AuthorizationRequest request,
        CancellationToken cancellationToken = default);
}
