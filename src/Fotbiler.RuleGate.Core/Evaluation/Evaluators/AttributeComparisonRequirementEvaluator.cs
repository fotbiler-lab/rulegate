using Fotbiler.RuleGate.Abstractions.Attributes;
using Fotbiler.RuleGate.Abstractions.Authorization;
using Fotbiler.RuleGate.Abstractions.Constants;
using Fotbiler.RuleGate.Abstractions.Evaluation;
using Fotbiler.RuleGate.Abstractions.Policies;

namespace Fotbiler.RuleGate.Core.Evaluation.Evaluators;

public sealed class AttributeComparisonRequirementEvaluator
    : RequirementEvaluator<
        AttributeComparisonRequirementDefinition>
{
    protected override ValueTask<RequirementEvaluationResult>
        EvaluateAsync(
            AttributeComparisonRequirementDefinition requirement,
            RequirementEvaluationContext context,
            IRequirementEvaluationDispatcher dispatcher,
            CancellationToken cancellationToken)
    {
        var leftResolution = Resolve(
            requirement.Left,
            context);

        if (leftResolution.FailureCode is not null)
        {
            return ValueTask.FromResult(
                CreateResolutionFailure(
                    leftResolution,
                    requirement.Id));
        }

        var rightResolution = Resolve(
            requirement.Right,
            context);

        if (rightResolution.FailureCode is not null)
        {
            return ValueTask.FromResult(
                CreateResolutionFailure(
                    rightResolution,
                    requirement.Id));
        }

        var comparison =
            AttributeRequirementEvaluator.CompareValues(
                leftResolution.Value!,
                rightResolution.Value!,
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

        return ValueTask.FromResult(result);
    }

    private static OperandResolution Resolve(
        AuthorizationAttributeOperand operand,
        RequirementEvaluationContext context)
    {
        if (operand.IsLiteral)
        {
            return OperandResolution.Success(
                operand.LiteralValue!);
        }

        var source = operand.Kind switch
        {
            AuthorizationAttributeOperandKind.Subject =>
                AuthorizationAttributeSource.Subject,

            AuthorizationAttributeOperandKind.Resource =>
                AuthorizationAttributeSource.Resource,

            AuthorizationAttributeOperandKind.Context =>
                AuthorizationAttributeSource.Context,

            _ => throw new ArgumentOutOfRangeException(
                nameof(operand),
                operand.Kind,
                "The authorization attribute operand kind is not supported.")
        };

        var attributes =
            AttributeRequirementEvaluator.GetAttributes(
                source,
                context);

        if (!attributes.TryGetValue(
                operand.Name!,
                out var rawValue))
        {
            return OperandResolution.Failure(
                AuthorizationFailureCodes
                    .AttributeNotFound,
                indeterminate: false);
        }

        try
        {
            return OperandResolution.Success(
                AuthorizationAttributeValue.Create(
                    rawValue));
        }
        catch (ArgumentException)
        {
            return OperandResolution.Failure(
                AuthorizationFailureCodes
                    .AttributeTypeNotSupported,
                indeterminate: true);
        }
    }

    private static RequirementEvaluationResult
        CreateResolutionFailure(
            OperandResolution resolution,
            string? requirementId)
    {
        var failure = new AuthorizationFailure(
            resolution.FailureCode!,
            requirementId);

        return resolution.IsIndeterminate
            ? RequirementEvaluationResult.Indeterminate(
                failure)
            : RequirementEvaluationResult.NotSatisfied(
                failure);
    }

    private readonly record struct OperandResolution(
        AuthorizationAttributeValue? Value,
        string? FailureCode,
        bool IsIndeterminate)
    {
        internal static OperandResolution Success(
            AuthorizationAttributeValue value)
        {
            return new OperandResolution(
                value,
                FailureCode: null,
                IsIndeterminate: false);
        }

        internal static OperandResolution Failure(
            string failureCode,
            bool indeterminate)
        {
            return new OperandResolution(
                Value: null,
                failureCode,
                indeterminate);
        }
    }
}
