using System.Globalization;
using Fotbiler.RuleGate.Abstractions.Authorization;

namespace Fotbiler.RuleGate.Manifest.Parsing;

internal static class ManifestTimeContextConversions
{
    internal static bool TryParseDay(
        string? value,
        out DayOfWeek day)
    {
        switch (value)
        {
            case "sunday":
                day = DayOfWeek.Sunday;
                return true;
            case "monday":
                day = DayOfWeek.Monday;
                return true;
            case "tuesday":
                day = DayOfWeek.Tuesday;
                return true;
            case "wednesday":
                day = DayOfWeek.Wednesday;
                return true;
            case "thursday":
                day = DayOfWeek.Thursday;
                return true;
            case "friday":
                day = DayOfWeek.Friday;
                return true;
            case "saturday":
                day = DayOfWeek.Saturday;
                return true;
            default:
                day = default;
                return false;
        }
    }

    internal static bool TryParseTime(
        string? value,
        out TimeSpan time)
    {
        return TimeSpan.TryParseExact(
            value,
            @"hh\:mm",
            CultureInfo.InvariantCulture,
            out time);
    }

    internal static bool TryParseTimeZone(
        string? value,
        out TimeZoneInfo? timeZone)
    {
        timeZone = null;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        try
        {
            timeZone =
                TimeZoneInfo.FindSystemTimeZoneById(value);
            return true;
        }
        catch (TimeZoneNotFoundException)
        {
            return false;
        }
        catch (InvalidTimeZoneException)
        {
            return false;
        }
    }

    internal static bool TryParseDateTimeOffset(
        string? value,
        out DateTimeOffset dateTimeOffset)
    {
        var parsed =
            ManifestAttributeRequirementConversions.TryConvertValue(
                "dateTimeOffset",
                value,
                value is not null,
                out var converted);

        dateTimeOffset = parsed
            ? (DateTimeOffset)converted!
            : default;

        return parsed;
    }

    internal static bool TryParseMaximumAge(
        string? value,
        out TimeSpan maximumAge)
    {
        return TimeSpan.TryParseExact(
                   value,
                   "c",
                   CultureInfo.InvariantCulture,
                   out maximumAge) &&
               maximumAge > TimeSpan.Zero;
    }

    internal static bool TryParseContextTimestamp(
        string? value,
        out AuthorizationContextTimestamp timestamp)
    {
        switch (value)
        {
            case "authentication":
                timestamp =
                    AuthorizationContextTimestamp.AuthenticationTime;
                return true;
            case "mfa":
                timestamp =
                    AuthorizationContextTimestamp
                        .MultiFactorAuthenticationTime;
                return true;
            default:
                timestamp = default;
                return false;
        }
    }

    internal static bool TryParseContextProperty(
        string? value,
        out AuthorizationContextProperty property)
    {
        switch (value)
        {
            case "authenticationMethod":
                property =
                    AuthorizationContextProperty.AuthenticationMethod;
                return true;
            case "requestChannel":
                property =
                    AuthorizationContextProperty.RequestChannel;
                return true;
            case "networkZone":
                property =
                    AuthorizationContextProperty.NetworkZone;
                return true;
            case "tenantId":
                property =
                    AuthorizationContextProperty.TenantId;
                return true;
            case "organizationId":
                property =
                    AuthorizationContextProperty.OrganizationId;
                return true;
            case "trustedDevice":
                property =
                    AuthorizationContextProperty.TrustedDevice;
                return true;
            case "identityType":
                property =
                    AuthorizationContextProperty.IdentityType;
                return true;
            default:
                property = default;
                return false;
        }
    }
}
