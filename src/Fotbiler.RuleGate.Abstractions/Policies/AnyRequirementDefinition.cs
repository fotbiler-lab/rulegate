namespace Fotbiler.RuleGate.Abstractions.Policies;

public sealed record AnyRequirementDefinition
    : RequirementDefinition
{
    public AnyRequirementDefinition(
        IEnumerable<RequirementDefinition> requirements,
        string? id = null)
        : base(id)
    {
        ArgumentNullException.ThrowIfNull(requirements);

        var items = requirements.ToArray();

        if (items.Length == 0)
        {
            throw new ArgumentException(
                "An any requirement must contain at least one child requirement.",
                nameof(requirements));
        }

        if (items.Any(static item => item is null))
        {
            throw new ArgumentException(
                "An any requirement cannot contain null child requirements.",
                nameof(requirements));
        }

        Requirements = Array.AsReadOnly(items);
    }

    public IReadOnlyList<RequirementDefinition> Requirements { get; }
}
