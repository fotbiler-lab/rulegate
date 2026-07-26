using System.Security.Claims;

namespace Fotbiler.RuleGate.AspNetCore.Subjects;

public sealed class RuleGateSubjectOptions
{
    public const string DefaultPermissionClaimType =
        "permission";

    public string SubjectIdClaimType { get; set; } =
        ClaimTypes.NameIdentifier;

    public IList<string> RoleClaimTypes { get; } =
        new List<string>
        {
            ClaimTypes.Role,
        };

    public IList<string> PermissionClaimTypes { get; } =
        new List<string>
        {
            DefaultPermissionClaimType,
        };
}
