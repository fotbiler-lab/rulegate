namespace Fotbiler.RuleGate.Abstractions.Policies;

public sealed record DateTimeWindowRequirementDefinition
    : RequirementDefinition
{
    public DateTimeWindowRequirementDefinition(
        DateTimeOffset? startsAt = null,
        DateTimeOffset? endsAt = null,
        string? id = null)
        : base(id)
    {
        if (startsAt is null && endsAt is null)
        {
            throw new ArgumentException(
                "A date/time window must define at least one boundary.");
        }

        if (startsAt is not null &&
            endsAt is not null &&
            startsAt >= endsAt)
        {
            throw new ArgumentException(
                "The date/time window start must be earlier than its end.");
        }

        StartsAt = startsAt?.ToUniversalTime();
        EndsAt = endsAt?.ToUniversalTime();
    }

    public DateTimeOffset? StartsAt { get; }

    public DateTimeOffset? EndsAt { get; }
}
