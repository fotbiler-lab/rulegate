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

        var attributeExists =
            attributes.TryGetValue(
                requirement.Name,
                out var actualRawValue);

        if (requirement.Operator is
            AuthorizationAttributeOperator.Exists or
            AuthorizationAttributeOperator.NotExists)
        {
            var isSatisfied =
                requirement.Operator ==
                AuthorizationAttributeOperator.Exists
                    ? attributeExists
                    : !attributeExists;

            return ValueTaskCompat.FromResult(
                CreateBooleanResult(
                    isSatisfied,
                    requirement,
                    attributeExists
                        ? AuthorizationFailureCodes
                            .AttributeComparisonNotSatisfied
                        : AuthorizationFailureCodes
                            .AttributeNotFound));
        }

        if (!attributeExists)
        {
            return ValueTaskCompat.FromResult(
                RequirementEvaluationResult.NotSatisfied(
                    new AuthorizationFailure(
                        AuthorizationFailureCodes
                            .AttributeNotFound,
                        requirement.Id)));
        }

        if (requirement.Operator is
            AuthorizationAttributeOperator.IsNull or
            AuthorizationAttributeOperator.IsNotNull)
        {
            var isNull = actualRawValue is null;

            var isSatisfied =
                requirement.Operator ==
                AuthorizationAttributeOperator.IsNull
                    ? isNull
                    : !isNull;

            return ValueTaskCompat.FromResult(
                CreateBooleanResult(
                    isSatisfied,
                    requirement,
                    AuthorizationFailureCodes
                        .AttributeComparisonNotSatisfied));
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
            return ValueTaskCompat.FromResult(
                RequirementEvaluationResult.Indeterminate(
                    new AuthorizationFailure(
                        AuthorizationFailureCodes
                            .AttributeTypeNotSupported,
                        requirement.Id)));
        }

        var comparison =
            CompareValues(
                actualValue,
                requirement.ExpectedValue,
                requirement.Operator,
                requirement.StringComparison);

        var result = comparison switch
        {
            AttributeComparisonResult.Satisfied =>
                RequirementEvaluationResult.Satisfied(),

            AttributeComparisonResult.NotSatisfied =>
                RequirementEvaluationResult.NotSatisfied(
                    new AuthorizationFailure(
                        AuthorizationFailureCodes
                            .AttributeComparisonNotSatisfied,
                        requirement.Id)),

            AttributeComparisonResult.TypeMismatch =>
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

    private static RequirementEvaluationResult
        CreateBooleanResult(
            bool isSatisfied,
            AttributeRequirementDefinition requirement,
            string failureCode)
    {
        return isSatisfied
            ? RequirementEvaluationResult.Satisfied()
            : RequirementEvaluationResult.NotSatisfied(
                new AuthorizationFailure(
                    failureCode,
                    requirement.Id));
    }

    internal static AuthorizationAttributes GetAttributes(
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

    internal static AttributeComparisonResult CompareValues(
        AuthorizationAttributeValue actual,
        AuthorizationAttributeValue expected,
        AuthorizationAttributeOperator @operator,
        AuthorizationStringComparison stringComparison)
    {
        return @operator switch
        {
            AuthorizationAttributeOperator.Equal =>
                CompareEquality(
                    actual,
                    expected,
                    stringComparison,
                    negate: false),

            AuthorizationAttributeOperator.NotEqual =>
                CompareEquality(
                    actual,
                    expected,
                    stringComparison,
                    negate: true),

            AuthorizationAttributeOperator.GreaterThan or
            AuthorizationAttributeOperator
                .GreaterThanOrEqual or
            AuthorizationAttributeOperator.LessThan or
            AuthorizationAttributeOperator.LessThanOrEqual =>
                CompareOrdering(
                    actual,
                    expected,
                    @operator),

            AuthorizationAttributeOperator.Contains =>
                CompareContains(
                    actual,
                    expected,
                    stringComparison),

            AuthorizationAttributeOperator.StartsWith =>
                CompareStringOperation(
                    actual,
                    expected,
                    stringComparison,
                    static (value, candidate, comparison) =>
                        value.StartsWith(
                            candidate,
                            comparison)),

            AuthorizationAttributeOperator.EndsWith =>
                CompareStringOperation(
                    actual,
                    expected,
                    stringComparison,
                    static (value, candidate, comparison) =>
                        value.EndsWith(
                            candidate,
                            comparison)),

            AuthorizationAttributeOperator.ContainsAny or
            AuthorizationAttributeOperator.Intersects =>
                CompareCollectionIntersection(
                    actual,
                    expected,
                    stringComparison),

            AuthorizationAttributeOperator.ContainsAll =>
                CompareCollectionContainsAll(
                    actual,
                    expected,
                    stringComparison),

            AuthorizationAttributeOperator.In =>
                CompareMembership(
                    actual,
                    expected,
                    stringComparison,
                    negate: false),

            AuthorizationAttributeOperator.NotIn =>
                CompareMembership(
                    actual,
                    expected,
                    stringComparison,
                    negate: true),

            AuthorizationAttributeOperator.IsEmpty =>
                CompareCollectionEmpty(
                    actual,
                    negate: false),

            AuthorizationAttributeOperator.IsNotEmpty =>
                CompareCollectionEmpty(
                    actual,
                    negate: true),

            _ =>
                AttributeComparisonResult
                    .OperatorNotSupported
        };
    }

    private static AttributeComparisonResult
        CompareEquality(
            AuthorizationAttributeValue actual,
            AuthorizationAttributeValue expected,
            AuthorizationStringComparison stringComparison,
            bool negate)
    {
        if (actual.Kind != expected.Kind)
        {
            return AttributeComparisonResult.TypeMismatch;
        }

        if (actual.Kind ==
            AuthorizationAttributeValueKind.Collection)
        {
            return AttributeComparisonResult
                .OperatorNotSupported;
        }

        var areEqual =
            AreEqual(
                actual,
                expected,
                stringComparison);

        return ToComparisonResult(
            negate ? !areEqual : areEqual);
    }

    private static AttributeComparisonResult
        CompareOrdering(
            AuthorizationAttributeValue actual,
            AuthorizationAttributeValue expected,
            AuthorizationAttributeOperator @operator)
    {
        if (actual.Kind != expected.Kind)
        {
            return AttributeComparisonResult.TypeMismatch;
        }

        if (!TryGetOrderingComparison(
                actual,
                expected,
                out var comparison))
        {
            return AttributeComparisonResult
                .OperatorNotSupported;
        }

        var isSatisfied = @operator switch
        {
            AuthorizationAttributeOperator.GreaterThan =>
                comparison > 0,

            AuthorizationAttributeOperator
                .GreaterThanOrEqual =>
                comparison >= 0,

            AuthorizationAttributeOperator.LessThan =>
                comparison < 0,

            AuthorizationAttributeOperator.LessThanOrEqual =>
                comparison <= 0,

            _ => false
        };

        return ToComparisonResult(isSatisfied);
    }

    private static AttributeComparisonResult
        CompareContains(
            AuthorizationAttributeValue actual,
            AuthorizationAttributeValue expected,
            AuthorizationStringComparison stringComparison)
    {
        if (actual.Kind ==
            AuthorizationAttributeValueKind.String)
        {
            return CompareStringOperation(
                actual,
                expected,
                stringComparison,
                static (value, candidate, comparison) =>
                    value.Contains(
                        candidate,
                        comparison));
        }

        if (actual.Kind !=
            AuthorizationAttributeValueKind.Collection ||
            expected.Kind ==
            AuthorizationAttributeValueKind.Collection)
        {
            return AttributeComparisonResult
                .OperatorNotSupported;
        }

        return CollectionContains(
            actual,
            expected,
            stringComparison,
            out var contains)
                ? ToComparisonResult(contains)
                : AttributeComparisonResult.TypeMismatch;
    }

    private static AttributeComparisonResult
        CompareStringOperation(
            AuthorizationAttributeValue actual,
            AuthorizationAttributeValue expected,
            AuthorizationStringComparison stringComparison,
            Func<string, string, StringComparison, bool>
                operation)
    {
        if (actual.Kind != expected.Kind)
        {
            return AttributeComparisonResult.TypeMismatch;
        }

        if (actual.Kind !=
            AuthorizationAttributeValueKind.String)
        {
            return AttributeComparisonResult
                .OperatorNotSupported;
        }

        return ToComparisonResult(
            operation(
                (string)actual.Value!,
                (string)expected.Value!,
                ToStringComparison(stringComparison)));
    }

    private static AttributeComparisonResult
        CompareCollectionIntersection(
            AuthorizationAttributeValue actual,
            AuthorizationAttributeValue expected,
            AuthorizationStringComparison stringComparison)
    {
        if (!AreCollections(actual, expected))
        {
            return AttributeComparisonResult
                .OperatorNotSupported;
        }

        if (!AreCollectionKindsCompatible(
                actual,
                expected))
        {
            return AttributeComparisonResult.TypeMismatch;
        }

        var isSatisfied =
            expected.CollectionItems.Any(
                expectedItem =>
                    CollectionContains(
                        actual,
                        expectedItem,
                        stringComparison,
                        out var contains) &&
                    contains);

        return ToComparisonResult(isSatisfied);
    }

    private static AttributeComparisonResult
        CompareCollectionContainsAll(
            AuthorizationAttributeValue actual,
            AuthorizationAttributeValue expected,
            AuthorizationStringComparison stringComparison)
    {
        if (!AreCollections(actual, expected))
        {
            return AttributeComparisonResult
                .OperatorNotSupported;
        }

        if (!AreCollectionKindsCompatible(
                actual,
                expected))
        {
            return AttributeComparisonResult.TypeMismatch;
        }

        var isSatisfied =
            expected.CollectionItems.All(
                expectedItem =>
                    CollectionContains(
                        actual,
                        expectedItem,
                        stringComparison,
                        out var contains) &&
                    contains);

        return ToComparisonResult(isSatisfied);
    }

    private static AttributeComparisonResult
        CompareMembership(
            AuthorizationAttributeValue actual,
            AuthorizationAttributeValue expected,
            AuthorizationStringComparison stringComparison,
            bool negate)
    {
        if (actual.Kind ==
                AuthorizationAttributeValueKind.Collection ||
            expected.Kind !=
                AuthorizationAttributeValueKind.Collection)
        {
            return AttributeComparisonResult
                .OperatorNotSupported;
        }

        if (!CollectionContains(
                expected,
                actual,
                stringComparison,
                out var contains))
        {
            return AttributeComparisonResult.TypeMismatch;
        }

        return ToComparisonResult(
            negate ? !contains : contains);
    }

    private static AttributeComparisonResult
        CompareCollectionEmpty(
            AuthorizationAttributeValue actual,
            bool negate)
    {
        if (actual.Kind !=
            AuthorizationAttributeValueKind.Collection)
        {
            return AttributeComparisonResult
                .OperatorNotSupported;
        }

        var isEmpty =
            actual.CollectionItems.Count == 0;

        return ToComparisonResult(
            negate ? !isEmpty : isEmpty);
    }

    private static bool CollectionContains(
        AuthorizationAttributeValue collection,
        AuthorizationAttributeValue candidate,
        AuthorizationStringComparison stringComparison,
        out bool contains)
    {
        if (collection.Kind !=
            AuthorizationAttributeValueKind.Collection)
        {
            contains = false;
            return false;
        }

        if (collection.CollectionElementKind is not null &&
            collection.CollectionElementKind != candidate.Kind)
        {
            contains = false;
            return false;
        }

        contains =
            collection.CollectionItems.Any(
                item =>
                    AreEqual(
                        item,
                        candidate,
                        stringComparison));

        return true;
    }

    private static bool AreCollections(
        AuthorizationAttributeValue actual,
        AuthorizationAttributeValue expected)
    {
        return actual.Kind ==
                AuthorizationAttributeValueKind.Collection &&
            expected.Kind ==
                AuthorizationAttributeValueKind.Collection;
    }

    private static bool AreCollectionKindsCompatible(
        AuthorizationAttributeValue actual,
        AuthorizationAttributeValue expected)
    {
        return actual.CollectionElementKind is null ||
            expected.CollectionElementKind is null ||
            actual.CollectionElementKind ==
                expected.CollectionElementKind;
    }

    private static bool AreEqual(
        AuthorizationAttributeValue actual,
        AuthorizationAttributeValue expected,
        AuthorizationStringComparison stringComparison)
    {
        if (actual.Kind != expected.Kind)
        {
            return false;
        }

        return actual.Kind switch
        {
            AuthorizationAttributeValueKind.Null =>
                true,

            AuthorizationAttributeValueKind.String =>
                string.Equals(
                    (string)actual.Value!,
                    (string)expected.Value!,
                    ToStringComparison(
                        stringComparison)),

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

    private static StringComparison ToStringComparison(
        AuthorizationStringComparison comparison)
    {
        return comparison switch
        {
            AuthorizationStringComparison.Ordinal =>
                StringComparison.Ordinal,

            AuthorizationStringComparison.OrdinalIgnoreCase =>
                StringComparison.OrdinalIgnoreCase,

            _ =>
                throw new ArgumentOutOfRangeException(
                    nameof(comparison),
                    comparison,
                    "The authorization string comparison is not supported.")
        };
    }

    private static AttributeComparisonResult
        ToComparisonResult(
            bool isSatisfied)
    {
        return isSatisfied
            ? AttributeComparisonResult.Satisfied
            : AttributeComparisonResult.NotSatisfied;
    }

    internal enum AttributeComparisonResult
    {
        NotSatisfied = 0,
        Satisfied = 1,
        TypeMismatch = 2,
        OperatorNotSupported = 3
    }
}
