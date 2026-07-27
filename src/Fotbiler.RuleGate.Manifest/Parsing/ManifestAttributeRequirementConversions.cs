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

            default:
                @operator = default;
                return false;
        }
    }

    internal static bool TryParseValueType(
        string? value,
        out AuthorizationAttributeValueKind kind)
    {
        switch (value)
        {
            case "nullValue":
                kind =
                    AuthorizationAttributeValueKind.Null;
                return true;

            case "string":
                kind =
                    AuthorizationAttributeValueKind.String;
                return true;

            case "boolean":
                kind =
                    AuthorizationAttributeValueKind.Boolean;
                return true;

            case "number":
                kind =
                    AuthorizationAttributeValueKind.Number;
                return true;

            case "dateTimeOffset":
                kind =
                    AuthorizationAttributeValueKind
                        .DateTimeOffset;
                return true;

            default:
                kind = default;
                return false;
        }
    }

    internal static bool IsOperatorSupported(
        AuthorizationAttributeOperator @operator,
        AuthorizationAttributeValueKind valueKind)
    {
        return @operator switch
        {
            AuthorizationAttributeOperator.Equal =>
                true,

            AuthorizationAttributeOperator.NotEqual =>
                true,

            AuthorizationAttributeOperator.GreaterThan or
            AuthorizationAttributeOperator
                .GreaterThanOrEqual or
            AuthorizationAttributeOperator.LessThan or
            AuthorizationAttributeOperator
                .LessThanOrEqual =>
                valueKind is
                    AuthorizationAttributeValueKind.Number or
                    AuthorizationAttributeValueKind
                        .DateTimeOffset,

            _ => false
        };
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
            AuthorizationAttributeValueKind.Null =>
                TryConvertNull(
                    requirement.Value,
                    out value),

            AuthorizationAttributeValueKind.String =>
                TryConvertString(
                    requirement.Value,
                    out value),

            AuthorizationAttributeValueKind.Boolean =>
                TryConvertBoolean(
                    requirement.Value,
                    out value),

            AuthorizationAttributeValueKind.Number =>
                TryConvertNumber(
                    requirement.Value,
                    out value),

            AuthorizationAttributeValueKind
                .DateTimeOffset =>
                TryConvertDateTimeOffset(
                    requirement.Value,
                    out value),

            _ => false
        };
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
}
