namespace Fotbiler.RuleGate.Abstractions.Policies;

public sealed record PermissionRequirementDefinition
    : RequirementDefinition
{
    public PermissionRequirementDefinition(
        string permission,
        string? id = null)
        : base(id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(permission);

        Permission = permission;
    }

    public string Permission { get; }
}
