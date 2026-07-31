using Fotbiler.RuleGate.Abstractions.Attributes;
using Fotbiler.RuleGate.Abstractions.Authorization;
using Fotbiler.RuleGate.Abstractions.Constants;
using Fotbiler.RuleGate.Abstractions.Evaluation;
using Fotbiler.RuleGate.Abstractions.Policies;

namespace Fotbiler.RuleGate.Core.Evaluation.Evaluators;

public sealed class ContextRequirementEvaluator
    : RequirementEvaluator<ContextRequirementDefinition>
{
    protected override ValueTask<RequirementEvaluationResult>
        EvaluateAsync(
            ContextRequirementDefinition requirement,
            RequirementEvaluationContext context,
            IRequirementEvaluationDispatcher dispatcher,
            CancellationToken cancellationToken)
    {
        if (!context.AuthorizationContext.Attributes.TryGetValue(
                requirement.AttributeName,
                out var rawValue))
        {
            return ValueTaskCompat.FromResult(
                RequirementEvaluationResult.NotSatisfied(
                    new AuthorizationFailure(
                        AuthorizationFailureCodes
                            .AttributeNotFound,
                        requirement.Id)));
        }

        AuthorizationAttributeValue actualValue;

        try
        {
            actualValue =
                AuthorizationAttributeValue.Create(
                    rawValue);
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

        var comparison =
            AttributeRequirementEvaluator.CompareValues(
                actualValue,
                requirement.ExpectedValue,
                requirement.Operator,
                requirement.StringComparison);

        var result = comparison switch
        {
            AttributeRequirementEvaluator
                .AttributeComparisonResult.Satisfied =>
                RequirementEvaluationResult.Satisfied(),

            AttributeRequirementEvaluator
                .AttributeComparisonResult.NotSatisfied =>
                RequirementEvaluationResult.NotSatisfied(
                    new AuthorizationFailure(
                        AuthorizationFailureCodes
                            .AttributeComparisonNotSatisfied,
                        requirement.Id)),

            AttributeRequirementEvaluator
                .AttributeComparisonResult.TypeMismatch =>
                RequirementEvaluationResult.Indeterminate(
                    new AuthorizationFailure(
                        AuthorizationFailureCodes
                            .AttributeTypeMismatch,
                        requirement.Id)),

            _ =>
                RequirementEvaluationResult.Indeterminate(
                    new AuthorizationFailure(
                        AuthorizationFailureCodes
                            .AttributeOperatorNotSupported,
                        requirement.Id))
        };

        return ValueTaskCompat.FromResult(result);
    }
}
