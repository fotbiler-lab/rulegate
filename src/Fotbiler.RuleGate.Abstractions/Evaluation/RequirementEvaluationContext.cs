using Fotbiler.RuleGate.Abstractions.Authorization;

namespace Fotbiler.RuleGate.Abstractions.Evaluation;

public sealed class RequirementEvaluationContext
{
    public RequirementEvaluationContext(
        AuthorizationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        Request = request;
    }

    public AuthorizationRequest Request { get; }

    public AuthorizationSubject Subject => Request.Subject;

    public AuthorizationResource Resource => Request.Resource;

    public AuthorizationContext AuthorizationContext =>
        Request.Context;
}
