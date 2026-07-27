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
        string? id = null)
        : base(id)
    {
        if (!Enum.IsDefined(source))
        {
            throw new ArgumentOutOfRangeException(
                nameof(source),
                source,
                "The authorization attribute source is not supported.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (!Enum.IsDefined(@operator))
        {
            throw new ArgumentOutOfRangeException(
                nameof(@operator),
                @operator,
                "The authorization attribute operator is not supported.");
        }

        Source = source;
        Name = name;
        Operator = @operator;
        ExpectedValue =
            AuthorizationAttributeValue.Create(value);
    }

    public AuthorizationAttributeSource Source { get; }

    public string Name { get; }

    public AuthorizationAttributeOperator Operator { get; }

    public AuthorizationAttributeValue ExpectedValue { get; }
}
