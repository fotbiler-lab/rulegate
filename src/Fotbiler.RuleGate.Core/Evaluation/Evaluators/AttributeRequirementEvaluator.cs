using Fotbiler.RuleGate.Abstractions.Attributes;
using Fotbiler.RuleGate.Abstractions.Authorization;
using Fotbiler.RuleGate.Abstractions.Constants;
using Fotbiler.RuleGate.Abstractions.Evaluation;
using Fotbiler.RuleGate.Abstractions.Policies;

namespace Fotbiler.RuleGate.Core.Evaluation.Evaluators;

public sealed class AttributeRequirementEvaluator
    : RequirementEvaluator<AttributeRequirementDefinition>
{
    protected override ValueTask<RequirementEvaluationResult>
        EvaluateAsync(
            AttributeRequirementDefinition requirement,
            RequirementEvaluationContext context,
            IRequirementEvaluationDispatcher dispatcher,
            CancellationToken cancellationToken)
    {
        var attributes =
            GetAttributes(
                requirement.Source,
                context);

        if (!attributes.TryGetValue(
                requirement.Name,
                out var actualRawValue))
        {
            return ValueTask.FromResult(
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
                    actualRawValue);
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

        if (actualValue.Kind !=
            requirement.ExpectedValue.Kind)
        {
            return ValueTask.FromResult(
                RequirementEvaluationResult.Indeterminate(
                    new AuthorizationFailure(
                        AuthorizationFailureCodes
                            .AttributeTypeMismatch,
                        requirement.Id)));
        }

        if (!TryCompare(
                actualValue,
                requirement.ExpectedValue,
                requirement.Operator,
                out var isSatisfied))
        {
            return ValueTask.FromResult(
                RequirementEvaluationResult.Indeterminate(
                    new AuthorizationFailure(
                        AuthorizationFailureCodes
                            .AttributeOperatorNotSupported,
                        requirement.Id)));
        }

        var result = isSatisfied
            ? RequirementEvaluationResult.Satisfied()
            : RequirementEvaluationResult.NotSatisfied(
                new AuthorizationFailure(
                    AuthorizationFailureCodes
                        .AttributeComparisonNotSatisfied,
                    requirement.Id));

        return ValueTask.FromResult(result);
    }

    private static AuthorizationAttributes GetAttributes(
        AuthorizationAttributeSource source,
        RequirementEvaluationContext context)
    {
        return source switch
        {
            AuthorizationAttributeSource.Subject =>
                context.Subject.Attributes,

            AuthorizationAttributeSource.Resource =>
                context.Resource.Attributes,

            AuthorizationAttributeSource.Context =>
                context.AuthorizationContext.Attributes,

            _ =>
                throw new ArgumentOutOfRangeException(
                    nameof(source),
                    source,
                    "The authorization attribute source is not supported.")
        };
    }

    private static bool TryCompare(
        AuthorizationAttributeValue actual,
        AuthorizationAttributeValue expected,
        AuthorizationAttributeOperator @operator,
        out bool isSatisfied)
    {
        if (@operator is
            AuthorizationAttributeOperator.Equal or
            AuthorizationAttributeOperator.NotEqual)
        {
            var areEqual =
                AreEqual(actual, expected);

            isSatisfied =
                @operator ==
                AuthorizationAttributeOperator.Equal
                    ? areEqual
                    : !areEqual;

            return true;
        }

        if (!TryGetOrderingComparison(
                actual,
                expected,
                out var comparison))
        {
            isSatisfied = false;
            return false;
        }

        switch (@operator)
        {
            case AuthorizationAttributeOperator
                .GreaterThan:
                isSatisfied = comparison > 0;
                return true;

            case AuthorizationAttributeOperator
                .GreaterThanOrEqual:
                isSatisfied = comparison >= 0;
                return true;

            case AuthorizationAttributeOperator
                .LessThan:
                isSatisfied = comparison < 0;
                return true;

            case AuthorizationAttributeOperator
                .LessThanOrEqual:
                isSatisfied = comparison <= 0;
                return true;

            default:
                isSatisfied = false;
                return false;
        }
    }

    private static bool AreEqual(
        AuthorizationAttributeValue actual,
        AuthorizationAttributeValue expected)
    {
        return actual.Kind switch
        {
            AuthorizationAttributeValueKind.Null =>
                true,

            AuthorizationAttributeValueKind.String =>
                string.Equals(
                    (string)actual.Value!,
                    (string)expected.Value!,
                    StringComparison.Ordinal),

            AuthorizationAttributeValueKind.Boolean =>
                (bool)actual.Value! ==
                (bool)expected.Value!,

            AuthorizationAttributeValueKind.Number =>
                (decimal)actual.Value! ==
                (decimal)expected.Value!,

            AuthorizationAttributeValueKind.DateTimeOffset =>
                DateTimeOffset.Compare(
                    (DateTimeOffset)actual.Value!,
                    (DateTimeOffset)expected.Value!) == 0,

            _ => false
        };
    }

    private static bool TryGetOrderingComparison(
        AuthorizationAttributeValue actual,
        AuthorizationAttributeValue expected,
        out int comparison)
    {
        switch (actual.Kind)
        {
            case AuthorizationAttributeValueKind.Number:
                comparison = decimal.Compare(
                    (decimal)actual.Value!,
                    (decimal)expected.Value!);

                return true;

            case AuthorizationAttributeValueKind
                .DateTimeOffset:
                comparison = DateTimeOffset.Compare(
                    (DateTimeOffset)actual.Value!,
                    (DateTimeOffset)expected.Value!);

                return true;

            default:
                comparison = 0;
                return false;
        }
    }
}
