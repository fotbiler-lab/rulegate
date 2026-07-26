using Fotbiler.RuleGate.Abstractions.Policies;

namespace Fotbiler.RuleGate.Abstractions.Evaluation;

public interface IRequirementEvaluator
{
    Type RequirementType { get; }

    ValueTask<RequirementEvaluationResult> EvaluateAsync(
        RequirementDefinition requirement,
        RequirementEvaluationContext context,
        IRequirementEvaluationDispatcher dispatcher,
        CancellationToken cancellationToken = default);
}
