using System.Security.Claims;
using Fotbiler.RuleGate.Abstractions.Authorization;
using Microsoft.Extensions.Options;

namespace Fotbiler.RuleGate.AspNetCore.Subjects;

public sealed class ClaimsPrincipalRuleGateSubjectFactory
    : IRuleGateSubjectFactory
{
    private readonly string _subjectIdClaimType;

    private readonly HashSet<string>
        _roleClaimTypes;

    private readonly HashSet<string>
        _permissionClaimTypes;

    public ClaimsPrincipalRuleGateSubjectFactory(
        IOptions<RuleGateSubjectOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var value = options.Value;

        ArgumentNullException.ThrowIfNull(value);

        _subjectIdClaimType = ValidateClaimType(
            value.SubjectIdClaimType,
            nameof(
                RuleGateSubjectOptions
                    .SubjectIdClaimType));

        _roleClaimTypes = ValidateClaimTypes(
            value.RoleClaimTypes,
            nameof(
                RuleGateSubjectOptions
                    .RoleClaimTypes));

        _permissionClaimTypes = ValidateClaimTypes(
            value.PermissionClaimTypes,
            nameof(
                RuleGateSubjectOptions
                    .PermissionClaimTypes));
    }

    public AuthorizationSubject Create(
        ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        var subjectId = ReadSubjectId(principal);

        var roles = ReadClaimValues(
            principal,
            _roleClaimTypes);

        var permissions = ReadClaimValues(
            principal,
            _permissionClaimTypes);

        return new AuthorizationSubject(
            id: subjectId,
            roles: roles,
            permissions: permissions);
    }

    private string ReadSubjectId(
        ClaimsPrincipal principal)
    {
        var subjectIds =
            new HashSet<string>(
                StringComparer.Ordinal);

        foreach (var claim in principal.Claims)
        {
            if (!string.Equals(
                    claim.Type,
                    _subjectIdClaimType,
                    StringComparison.Ordinal) ||
                string.IsNullOrWhiteSpace(
                    claim.Value))
            {
                continue;
            }

            subjectIds.Add(claim.Value);
        }

        if (subjectIds.Count == 0)
        {
            throw new InvalidOperationException(
                $"The claims principal does not contain a non-empty subject identifier claim of type '{_subjectIdClaimType}'.");
        }

        if (subjectIds.Count > 1)
        {
            throw new InvalidOperationException(
                $"The claims principal contains multiple distinct subject identifier values for claim type '{_subjectIdClaimType}'.");
        }

        return subjectIds.Single();
    }

    private static HashSet<string> ReadClaimValues(
        ClaimsPrincipal principal,
        HashSet<string> claimTypes)
    {
        var values =
            new HashSet<string>(
                StringComparer.Ordinal);

        foreach (var claim in principal.Claims)
        {
            if (!claimTypes.Contains(claim.Type) ||
                string.IsNullOrWhiteSpace(
                    claim.Value))
            {
                continue;
            }

            values.Add(claim.Value);
        }

        return values;
    }

    private static string ValidateClaimType(
        string claimType,
        string optionName)
    {
        if (string.IsNullOrWhiteSpace(claimType))
        {
            throw new ArgumentException(
                "A claim type cannot be null, empty, or whitespace.",
                optionName);
        }

        return claimType;
    }

    private static HashSet<string>
        ValidateClaimTypes(
            IEnumerable<string> claimTypes,
            string optionName)
    {
        ArgumentNullException.ThrowIfNull(
            claimTypes);

        var validatedClaimTypes =
            new HashSet<string>(
                StringComparer.Ordinal);

        foreach (var claimType in claimTypes)
        {
            validatedClaimTypes.Add(
                ValidateClaimType(
                    claimType,
                    optionName));
        }

        return validatedClaimTypes;
    }
}
