using System.Text;

namespace Fotbiler.RuleGate.Keycloak;

public static class KeycloakRoleNames
{
    private const string RealmRolePrefix =
        "keycloak:realm:";

    private const string ClientRolePrefix =
        "keycloak:client:";

    public static string RealmRole(
        string role)
    {
        return RealmRolePrefix
            + EncodeComponent(role);
    }

    public static string ClientRole(
        string clientId,
        string role)
    {
        return ClientRolePrefix
            + EncodeComponent(clientId)
            + ":"
            + EncodeComponent(role);
    }

    public static string EncodeComponent(
        string value)
    {
        ValidateIdentifier(value);

        var bytes = Encoding.UTF8.GetBytes(value);
        var builder = new StringBuilder(
            bytes.Length);

        foreach (var valueByte in bytes)
        {
            if (IsUnreserved(valueByte))
            {
                builder.Append(
                    (char)valueByte);
            }
            else
            {
                builder.Append('%');
                builder.Append(
                    valueByte.ToString("X2"));
            }
        }

        return builder.ToString();
    }

    private static void ValidateIdentifier(
        string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            value);

        if (!string.Equals(
                value,
                value.Trim(),
                StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "A Keycloak role-name component cannot have leading or trailing whitespace.",
                nameof(value));
        }
    }

    private static bool IsUnreserved(
        byte value)
    {
        return value is
            >= (byte)'A' and <= (byte)'Z'
            or >= (byte)'a' and <= (byte)'z'
            or >= (byte)'0' and <= (byte)'9'
            or (byte)'-'
            or (byte)'.'
            or (byte)'_'
            or (byte)'~';
    }
}
