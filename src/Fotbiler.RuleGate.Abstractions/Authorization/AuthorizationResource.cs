using Fotbiler.RuleGate.Abstractions.Attributes;

namespace Fotbiler.RuleGate.Abstractions.Authorization;

public sealed class AuthorizationResource
{
    public AuthorizationResource(
        string type,
        string? id = null,
        AuthorizationAttributes? attributes = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(type);

        if (id is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
        }

        Type = type;
        Id = id;

        Attributes = attributes
            ?? AuthorizationAttributes.Empty;
    }

    public string Type { get; }

    public string? Id { get; }

    public AuthorizationAttributes Attributes { get; }
}
