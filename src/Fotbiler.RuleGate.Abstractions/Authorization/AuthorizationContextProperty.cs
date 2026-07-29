namespace Fotbiler.RuleGate.Abstractions.Authorization;

public enum AuthorizationContextProperty
{
    AuthenticationMethod = 0,
    RequestChannel = 1,
    NetworkZone = 2,
    TenantId = 3,
    OrganizationId = 4,
    TrustedDevice = 5,
    IdentityType = 6
}
