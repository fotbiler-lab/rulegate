using Fotbiler.RuleGate.Abstractions.Authorization;
using Fotbiler.RuleGate.Abstractions.Evaluation;
using Fotbiler.RuleGate.Abstractions.Policies;

namespace Fotbiler.RuleGate.Core.Evaluation.Evaluators;

public sealed class AnyRequirementEvaluator
    : RequirementEvaluator<AnyRequirementDefinition>
{
    protected override async ValueTask<RequirementEvaluationResult>
        EvaluateAsync(
            AnyRequirementDefinition requirement,
            RequirementEvaluationContext context,
            IRequirementEvaluationDispatcher dispatcher,
            CancellationToken cancellationToken)
    {
        var failures = new List<AuthorizationFailure>();
        var hasIndeterminateResult = false;

        foreach (var child in requirement.Requirements)
        {
            var result = await dispatcher.EvaluateAsync(
                child,
                context,
                cancellationToken);

            if (result.IsSatisfied)
            {
                return RequirementEvaluationResult.Satisfied();
            }

            failures.AddRange(result.Failures);
            hasIndeterminateResult |= result.IsIndeterminate;
        }

        return hasIndeterminateResult
            ? RequirementEvaluationResult.Indeterminate(
                failures.ToArray())
            : RequirementEvaluationResult.NotSatisfied(
                failures.ToArray());
    }
}
