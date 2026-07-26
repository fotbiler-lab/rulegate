using Fotbiler.RuleGate.Abstractions.Authorization;
using Fotbiler.RuleGate.Abstractions.Constants;
using Fotbiler.RuleGate.Abstractions.Evaluation;
using Fotbiler.RuleGate.Abstractions.Policies;

namespace Fotbiler.RuleGate.Core.Evaluation.Evaluators;

public sealed class PermissionRequirementEvaluator
    : RequirementEvaluator<PermissionRequirementDefinition>
{
    protected override ValueTask<RequirementEvaluationResult>
        EvaluateAsync(
            PermissionRequirementDefinition requirement,
            RequirementEvaluationContext context,
            IRequirementEvaluationDispatcher dispatcher,
            CancellationToken cancellationToken)
    {
        var result = context.Subject.Permissions.Contains(
            requirement.Permission)
            ? RequirementEvaluationResult.Satisfied()
            : RequirementEvaluationResult.NotSatisfied(
                new AuthorizationFailure(
                    AuthorizationFailureCodes.MissingPermission,
                    requirement.Id));

        return ValueTask.FromResult(result);
    }
}
