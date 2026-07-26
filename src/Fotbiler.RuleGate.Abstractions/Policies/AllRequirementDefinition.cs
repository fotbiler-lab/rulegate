namespace Fotbiler.RuleGate.Abstractions.Policies;

public sealed record AllRequirementDefinition
    : RequirementDefinition
{
    public AllRequirementDefinition(
        IEnumerable<RequirementDefinition> requirements,
        string? id = null)
        : base(id)
    {
        ArgumentNullException.ThrowIfNull(requirements);

        var items = requirements.ToArray();

        if (items.Length == 0)
        {
            throw new ArgumentException(
                "An all requirement must contain at least one child requirement.",
                nameof(requirements));
        }

        if (items.Any(static item => item is null))
        {
            throw new ArgumentException(
                "An all requirement cannot contain null child requirements.",
                nameof(requirements));
        }

        Requirements = Array.AsReadOnly(items);
    }

    public IReadOnlyList<RequirementDefinition> Requirements { get; }
}
