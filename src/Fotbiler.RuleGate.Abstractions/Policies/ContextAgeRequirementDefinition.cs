using Fotbiler.RuleGate.Abstractions.Authorization;

namespace Fotbiler.RuleGate.Abstractions.Policies;

public sealed record ContextAgeRequirementDefinition
    : RequirementDefinition
{
    public ContextAgeRequirementDefinition(
        AuthorizationContextTimestamp timestamp,
        TimeSpan maximumAge,
        string? id = null)
        : base(id)
    {
        if (!Enum.IsDefined(
                typeof(AuthorizationContextTimestamp),
                timestamp))
        {
            throw new ArgumentOutOfRangeException(
                nameof(timestamp),
                timestamp,
                "The authorization context timestamp is not supported.");
        }

        if (maximumAge <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumAge),
                maximumAge,
                "The maximum context age must be greater than zero.");
        }

        Timestamp = timestamp;
        MaximumAge = maximumAge;
    }

    public AuthorizationContextTimestamp Timestamp { get; }

    public string AttributeName =>
        AuthorizationContextAttributeNames.GetName(
            Timestamp);

    public TimeSpan MaximumAge { get; }
}
