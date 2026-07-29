using Fotbiler.RuleGate.Abstractions.Attributes;

namespace Fotbiler.RuleGate.Abstractions.Policies;

public sealed record AuthorizationAttributeOperand
{
    private AuthorizationAttributeOperand(
        AuthorizationAttributeOperandKind kind,
        string? name,
        AuthorizationAttributeValue? literalValue)
    {
        Kind = kind;
        Name = name;
        LiteralValue = literalValue;
    }

    public AuthorizationAttributeOperandKind Kind { get; }

    public string? Name { get; }

    public AuthorizationAttributeValue? LiteralValue { get; }

    public bool IsLiteral =>
        Kind == AuthorizationAttributeOperandKind.Literal;

    public static AuthorizationAttributeOperand Literal(
        object? value)
    {
        return new AuthorizationAttributeOperand(
            AuthorizationAttributeOperandKind.Literal,
            name: null,
            AuthorizationAttributeValue.Create(value));
    }

    public static AuthorizationAttributeOperand Attribute(
        AuthorizationAttributeSource source,
        string name)
    {
        if (!Enum.IsDefined(source))
        {
            throw new ArgumentOutOfRangeException(
                nameof(source),
                source,
                "The authorization attribute source is not supported.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new AuthorizationAttributeOperand(
            source switch
            {
                AuthorizationAttributeSource.Subject =>
                    AuthorizationAttributeOperandKind.Subject,

                AuthorizationAttributeSource.Resource =>
                    AuthorizationAttributeOperandKind.Resource,

                AuthorizationAttributeSource.Context =>
                    AuthorizationAttributeOperandKind.Context,

                _ => throw new ArgumentOutOfRangeException(
                    nameof(source),
                    source,
                    "The authorization attribute source is not supported.")
            },
            name,
            literalValue: null);
    }

    public static AuthorizationAttributeOperand Subject(
        string name)
    {
        return Attribute(
            AuthorizationAttributeSource.Subject,
            name);
    }

    public static AuthorizationAttributeOperand Resource(
        string name)
    {
        return Attribute(
            AuthorizationAttributeSource.Resource,
            name);
    }

    public static AuthorizationAttributeOperand Context(
        string name)
    {
        return Attribute(
            AuthorizationAttributeSource.Context,
            name);
    }
}
