namespace Fotbiler.RuleGate.Abstractions.Policies;

public sealed record RoleRequirementDefinition
    : RequirementDefinition
{
    public RoleRequirementDefinition(
        string role,
        string? id = null)
        : base(id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(role);

        Role = role;
    }

    public string Role { get; }
}
