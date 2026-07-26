using Fotbiler.RuleGate.Abstractions.Authorization;
using Fotbiler.RuleGate.Abstractions.Constants;
using Fotbiler.RuleGate.Abstractions.Evaluation;
using Fotbiler.RuleGate.Abstractions.Policies;

namespace Fotbiler.RuleGate.Core.Engine;

public sealed class PolicyAuthorizationEngine
    : IAuthorizationEngine
{
    private readonly IPolicyProvider _policyProvider;
    private readonly IRequirementEvaluationDispatcher
        _requirementDispatcher;

    public PolicyAuthorizationEngine(
        IPolicyProvider policyProvider,
        IRequirementEvaluationDispatcher
            requirementDispatcher)
    {
        ArgumentNullException.ThrowIfNull(
            policyProvider);

        ArgumentNullException.ThrowIfNull(
            requirementDispatcher);

        _policyProvider = policyProvider;
        _requirementDispatcher = requirementDispatcher;
    }

    public async ValueTask<AuthorizationDecision>
        EvaluateAsync(
            AuthorizationRequest request,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        cancellationToken.ThrowIfCancellationRequested();

        var policy = await _policyProvider.FindAsync(
            request.Resource.Type,
            request.Action,
            cancellationToken);

        if (policy is null)
        {
            return AuthorizationDecision.Deny(
                new AuthorizationFailure(
                    AuthorizationFailureCodes
                        .NoMatchingPolicy));
        }

        var result =
            await _requirementDispatcher.EvaluateAsync(
                policy.Requirement,
                new RequirementEvaluationContext(request),
                cancellationToken);

        if (result.IsSatisfied)
        {
            return AuthorizationDecision.Allow();
        }

        return AuthorizationDecision.Deny(
            result.Failures.ToArray());
    }
}
