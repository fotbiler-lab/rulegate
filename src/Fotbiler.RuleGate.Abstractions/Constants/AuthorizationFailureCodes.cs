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
}
