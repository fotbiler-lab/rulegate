namespace Fotbiler.RuleGate.Abstractions.Policies;

public sealed record PolicyDefinition
{
    public PolicyDefinition(
        string id,
        string resourceType,
        string action,
        RequirementDefinition requirement)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceType);
        ArgumentException.ThrowIfNullOrWhiteSpace(action);
        ArgumentNullException.ThrowIfNull(requirement);

        Id = id;
        ResourceType = resourceType;
        Action = action;
        Requirement = requirement;
    }

    public string Id { get; }

    public string ResourceType { get; }

    public string Action { get; }

    public RequirementDefinition Requirement { get; }
}
