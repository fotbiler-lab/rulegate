namespace Fotbiler.RuleGate.Abstractions.Policies;

public abstract record RequirementDefinition
{
    protected RequirementDefinition(string? id = null)
    {
        if (id is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(id);
        }

        Id = id;
    }

    public string? Id { get; }
}
