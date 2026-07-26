using System.Collections.Frozen;
using Fotbiler.RuleGate.Abstractions.Authorization;
using Fotbiler.RuleGate.Abstractions.Constants;
using Fotbiler.RuleGate.Abstractions.Evaluation;
using Fotbiler.RuleGate.Abstractions.Policies;

namespace Fotbiler.RuleGate.Core.Evaluation;

public sealed class RequirementEvaluationDispatcher
    : IRequirementEvaluationDispatcher
{
    private readonly FrozenDictionary<Type, IRequirementEvaluator>
        _evaluators;

    public RequirementEvaluationDispatcher(
        IEnumerable<IRequirementEvaluator> evaluators)
    {
        ArgumentNullException.ThrowIfNull(evaluators);

        var evaluatorMap =
            new Dictionary<Type, IRequirementEvaluator>();

        foreach (var evaluator in evaluators)
        {
            ArgumentNullException.ThrowIfNull(evaluator);

            if (!typeof(RequirementDefinition).IsAssignableFrom(
                    evaluator.RequirementType))
            {
                throw new ArgumentException(
                    $"Evaluator requirement type '{evaluator.RequirementType}' must derive from RequirementDefinition.",
                    nameof(evaluators));
            }

            if (!evaluatorMap.TryAdd(
                    evaluator.RequirementType,
                    evaluator))
            {
                throw new InvalidOperationException(
                    $"Multiple evaluators are registered for requirement type '{evaluator.RequirementType.Name}'.");
            }
        }

        _evaluators = evaluatorMap.ToFrozenDictionary();
    }

    public ValueTask<RequirementEvaluationResult> EvaluateAsync(
        RequirementDefinition requirement,
        RequirementEvaluationContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requirement);
        ArgumentNullException.ThrowIfNull(context);

        cancellationToken.ThrowIfCancellationRequested();

        if (!_evaluators.TryGetValue(
                requirement.GetType(),
                out var evaluator))
        {
            return ValueTask.FromResult(
                RequirementEvaluationResult.Indeterminate(
                    new AuthorizationFailure(
                        AuthorizationFailureCodes
                            .RequirementEvaluatorNotFound,
                        requirement.Id)));
        }

        return evaluator.EvaluateAsync(
            requirement,
            context,
            this,
            cancellationToken);
    }
}
