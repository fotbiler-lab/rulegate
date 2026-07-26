using Fotbiler.RuleGate.Abstractions.Policies;

namespace Fotbiler.RuleGate.Abstractions.Evaluation;

public interface IRequirementEvaluationDispatcher
{
    ValueTask<RequirementEvaluationResult> EvaluateAsync(
        RequirementDefinition requirement,
        RequirementEvaluationContext context,
        CancellationToken cancellationToken = default);
}
