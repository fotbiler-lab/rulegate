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
            return ValueTaskCompat.FromResult(
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
            return ValueTaskCompat.FromResult(
                RequirementEvaluationResult.Indeterminate(
                    new AuthorizationFailure(
                        AuthorizationFailureCodes
                            .AttributeTypeNotSupported,
                        requirement.Id)));
        }

        if (timestampValue.Kind !=
            AuthorizationAttributeValueKind.DateTimeOffset)
        {
            return ValueTaskCompat.FromResult(
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
            return ValueTaskCompat.FromResult(
                RequirementEvaluationResult.Indeterminate(
                    new AuthorizationFailure(
                        AuthorizationFailureCodes
                            .ContextTimestampInFuture,
                        requirement.Id)));
        }

        var isSatisfied =
            evaluationTime - timestamp <=
            requirement.MaximumAge;

        return ValueTaskCompat.FromResult(
            isSatisfied
                ? RequirementEvaluationResult.Satisfied()
                : RequirementEvaluationResult.NotSatisfied(
                    new AuthorizationFailure(
                        AuthorizationFailureCodes
                            .ContextAgeNotSatisfied,
                        requirement.Id)));
    }
}
