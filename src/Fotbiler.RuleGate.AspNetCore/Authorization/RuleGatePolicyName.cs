using System.Diagnostics.CodeAnalysis;

namespace Fotbiler.RuleGate.AspNetCore.Authorization;

public sealed class RuleGatePolicyName
{
    public const string Prefix = "RuleGate";

    public const char Separator = ':';

    public RuleGatePolicyName(
        string resourceType,
        string action)
    {
        ValidateSegment(
            resourceType,
            nameof(resourceType));

        ValidateSegment(
            action,
            nameof(action));

        ResourceType = resourceType;
        Action = action;
    }

    public string ResourceType { get; }

    public string Action { get; }

    public static bool TryParse(
        string? value,
        [NotNullWhen(true)]
        out RuleGatePolicyName? policyName)
    {
        policyName = null;

        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        var segments =
            value.Split(Separator);

        if (segments.Length != 3)
        {
            return false;
        }

        if (!string.Equals(
                segments[0],
                Prefix,
                StringComparison.Ordinal))
        {
            return false;
        }

        if (!IsValidSegment(segments[1])
            || !IsValidSegment(segments[2]))
        {
            return false;
        }

        policyName =
            new RuleGatePolicyName(
                resourceType: segments[1],
                action: segments[2]);

        return true;
    }

    public override string ToString()
    {
        return string.Concat(
            Prefix,
            Separator,
            ResourceType,
            Separator,
            Action);
    }

    internal static bool HasPrefix(
        string? value)
    {
        return value?.StartsWith(
            string.Concat(
                Prefix,
                Separator),
            StringComparison.Ordinal) == true;
    }

    private static bool IsValidSegment(
        string value)
    {
        return value.Length > 0
            && !value.Any(char.IsWhiteSpace)
            && !value.Contains(Separator);
    }

    private static void ValidateSegment(
        string value,
        string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            value,
            parameterName);

        if (IsValidSegment(value))
        {
            return;
        }

        throw new ArgumentException(
            "RuleGate policy-name segments cannot contain whitespace or the ':' separator.",
            parameterName);
    }
}
