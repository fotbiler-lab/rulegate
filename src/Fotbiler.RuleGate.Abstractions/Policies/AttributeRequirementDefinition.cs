using Fotbiler.RuleGate.Abstractions.Attributes;

namespace Fotbiler.RuleGate.Abstractions.Policies;

public sealed record AttributeRequirementDefinition
    : RequirementDefinition
{
    public AttributeRequirementDefinition(
        AuthorizationAttributeSource source,
        string name,
        AuthorizationAttributeOperator @operator,
        object? value,
        string? id = null,
        AuthorizationStringComparison
            stringComparison =
                AuthorizationStringComparison.Ordinal)
        : base(id)
    {
        if (!Enum.IsDefined(
                typeof(AuthorizationAttributeSource),
                source))
        {
            throw new ArgumentOutOfRangeException(
                nameof(source),
                source,
                "The authorization attribute source is not supported.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (!Enum.IsDefined(
                typeof(AuthorizationAttributeOperator),
                @operator))
        {
            throw new ArgumentOutOfRangeException(
                nameof(@operator),
                @operator,
                "The authorization attribute operator is not supported.");
        }

        if (!Enum.IsDefined(
                typeof(AuthorizationStringComparison),
                stringComparison))
        {
            throw new ArgumentOutOfRangeException(
                nameof(stringComparison),
                stringComparison,
                "The authorization string comparison is not supported.");
        }

        Source = source;
        Name = name;
        Operator = @operator;
        StringComparison = stringComparison;
        ExpectedValue =
            AuthorizationAttributeValue.Create(value);
    }

    public AuthorizationAttributeSource Source { get; }

    public string Name { get; }

    public AuthorizationAttributeOperator Operator { get; }

    public AuthorizationStringComparison
        StringComparison
    { get; }

    public AuthorizationAttributeValue ExpectedValue { get; }
}
