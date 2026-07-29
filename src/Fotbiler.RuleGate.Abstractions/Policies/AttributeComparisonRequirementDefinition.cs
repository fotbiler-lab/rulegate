namespace Fotbiler.RuleGate.Abstractions.Policies;

public sealed record AttributeComparisonRequirementDefinition
    : RequirementDefinition
{
    public AttributeComparisonRequirementDefinition(
        AuthorizationAttributeOperand left,
        AuthorizationAttributeOperator @operator,
        AuthorizationAttributeOperand right,
        string? id = null,
        AuthorizationStringComparison stringComparison =
            AuthorizationStringComparison.Ordinal)
        : base(id)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        if (!Enum.IsDefined(@operator))
        {
            throw new ArgumentOutOfRangeException(
                nameof(@operator),
                @operator,
                "The authorization attribute operator is not supported.");
        }

        if (!RequiresRightOperand(@operator))
        {
            throw new ArgumentException(
                "The authorization attribute comparison operator must accept two operands.",
                nameof(@operator));
        }

        if (!Enum.IsDefined(stringComparison))
        {
            throw new ArgumentOutOfRangeException(
                nameof(stringComparison),
                stringComparison,
                "The authorization string comparison is not supported.");
        }

        Left = left;
        Operator = @operator;
        Right = right;
        StringComparison = stringComparison;
    }

    public AuthorizationAttributeOperand Left { get; }

    public AuthorizationAttributeOperator Operator { get; }

    public AuthorizationAttributeOperand Right { get; }

    public AuthorizationStringComparison StringComparison { get; }

    internal static bool RequiresRightOperand(
        AuthorizationAttributeOperator @operator)
    {
        return @operator is not (
            AuthorizationAttributeOperator.IsEmpty or
            AuthorizationAttributeOperator.IsNotEmpty or
            AuthorizationAttributeOperator.Exists or
            AuthorizationAttributeOperator.NotExists or
            AuthorizationAttributeOperator.IsNull or
            AuthorizationAttributeOperator.IsNotNull);
    }
}
