namespace Fotbiler.RuleGate.Abstractions.Authorization;

public static class AuthorizationContextAttributeNames
{
    public const string AuthenticationMethod =
        "authenticationMethod";

    public const string RequestChannel =
        "requestChannel";

    public const string NetworkZone =
        "networkZone";

    public const string TenantId =
        "tenantId";

    public const string OrganizationId =
        "organizationId";

    public const string TrustedDevice =
        "trustedDevice";

    public const string IdentityType =
        "identityType";

    public const string AuthenticationTime =
        "authenticationTime";

    public const string MultiFactorAuthenticationTime =
        "multiFactorAuthenticationTime";

    public static string GetName(
        AuthorizationContextProperty property)
    {
        return property switch
        {
            AuthorizationContextProperty
                .AuthenticationMethod =>
                AuthenticationMethod,

            AuthorizationContextProperty.RequestChannel =>
                RequestChannel,

            AuthorizationContextProperty.NetworkZone =>
                NetworkZone,

            AuthorizationContextProperty.TenantId =>
                TenantId,

            AuthorizationContextProperty.OrganizationId =>
                OrganizationId,

            AuthorizationContextProperty.TrustedDevice =>
                TrustedDevice,

            AuthorizationContextProperty.IdentityType =>
                IdentityType,

            _ => throw new ArgumentOutOfRangeException(
                nameof(property),
                property,
                "The authorization context property is not supported.")
        };
    }

    public static string GetName(
        AuthorizationContextTimestamp timestamp)
    {
        return timestamp switch
        {
            AuthorizationContextTimestamp
                .AuthenticationTime =>
                AuthenticationTime,

            AuthorizationContextTimestamp
                .MultiFactorAuthenticationTime =>
                MultiFactorAuthenticationTime,

            _ => throw new ArgumentOutOfRangeException(
                nameof(timestamp),
                timestamp,
                "The authorization context timestamp is not supported.")
        };
    }
}
