using Fotbiler.RuleGate.Abstractions.Authorization;
using Fotbiler.RuleGate.Abstractions.Constants;
using Fotbiler.RuleGate.Abstractions.Evaluation;
using Fotbiler.RuleGate.Abstractions.Policies;

namespace Fotbiler.RuleGate.Core.Evaluation.Evaluators;

public sealed class NotRequirementEvaluator
    : RequirementEvaluator<NotRequirementDefinition>
{
    protected override async ValueTask<RequirementEvaluationResult>
        EvaluateAsync(
            NotRequirementDefinition requirement,
            RequirementEvaluationContext context,
            IRequirementEvaluationDispatcher dispatcher,
            CancellationToken cancellationToken)
    {
        var childResult = await dispatcher.EvaluateAsync(
            requirement.Requirement,
            context,
            cancellationToken);

        if (childResult.IsIndeterminate)
        {
            return RequirementEvaluationResult.Indeterminate(
                childResult.Failures.ToArray());
        }

        if (childResult.IsNotSatisfied)
        {
            return RequirementEvaluationResult.Satisfied();
        }

        return RequirementEvaluationResult.NotSatisfied(
            new AuthorizationFailure(
                AuthorizationFailureCodes
                    .NegatedRequirementSatisfied,
                requirement.Id));
    }
}
