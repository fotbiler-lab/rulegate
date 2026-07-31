using Fotbiler.RuleGate.Abstractions.Authorization;
using Fotbiler.RuleGate.Abstractions.Constants;
using Fotbiler.RuleGate.Abstractions.Evaluation;
using Fotbiler.RuleGate.Abstractions.Policies;

namespace Fotbiler.RuleGate.Core.Evaluation.Evaluators;

public sealed class DateTimeWindowRequirementEvaluator
    : RequirementEvaluator<
        DateTimeWindowRequirementDefinition>
{
    protected override ValueTask<RequirementEvaluationResult>
        EvaluateAsync(
            DateTimeWindowRequirementDefinition requirement,
            RequirementEvaluationContext context,
            IRequirementEvaluationDispatcher dispatcher,
            CancellationToken cancellationToken)
    {
        var evaluationTime =
            context.AuthorizationContext
                .EvaluationTime
                .ToUniversalTime();

        var isSatisfied =
            (requirement.StartsAt is null ||
             evaluationTime >= requirement.StartsAt) &&
            (requirement.EndsAt is null ||
             evaluationTime < requirement.EndsAt);

        return ValueTaskCompat.FromResult(
            isSatisfied
                ? RequirementEvaluationResult.Satisfied()
                : RequirementEvaluationResult.NotSatisfied(
                    new AuthorizationFailure(
                        AuthorizationFailureCodes
                            .DateTimeWindowNotSatisfied,
                        requirement.Id)));
    }
}
