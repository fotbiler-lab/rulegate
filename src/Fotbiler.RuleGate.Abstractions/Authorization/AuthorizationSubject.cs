using System.Collections.Frozen;
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

        Roles = (roles ?? [])
            .ToFrozenSet(StringComparer.Ordinal);

        Permissions = (permissions ?? [])
            .ToFrozenSet(StringComparer.Ordinal);

        Attributes = attributes
            ?? AuthorizationAttributes.Empty;
    }

    public string Id { get; }

    public IReadOnlySet<string> Roles { get; }

    public IReadOnlySet<string> Permissions { get; }

    public AuthorizationAttributes Attributes { get; }
}
