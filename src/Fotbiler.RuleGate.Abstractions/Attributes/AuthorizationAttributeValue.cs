using System.Collections;

namespace Fotbiler.RuleGate.Abstractions.Attributes;

public sealed record AuthorizationAttributeValue
{
    public const int MaximumCollectionElementCount =
        256;

    private AuthorizationAttributeValue(
        AuthorizationAttributeValueKind kind,
        object? value,
        AuthorizationAttributeValueKind?
            collectionElementKind = null,
        IReadOnlyList<AuthorizationAttributeValue>?
            collectionItems = null)
    {
        Kind = kind;
        Value = value;
        CollectionElementKind =
            collectionElementKind;
        CollectionItems =
            collectionItems ??
            Array.Empty<AuthorizationAttributeValue>();
    }

    public AuthorizationAttributeValueKind Kind { get; }

    public object? Value { get; }

    public AuthorizationAttributeValueKind?
        CollectionElementKind
    { get; }

    public IReadOnlyList<AuthorizationAttributeValue>
        CollectionItems
    { get; }

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

            IDictionary =>
                throw CreateUnsupportedTypeException(
                    value),

            IEnumerable collection =>
                CreateCollection(collection),

            _ =>
                throw CreateUnsupportedTypeException(
                    value)
        };
    }

    private static AuthorizationAttributeValue
        CreateCollection(
            IEnumerable values)
    {
        var items =
            new List<AuthorizationAttributeValue>();

        AuthorizationAttributeValueKind?
            elementKind = null;

        foreach (var value in values)
        {
            if (items.Count >=
                MaximumCollectionElementCount)
            {
                throw new ArgumentException(
                    $"Authorization attribute collections cannot contain more than {MaximumCollectionElementCount} elements.",
                    nameof(values));
            }

            var item = CreateScalar(value);

            if (item.Kind ==
                AuthorizationAttributeValueKind.Null)
            {
                throw new ArgumentException(
                    "Authorization attribute collections cannot contain null elements.",
                    nameof(values));
            }

            if (elementKind is not null &&
                elementKind != item.Kind)
            {
                throw new ArgumentException(
                    "Authorization attribute collections must contain one supported element type.",
                    nameof(values));
            }

            elementKind = item.Kind;
            items.Add(item);
        }

        var rawValues =
            items
                .Select(
                    static item => item.Value)
                .ToArray();

        return new AuthorizationAttributeValue(
            AuthorizationAttributeValueKind.Collection,
            Array.AsReadOnly(rawValues),
            elementKind,
            items.AsReadOnly());
    }

    private static AuthorizationAttributeValue
        CreateScalar(
            object? value)
    {
        var normalized = Create(value);

        if (normalized.Kind ==
            AuthorizationAttributeValueKind.Collection)
        {
            throw new ArgumentException(
                "Nested authorization attribute collections are not supported.",
                nameof(value));
        }

        return normalized;
    }

    private static AuthorizationAttributeValue
        CreateNumber(
            decimal value)
    {
        return new AuthorizationAttributeValue(
            AuthorizationAttributeValueKind.Number,
            value);
    }

    private static ArgumentException
        CreateUnsupportedTypeException(
            object value)
    {
        return new ArgumentException(
            $"Authorization attribute value type '{value.GetType().FullName}' is not supported.",
            nameof(value));
    }
}
