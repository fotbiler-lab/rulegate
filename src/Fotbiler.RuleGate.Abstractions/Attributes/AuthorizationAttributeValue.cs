namespace Fotbiler.RuleGate.Abstractions.Attributes;

public sealed record AuthorizationAttributeValue
{
    private AuthorizationAttributeValue(
        AuthorizationAttributeValueKind kind,
        object? value)
    {
        Kind = kind;
        Value = value;
    }

    public AuthorizationAttributeValueKind Kind { get; }

    public object? Value { get; }

    public static AuthorizationAttributeValue Create(
        object? value)
    {
        return value switch
        {
            null =>
                new AuthorizationAttributeValue(
                    AuthorizationAttributeValueKind.Null,
                    value: null),

            string stringValue =>
                new AuthorizationAttributeValue(
                    AuthorizationAttributeValueKind.String,
                    stringValue),

            bool booleanValue =>
                new AuthorizationAttributeValue(
                    AuthorizationAttributeValueKind.Boolean,
                    booleanValue),

            byte number =>
                CreateNumber(number),

            sbyte number =>
                CreateNumber(number),

            short number =>
                CreateNumber(number),

            ushort number =>
                CreateNumber(number),

            int number =>
                CreateNumber(number),

            uint number =>
                CreateNumber(number),

            long number =>
                CreateNumber(number),

            ulong number =>
                CreateNumber(number),

            decimal number =>
                CreateNumber(number),

            DateTimeOffset dateTimeOffset =>
                new AuthorizationAttributeValue(
                    AuthorizationAttributeValueKind
                        .DateTimeOffset,
                    dateTimeOffset),

            _ =>
                throw new ArgumentException(
                    $"Authorization attribute value type '{value.GetType().FullName}' is not supported.",
                    nameof(value))
        };
    }

    private static AuthorizationAttributeValue
        CreateNumber(
            decimal value)
    {
        return new AuthorizationAttributeValue(
            AuthorizationAttributeValueKind.Number,
            value);
    }
}
