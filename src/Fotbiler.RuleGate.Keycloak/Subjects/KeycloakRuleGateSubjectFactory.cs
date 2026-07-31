using System.Security.Claims;
using System.Text.Json;
using Fotbiler.RuleGate.Abstractions.Authorization;
using Fotbiler.RuleGate.AspNetCore.Subjects;
using Microsoft.Extensions.Options;

namespace Fotbiler.RuleGate.Keycloak.Subjects;

public sealed class KeycloakRuleGateSubjectFactory
    : IRuleGateSubjectFactory
{
    private readonly bool _requireAuthenticatedPrincipal;

    private readonly bool _includeRealmRoles;

    private readonly string _subjectIdClaimType;

    private readonly string _realmAccessClaimType;

    private readonly string _resourceAccessClaimType;

    private readonly HashSet<string> _clientIds;

    private readonly HashSet<string>
        _permissionClaimTypes;

    public KeycloakRuleGateSubjectFactory(
        IOptions<RuleGateKeycloakSubjectOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var value = options.Value;

        ArgumentNullException.ThrowIfNull(value);

        _requireAuthenticatedPrincipal =
            value.RequireAuthenticatedPrincipal;

        _includeRealmRoles =
            value.IncludeRealmRoles;

        _subjectIdClaimType = ValidateIdentifier(
            value.SubjectIdClaimType,
            nameof(value.SubjectIdClaimType));

        _realmAccessClaimType = ValidateIdentifier(
            value.RealmAccessClaimType,
            nameof(value.RealmAccessClaimType));

        _resourceAccessClaimType = ValidateIdentifier(
            value.ResourceAccessClaimType,
            nameof(value.ResourceAccessClaimType));

        _clientIds = ValidateIdentifiers(
            value.ClientIds,
            nameof(value.ClientIds));

        _permissionClaimTypes =
            ValidateIdentifiers(
                value.PermissionClaimTypes,
                nameof(
                    value.PermissionClaimTypes));
    }

    public AuthorizationSubject Create(
        ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        if (_requireAuthenticatedPrincipal &&
            principal.Identities.All(
                static identity =>
                    !identity.IsAuthenticated))
        {
            throw new InvalidOperationException(
                "The Keycloak claims principal is not authenticated.");
        }

        var roles = new HashSet<string>(
            StringComparer.Ordinal);

        if (_includeRealmRoles)
        {
            AddRealmRoles(
                principal,
                roles);
        }

        if (_clientIds.Count != 0)
        {
            AddClientRoles(
                principal,
                roles);
        }

        return new AuthorizationSubject(
            id: ReadSubjectId(principal),
            roles: roles,
            permissions:
                ReadPermissions(principal));
    }

    private string ReadSubjectId(
        ClaimsPrincipal principal)
    {
        var values = ReadDistinctClaimValues(
            principal,
            _subjectIdClaimType);

        if (values.Count == 0)
        {
            throw new InvalidOperationException(
                $"The Keycloak claims principal does not contain a non-empty '{_subjectIdClaimType}' subject claim.");
        }

        if (values.Count > 1)
        {
            throw new InvalidOperationException(
                $"The Keycloak claims principal contains multiple distinct '{_subjectIdClaimType}' subject claims.");
        }

        return values.Single();
    }

    private void AddRealmRoles(
        ClaimsPrincipal principal,
        ISet<string> roles)
    {
        using var document = ReadStructuredClaim(
            principal,
            _realmAccessClaimType);

        if (document is null)
        {
            return;
        }

        foreach (var role in ReadRoles(
                     document.RootElement,
                     _realmAccessClaimType))
        {
            roles.Add(
                KeycloakRoleNames.RealmRole(role));
        }
    }

    private void AddClientRoles(
        ClaimsPrincipal principal,
        ISet<string> roles)
    {
        using var document = ReadStructuredClaim(
            principal,
            _resourceAccessClaimType);

        if (document is null)
        {
            return;
        }

        if (document.RootElement.ValueKind !=
            JsonValueKind.Object)
        {
            throw MalformedStructuredClaim(
                _resourceAccessClaimType);
        }

        foreach (var clientId in _clientIds)
        {
            if (!document.RootElement.TryGetProperty(
                    clientId,
                    out var clientAccess))
            {
                continue;
            }

            foreach (var role in ReadRoles(
                         clientAccess,
                         $"{_resourceAccessClaimType}.{clientId}"))
            {
                roles.Add(
                    KeycloakRoleNames.ClientRole(
                        clientId,
                        role));
            }
        }
    }

    private HashSet<string> ReadPermissions(
        ClaimsPrincipal principal)
    {
        var permissions = new HashSet<string>(
            StringComparer.Ordinal);

        foreach (var claim in principal.Claims)
        {
            if (!_permissionClaimTypes.Contains(
                    claim.Type))
            {
                continue;
            }

            if (claim.Value.StartsWith(
                    "[",
                    StringComparison.Ordinal))
            {
                AddJsonArrayValues(
                    claim.Value,
                    claim.Type,
                    permissions);
                continue;
            }

            permissions.Add(
                ValidateIdentifier(
                    claim.Value,
                    claim.Type));
        }

        return permissions;
    }

    private static void AddJsonArrayValues(
        string value,
        string claimType,
        ISet<string> values)
    {
        try
        {
            using var document =
                JsonDocument.Parse(value);

            if (document.RootElement.ValueKind !=
                JsonValueKind.Array)
            {
                throw MalformedStructuredClaim(
                    claimType);
            }

            foreach (var element in
                     document.RootElement
                         .EnumerateArray())
            {
                if (element.ValueKind !=
                    JsonValueKind.String)
                {
                    throw MalformedStructuredClaim(
                        claimType);
                }

                values.Add(
                    ValidateIdentifier(
                        element.GetString()!,
                        claimType));
            }
        }
        catch (JsonException exception)
        {
            throw MalformedStructuredClaim(
                claimType,
                exception);
        }
    }

    private static IEnumerable<string> ReadRoles(
        JsonElement access,
        string claimPath)
    {
        if (access.ValueKind !=
            JsonValueKind.Object)
        {
            throw MalformedStructuredClaim(
                claimPath);
        }

        if (!access.TryGetProperty(
                "roles",
                out var roles))
        {
            return [];
        }

        if (roles.ValueKind !=
            JsonValueKind.Array)
        {
            throw MalformedStructuredClaim(
                claimPath);
        }

        var values = new List<string>();

        foreach (var role in roles.EnumerateArray())
        {
            if (role.ValueKind !=
                JsonValueKind.String)
            {
                throw MalformedStructuredClaim(
                    claimPath);
            }

            values.Add(
                ValidateIdentifier(
                    role.GetString()!,
                    claimPath));
        }

        return values;
    }

    private static JsonDocument? ReadStructuredClaim(
        ClaimsPrincipal principal,
        string claimType)
    {
        var values = ReadDistinctClaimValues(
            principal,
            claimType);

        if (values.Count == 0)
        {
            return null;
        }

        if (values.Count > 1)
        {
            throw new InvalidOperationException(
                $"The Keycloak claims principal contains multiple distinct '{claimType}' structured claims.");
        }

        try
        {
            return JsonDocument.Parse(
                values.Single());
        }
        catch (JsonException exception)
        {
            throw MalformedStructuredClaim(
                claimType,
                exception);
        }
    }

    private static HashSet<string>
        ReadDistinctClaimValues(
            ClaimsPrincipal principal,
            string claimType)
    {
        var values = new HashSet<string>(
            StringComparer.Ordinal);

        foreach (var claim in principal.Claims)
        {
            if (!string.Equals(
                    claim.Type,
                    claimType,
                    StringComparison.Ordinal))
            {
                continue;
            }

            values.Add(
                ValidateIdentifier(
                    claim.Value,
                    claimType));
        }

        return values;
    }

    private static HashSet<string>
        ValidateIdentifiers(
            IEnumerable<string> values,
            string optionName)
    {
        ArgumentNullException.ThrowIfNull(values);

        return new HashSet<string>(
            values.Select(
                value => ValidateIdentifier(
                    value,
                    optionName)),
            StringComparer.Ordinal);
    }

    private static string ValidateIdentifier(
        string value,
        string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            value,
            parameterName);

        if (!string.Equals(
                value,
                value.Trim(),
                StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "A Keycloak identifier cannot have leading or trailing whitespace.",
                parameterName);
        }

        return value;
    }

    private static InvalidOperationException
        MalformedStructuredClaim(
            string claimType,
            Exception? innerException = null)
    {
        return new InvalidOperationException(
            $"The Keycloak '{claimType}' claim is malformed.",
            innerException);
    }
}
