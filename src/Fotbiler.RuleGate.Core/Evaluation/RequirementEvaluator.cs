using Fotbiler.RuleGate.Abstractions.Evaluation;
using Fotbiler.RuleGate.Abstractions.Policies;

namespace Fotbiler.RuleGate.Core.Evaluation;

public abstract class RequirementEvaluator<TRequirement>
    : IRequirementEvaluator
    where TRequirement : RequirementDefinition
{
    public Type RequirementType => typeof(TRequirement);

    public ValueTask<RequirementEvaluationResult> EvaluateAsync(
        RequirementDefinition requirement,
        RequirementEvaluationContext context,
        IRequirementEvaluationDispatcher dispatcher,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requirement);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(dispatcher);

        cancellationToken.ThrowIfCancellationRequested();

        if (requirement is not TRequirement typedRequirement)
        {
            throw new ArgumentException(
                $"Evaluator for '{typeof(TRequirement).Name}' cannot evaluate '{requirement.GetType().Name}'.",
                nameof(requirement));
        }

        return EvaluateAsync(
            typedRequirement,
            context,
            dispatcher,
            cancellationToken);
    }

    protected abstract ValueTask<RequirementEvaluationResult>
        EvaluateAsync(
            TRequirement requirement,
            RequirementEvaluationContext context,
            IRequirementEvaluationDispatcher dispatcher,
            CancellationToken cancellationToken);
}
