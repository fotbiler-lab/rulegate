using Fotbiler.RuleGate.Abstractions.Authorization;
using Fotbiler.RuleGate.Abstractions.Evaluation;
using Fotbiler.RuleGate.Abstractions.Policies;

namespace Fotbiler.RuleGate.Core.Evaluation.Evaluators;

public sealed class AllRequirementEvaluator
    : RequirementEvaluator<AllRequirementDefinition>
{
    protected override async ValueTask<RequirementEvaluationResult>
        EvaluateAsync(
            AllRequirementDefinition requirement,
            RequirementEvaluationContext context,
            IRequirementEvaluationDispatcher dispatcher,
            CancellationToken cancellationToken)
    {
        var failures = new List<AuthorizationFailure>();
        var hasNotSatisfiedResult = false;
        var hasIndeterminateResult = false;

        foreach (var child in requirement.Requirements)
        {
            var result = await dispatcher.EvaluateAsync(
                child,
                context,
                cancellationToken);

            if (result.IsSatisfied)
            {
                continue;
            }

            failures.AddRange(result.Failures);
            hasNotSatisfiedResult |= result.IsNotSatisfied;
            hasIndeterminateResult |= result.IsIndeterminate;
        }

        if (hasNotSatisfiedResult)
        {
            return RequirementEvaluationResult.NotSatisfied(
                failures.ToArray());
        }

        if (hasIndeterminateResult)
        {
            return RequirementEvaluationResult.Indeterminate(
                failures.ToArray());
        }

        return RequirementEvaluationResult.Satisfied();
    }
}
