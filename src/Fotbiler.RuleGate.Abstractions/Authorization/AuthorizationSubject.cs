using Fotbiler.RuleGate.Abstractions.Attributes;

namespace Fotbiler.RuleGate.Abstractions.Authorization;

public sealed class AuthorizationSubject
{
    public AuthorizationSubject(
        string id,
        IEnumerable<string>? roles = null,
        IEnumerable<string>? permissions = null,
        AuthorizationAttributes? attributes = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        Id = id;

        Roles = Array.AsReadOnly(
            (roles ?? [])
                .Distinct(StringComparer.Ordinal)
                .ToArray());

        Permissions = Array.AsReadOnly(
            (permissions ?? [])
                .Distinct(StringComparer.Ordinal)
                .ToArray());

        Attributes = attributes
            ?? AuthorizationAttributes.Empty;
    }

    public string Id { get; }

    public IReadOnlyCollection<string> Roles { get; }

    public IReadOnlyCollection<string> Permissions { get; }

    public AuthorizationAttributes Attributes { get; }
}
