using Fotbiler.RuleGate.Abstractions.Authorization;
using Fotbiler.RuleGate.Abstractions.Constants;
using Fotbiler.RuleGate.Abstractions.Evaluation;
using Fotbiler.RuleGate.Abstractions.Policies;

namespace Fotbiler.RuleGate.Core.Evaluation.Evaluators;

public sealed class RoleRequirementEvaluator
    : RequirementEvaluator<RoleRequirementDefinition>
{
    protected override ValueTask<RequirementEvaluationResult>
        EvaluateAsync(
            RoleRequirementDefinition requirement,
            RequirementEvaluationContext context,
            IRequirementEvaluationDispatcher dispatcher,
            CancellationToken cancellationToken)
    {
        var result = context.Subject.Roles.Contains(
            requirement.Role)
            ? RequirementEvaluationResult.Satisfied()
            : RequirementEvaluationResult.NotSatisfied(
                new AuthorizationFailure(
                    AuthorizationFailureCodes.MissingRole,
                    requirement.Id));

        return ValueTask.FromResult(result);
    }
}
