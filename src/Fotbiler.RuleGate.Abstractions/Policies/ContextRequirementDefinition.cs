using Fotbiler.RuleGate.Abstractions.Attributes;
using Fotbiler.RuleGate.Abstractions.Authorization;

namespace Fotbiler.RuleGate.Abstractions.Policies;

public sealed record ContextRequirementDefinition
    : RequirementDefinition
{
    public ContextRequirementDefinition(
        AuthorizationContextProperty property,
        AuthorizationAttributeOperator @operator,
        object value,
        string? id = null,
        AuthorizationStringComparison stringComparison =
            AuthorizationStringComparison.Ordinal)
        : base(id)
    {
        if (!Enum.IsDefined(property))
        {
            throw new ArgumentOutOfRangeException(
                nameof(property),
                property,
                "The authorization context property is not supported.");
        }

        if (!Enum.IsDefined(@operator))
        {
            throw new ArgumentOutOfRangeException(
                nameof(@operator),
                @operator,
                "The authorization context operator is not supported.");
        }

        if (!Enum.IsDefined(stringComparison))
        {
            throw new ArgumentOutOfRangeException(
                nameof(stringComparison),
                stringComparison,
                "The authorization string comparison is not supported.");
        }

        var expectedValue =
            AuthorizationAttributeValue.Create(value);

        if (!IsCompatible(
                property,
                @operator,
                expectedValue))
        {
            throw new ArgumentException(
                "The context property, operator, and expected value are not compatible.",
                nameof(value));
        }

        Property = property;
        Operator = @operator;
        ExpectedValue = expectedValue;
        StringComparison = stringComparison;
    }

    public AuthorizationContextProperty Property { get; }

    public string AttributeName =>
        AuthorizationContextAttributeNames.GetName(
            Property);

    public AuthorizationAttributeOperator Operator { get; }

    public AuthorizationAttributeValue ExpectedValue { get; }

    public AuthorizationStringComparison StringComparison { get; }

    internal static bool IsCompatible(
        AuthorizationContextProperty property,
        AuthorizationAttributeOperator @operator,
        AuthorizationAttributeValue expectedValue)
    {
        if (property ==
            AuthorizationContextProperty.TrustedDevice)
        {
            return @operator is
                    AuthorizationAttributeOperator.Equal or
                    AuthorizationAttributeOperator.NotEqual &&
                expectedValue.Kind ==
                    AuthorizationAttributeValueKind.Boolean;
        }

        return @operator switch
        {
            AuthorizationAttributeOperator.Equal or
            AuthorizationAttributeOperator.NotEqual or
            AuthorizationAttributeOperator.Contains or
            AuthorizationAttributeOperator.StartsWith or
            AuthorizationAttributeOperator.EndsWith =>
                expectedValue.Kind ==
                    AuthorizationAttributeValueKind.String,

            AuthorizationAttributeOperator.In or
            AuthorizationAttributeOperator.NotIn =>
                expectedValue.Kind ==
                    AuthorizationAttributeValueKind.Collection &&
                (expectedValue.CollectionElementKind is null or
                 AuthorizationAttributeValueKind.String),

            _ => false
        };
    }
}
