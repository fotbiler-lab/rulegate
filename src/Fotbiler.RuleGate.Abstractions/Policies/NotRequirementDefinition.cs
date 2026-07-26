namespace Fotbiler.RuleGate.Abstractions.Policies;

public sealed record NotRequirementDefinition
    : RequirementDefinition
{
    public NotRequirementDefinition(
        RequirementDefinition requirement,
        string? id = null)
        : base(id)
    {
        ArgumentNullException.ThrowIfNull(requirement);

        Requirement = requirement;
    }

    public RequirementDefinition Requirement { get; }
}
