using System.Collections;
using System.Globalization;
using Fotbiler.RuleGate.Abstractions.Attributes;
using Fotbiler.RuleGate.Abstractions.Policies;
using Fotbiler.RuleGate.Manifest.Models;

namespace Fotbiler.RuleGate.Manifest.Parsing;

internal static class
    ManifestAttributeRequirementConversions
{
    private static readonly string[]
        OffsetDateTimeFormats =
        [
            "yyyy-MM-dd'T'HH:mm:sszzz",
            "yyyy-MM-dd'T'HH:mm:ss.FFFFFFFzzz"
        ];

    private static readonly string[]
        UniversalDateTimeFormats =
        [
            "yyyy-MM-dd'T'HH:mm:ss'Z'",
            "yyyy-MM-dd'T'HH:mm:ss.FFFFFFF'Z'"
        ];

    internal static bool TryParseSource(
        string? value,
        out AuthorizationAttributeSource source)
    {
        switch (value)
        {
            case "subject":
                source =
                    AuthorizationAttributeSource.Subject;
                return true;

            case "resource":
                source =
                    AuthorizationAttributeSource.Resource;
                return true;

            case "context":
                source =
                    AuthorizationAttributeSource.Context;
                return true;

            default:
                source = default;
                return false;
        }
    }

    internal static bool TryParseOperator(
        string? value,
        out AuthorizationAttributeOperator @operator)
    {
        switch (value)
        {
            case "equal":
                @operator =
                    AuthorizationAttributeOperator.Equal;
                return true;

            case "notEqual":
                @operator =
                    AuthorizationAttributeOperator.NotEqual;
                return true;

            case "greaterThan":
                @operator =
                    AuthorizationAttributeOperator
                        .GreaterThan;
                return true;

            case "greaterThanOrEqual":
                @operator =
                    AuthorizationAttributeOperator
                        .GreaterThanOrEqual;
                return true;

            case "lessThan":
                @operator =
                    AuthorizationAttributeOperator.LessThan;
                return true;

            case "lessThanOrEqual":
                @operator =
                    AuthorizationAttributeOperator
                        .LessThanOrEqual;
                return true;

            case "contains":
                @operator =
                    AuthorizationAttributeOperator.Contains;
                return true;

            case "startsWith":
                @operator =
                    AuthorizationAttributeOperator.StartsWith;
                return true;

            case "endsWith":
                @operator =
                    AuthorizationAttributeOperator.EndsWith;
                return true;

            case "containsAny":
                @operator =
                    AuthorizationAttributeOperator.ContainsAny;
                return true;

            case "containsAll":
                @operator =
                    AuthorizationAttributeOperator.ContainsAll;
                return true;

            case "in":
                @operator =
                    AuthorizationAttributeOperator.In;
                return true;

            case "notIn":
                @operator =
                    AuthorizationAttributeOperator.NotIn;
                return true;

            case "intersects":
                @operator =
                    AuthorizationAttributeOperator.Intersects;
                return true;

            case "isEmpty":
                @operator =
                    AuthorizationAttributeOperator.IsEmpty;
                return true;

            case "isNotEmpty":
                @operator =
                    AuthorizationAttributeOperator.IsNotEmpty;
                return true;

            case "exists":
                @operator =
                    AuthorizationAttributeOperator.Exists;
                return true;

            case "notExists":
                @operator =
                    AuthorizationAttributeOperator.NotExists;
                return true;

            case "isNull":
                @operator =
                    AuthorizationAttributeOperator.IsNull;
                return true;

            case "isNotNull":
                @operator =
                    AuthorizationAttributeOperator.IsNotNull;
                return true;

            default:
                @operator = default;
                return false;
        }
    }

    internal static bool TryParseValueType(
        string? value,
        out ManifestAttributeValueType valueType)
    {
        switch (value)
        {
            case "nullValue":
                valueType =
                    ManifestAttributeValueType.Null;
                return true;

            case "string":
                valueType =
                    ManifestAttributeValueType.String;
                return true;

            case "boolean":
                valueType =
                    ManifestAttributeValueType.Boolean;
                return true;

            case "number":
                valueType =
                    ManifestAttributeValueType.Number;
                return true;

            case "dateTimeOffset":
                valueType =
                    ManifestAttributeValueType
                        .DateTimeOffset;
                return true;

            case "stringCollection":
                valueType =
                    ManifestAttributeValueType
                        .StringCollection;
                return true;

            case "booleanCollection":
                valueType =
                    ManifestAttributeValueType
                        .BooleanCollection;
                return true;

            case "numberCollection":
                valueType =
                    ManifestAttributeValueType
                        .NumberCollection;
                return true;

            case "dateTimeOffsetCollection":
                valueType =
                    ManifestAttributeValueType
                        .DateTimeOffsetCollection;
                return true;

            default:
                valueType = default;
                return false;
        }
    }

    internal static bool TryParseStringComparison(
        string? value,
        out AuthorizationStringComparison comparison)
    {
        switch (value)
        {
            case null:
            case "ordinal":
                comparison =
                    AuthorizationStringComparison.Ordinal;
                return true;

            case "ordinalIgnoreCase":
                comparison =
                    AuthorizationStringComparison
                        .OrdinalIgnoreCase;
                return true;

            default:
                comparison = default;
                return false;
        }
    }

    internal static bool OperatorRequiresValue(
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

    internal static bool IsOperatorSupported(
        AuthorizationAttributeOperator @operator,
        ManifestAttributeValueType valueType)
    {
        return @operator switch
        {
            AuthorizationAttributeOperator.Equal =>
                IsScalar(valueType),

            AuthorizationAttributeOperator.NotEqual =>
                IsScalar(valueType),

            AuthorizationAttributeOperator.GreaterThan or
            AuthorizationAttributeOperator
                .GreaterThanOrEqual or
            AuthorizationAttributeOperator.LessThan or
            AuthorizationAttributeOperator
                .LessThanOrEqual =>
                valueType is
                    ManifestAttributeValueType.Number or
                    ManifestAttributeValueType
                        .DateTimeOffset,

            AuthorizationAttributeOperator.Contains =>
                valueType is not (
                    ManifestAttributeValueType.Null or
                    ManifestAttributeValueType
                        .StringCollection or
                    ManifestAttributeValueType
                        .BooleanCollection or
                    ManifestAttributeValueType
                        .NumberCollection or
                    ManifestAttributeValueType
                        .DateTimeOffsetCollection),

            AuthorizationAttributeOperator.StartsWith or
            AuthorizationAttributeOperator.EndsWith =>
                valueType ==
                    ManifestAttributeValueType.String,

            AuthorizationAttributeOperator.ContainsAny or
            AuthorizationAttributeOperator.ContainsAll or
            AuthorizationAttributeOperator.In or
            AuthorizationAttributeOperator.NotIn or
            AuthorizationAttributeOperator.Intersects =>
                IsCollection(valueType),

            AuthorizationAttributeOperator.IsEmpty or
            AuthorizationAttributeOperator.IsNotEmpty or
            AuthorizationAttributeOperator.Exists or
            AuthorizationAttributeOperator.NotExists or
            AuthorizationAttributeOperator.IsNull or
            AuthorizationAttributeOperator.IsNotNull =>
                true,

            _ => false
        };
    }

    internal static bool SupportsStringComparison(
        AuthorizationAttributeOperator @operator,
        ManifestAttributeValueType valueType)
    {
        if (valueType is not (
            ManifestAttributeValueType.String or
            ManifestAttributeValueType.StringCollection))
        {
            return false;
        }

        return @operator is
            AuthorizationAttributeOperator.Equal or
            AuthorizationAttributeOperator.NotEqual or
            AuthorizationAttributeOperator.Contains or
            AuthorizationAttributeOperator.StartsWith or
            AuthorizationAttributeOperator.EndsWith or
            AuthorizationAttributeOperator.ContainsAny or
            AuthorizationAttributeOperator.ContainsAll or
            AuthorizationAttributeOperator.In or
            AuthorizationAttributeOperator.NotIn or
            AuthorizationAttributeOperator.Intersects;
    }

    internal static bool TryConvertValue(
        ManifestAttributeRequirement requirement,
        out object? value)
    {
        ArgumentNullException.ThrowIfNull(requirement);

        value = null;

        if (!requirement.HasValue ||
            !TryParseValueType(
                requirement.ValueType,
                out var kind))
        {
            return false;
        }

        return kind switch
        {
            ManifestAttributeValueType.Null =>
                TryConvertNull(
                    requirement.Value,
                    out value),

            ManifestAttributeValueType.String =>
                TryConvertString(
                    requirement.Value,
                    out value),

            ManifestAttributeValueType.Boolean =>
                TryConvertBoolean(
                    requirement.Value,
                    out value),

            ManifestAttributeValueType.Number =>
                TryConvertNumber(
                    requirement.Value,
                    out value),

            ManifestAttributeValueType
                .DateTimeOffset =>
                TryConvertDateTimeOffset(
                    requirement.Value,
                    out value),

            ManifestAttributeValueType.StringCollection =>
                TryConvertCollection(
                    requirement.Value,
                    TryConvertString,
                    out value),

            ManifestAttributeValueType.BooleanCollection =>
                TryConvertCollection(
                    requirement.Value,
                    TryConvertBoolean,
                    out value),

            ManifestAttributeValueType.NumberCollection =>
                TryConvertCollection(
                    requirement.Value,
                    TryConvertNumber,
                    out value),

            ManifestAttributeValueType
                .DateTimeOffsetCollection =>
                TryConvertCollection(
                    requirement.Value,
                    TryConvertDateTimeOffset,
                    out value),

            _ => false
        };
    }

    private static bool TryConvertCollection(
        object? rawValue,
        TryConvertScalar tryConvertScalar,
        out object? value)
    {
        if (rawValue is string or IDictionary ||
            rawValue is not IEnumerable values)
        {
            value = null;
            return false;
        }

        var converted = new List<object?>();

        foreach (var item in values)
        {
            if (converted.Count >=
                    AuthorizationAttributeValue
                        .MaximumCollectionElementCount ||
                !tryConvertScalar(
                    item,
                    out var convertedItem))
            {
                value = null;
                return false;
            }

            converted.Add(convertedItem);
        }

        value = converted.ToArray();
        return true;
    }

    private static bool IsScalar(
        ManifestAttributeValueType valueType)
    {
        return !IsCollection(valueType);
    }

    private static bool IsCollection(
        ManifestAttributeValueType valueType)
    {
        return valueType is
            ManifestAttributeValueType.StringCollection or
            ManifestAttributeValueType.BooleanCollection or
            ManifestAttributeValueType.NumberCollection or
            ManifestAttributeValueType
                .DateTimeOffsetCollection;
    }

    private static bool TryConvertNull(
        object? rawValue,
        out object? value)
    {
        value = null;
        return rawValue is null;
    }

    private static bool TryConvertString(
        object? rawValue,
        out object? value)
    {
        if (rawValue is string stringValue)
        {
            value = stringValue;
            return true;
        }

        value = null;
        return false;
    }

    private static bool TryConvertBoolean(
        object? rawValue,
        out object? value)
    {
        if (rawValue is bool booleanValue)
        {
            value = booleanValue;
            return true;
        }

        if (rawValue is string text)
        {
            switch (text)
            {
                case "true":
                    value = true;
                    return true;

                case "false":
                    value = false;
                    return true;
            }
        }

        value = null;
        return false;
    }

    private static bool TryConvertNumber(
        object? rawValue,
        out object? value)
    {
        if (rawValue is string text)
        {
            if (!HasCanonicalWhitespace(text) ||
                !decimal.TryParse(
                    text,
                    NumberStyles.AllowLeadingSign |
                    NumberStyles.AllowDecimalPoint,
                    CultureInfo.InvariantCulture,
                    out var parsedNumber))
            {
                value = null;
                return false;
            }

            value = parsedNumber;
            return true;
        }

        try
        {
            var normalized =
                AuthorizationAttributeValue.Create(
                    rawValue);

            if (normalized.Kind !=
                AuthorizationAttributeValueKind.Number)
            {
                value = null;
                return false;
            }

            value = normalized.Value;
            return true;
        }
        catch (ArgumentException)
        {
            value = null;
            return false;
        }
    }

    private static bool TryConvertDateTimeOffset(
        object? rawValue,
        out object? value)
    {
        if (rawValue is DateTimeOffset
            dateTimeOffset)
        {
            value = dateTimeOffset;
            return true;
        }

        if (rawValue is string text &&
            HasCanonicalWhitespace(text) &&
            TryParseDateTimeOffsetText(
                text,
                out var parsedDateTimeOffset))
        {
            value = parsedDateTimeOffset;
            return true;
        }

        value = null;
        return false;
    }

    private static bool TryParseDateTimeOffsetText(
        string value,
        out DateTimeOffset result)
    {
        if (value.EndsWith(
                "Z",
                StringComparison.Ordinal))
        {
            return DateTimeOffset.TryParseExact(
                value,
                UniversalDateTimeFormats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal |
                DateTimeStyles.AdjustToUniversal,
                out result);
        }

        return DateTimeOffset.TryParseExact(
            value,
            OffsetDateTimeFormats,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out result);
    }

    private static bool HasCanonicalWhitespace(
        string value)
    {
        return string.Equals(
            value,
            value.Trim(),
            StringComparison.Ordinal);
    }

    private delegate bool TryConvertScalar(
        object? rawValue,
        out object? value);
}
