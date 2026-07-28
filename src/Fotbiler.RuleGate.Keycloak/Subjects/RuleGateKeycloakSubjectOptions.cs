namespace Fotbiler.RuleGate.Keycloak.Subjects;

public sealed class RuleGateKeycloakSubjectOptions
{
    public const string DefaultSubjectIdClaimType =
        "sub";

    public const string DefaultRealmAccessClaimType =
        "realm_access";

    public const string DefaultResourceAccessClaimType =
        "resource_access";

    public const string DefaultPermissionClaimType =
        "permission";

    public bool RequireAuthenticatedPrincipal { get; set; } =
        true;

    public bool IncludeRealmRoles { get; set; } =
        true;

    public string SubjectIdClaimType { get; set; } =
        DefaultSubjectIdClaimType;

    public string RealmAccessClaimType { get; set; } =
        DefaultRealmAccessClaimType;

    public string ResourceAccessClaimType { get; set; } =
        DefaultResourceAccessClaimType;

    public IList<string> ClientIds { get; } =
        new List<string>();

    public IList<string> PermissionClaimTypes { get; } =
        new List<string>
        {
            DefaultPermissionClaimType,
        };
}
