namespace Fotbiler.RuleGate.Abstractions.Constants;

public static class AuthorizationFailureCodes
{
    public const string NoMatchingPolicy =
        "RULEGATE_NO_MATCHING_POLICY";

    public const string MissingPermission =
        "RULEGATE_MISSING_PERMISSION";

    public const string MissingRole =
        "RULEGATE_MISSING_ROLE";

    public const string RequirementEvaluatorNotFound =
        "RULEGATE_REQUIREMENT_EVALUATOR_NOT_FOUND";

    public const string NegatedRequirementSatisfied =
        "RULEGATE_NEGATED_REQUIREMENT_SATISFIED";

    public const string AttributeNotFound =
        "RULEGATE_ATTRIBUTE_NOT_FOUND";

    public const string AttributeComparisonNotSatisfied =
        "RULEGATE_ATTRIBUTE_COMPARISON_NOT_SATISFIED";

    public const string AttributeTypeMismatch =
        "RULEGATE_ATTRIBUTE_TYPE_MISMATCH";

    public const string AttributeTypeNotSupported =
        "RULEGATE_ATTRIBUTE_TYPE_NOT_SUPPORTED";

    public const string AttributeOperatorNotSupported =
        "RULEGATE_ATTRIBUTE_OPERATOR_NOT_SUPPORTED";

    public const string TimeWindowNotSatisfied =
        "RULEGATE_TIME_WINDOW_NOT_SATISFIED";

    public const string DateTimeWindowNotSatisfied =
        "RULEGATE_DATE_TIME_WINDOW_NOT_SATISFIED";

    public const string ContextAgeNotSatisfied =
        "RULEGATE_CONTEXT_AGE_NOT_SATISFIED";

    public const string ContextTimestampInFuture =
        "RULEGATE_CONTEXT_TIMESTAMP_IN_FUTURE";
}
