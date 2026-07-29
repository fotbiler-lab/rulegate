using Fotbiler.RuleGate.Abstractions.Attributes;
using Fotbiler.RuleGate.Abstractions.Authorization;
using Fotbiler.RuleGate.Abstractions.Constants;
using Fotbiler.RuleGate.Abstractions.Evaluation;
using Fotbiler.RuleGate.Abstractions.Policies;

namespace Fotbiler.RuleGate.Core.Evaluation.Evaluators;

public sealed class ContextAgeRequirementEvaluator
    : RequirementEvaluator<ContextAgeRequirementDefinition>
{
    protected override ValueTask<RequirementEvaluationResult>
        EvaluateAsync(
            ContextAgeRequirementDefinition requirement,
            RequirementEvaluationContext context,
            IRequirementEvaluationDispatcher dispatcher,
            CancellationToken cancellationToken)
    {
        if (!context.AuthorizationContext.Attributes.TryGetValue(
                requirement.AttributeName,
                out var rawTimestamp))
        {
            return ValueTask.FromResult(
                RequirementEvaluationResult.NotSatisfied(
                    new AuthorizationFailure(
                        AuthorizationFailureCodes
                            .AttributeNotFound,
                        requirement.Id)));
        }

        AuthorizationAttributeValue timestampValue;

        try
        {
            timestampValue =
                AuthorizationAttributeValue.Create(
                    rawTimestamp);
        }
        catch (ArgumentException)
        {
            return ValueTask.FromResult(
                RequirementEvaluationResult.Indeterminate(
                    new AuthorizationFailure(
                        AuthorizationFailureCodes
                            .AttributeTypeNotSupported,
                        requirement.Id)));
        }

        if (timestampValue.Kind !=
            AuthorizationAttributeValueKind.DateTimeOffset)
        {
            return ValueTask.FromResult(
                RequirementEvaluationResult.Indeterminate(
                    new AuthorizationFailure(
                        AuthorizationFailureCodes
                            .AttributeTypeMismatch,
                        requirement.Id)));
        }

        var timestamp =
            (DateTimeOffset)timestampValue.Value!;

        var evaluationTime =
            context.AuthorizationContext.EvaluationTime;

        if (timestamp > evaluationTime)
        {
            return ValueTask.FromResult(
                RequirementEvaluationResult.Indeterminate(
                    new AuthorizationFailure(
                        AuthorizationFailureCodes
                            .ContextTimestampInFuture,
                        requirement.Id)));
        }

        var isSatisfied =
            evaluationTime - timestamp <=
            requirement.MaximumAge;

        return ValueTask.FromResult(
            isSatisfied
                ? RequirementEvaluationResult.Satisfied()
                : RequirementEvaluationResult.NotSatisfied(
                    new AuthorizationFailure(
                        AuthorizationFailureCodes
                            .ContextAgeNotSatisfied,
                        requirement.Id)));
    }
}
