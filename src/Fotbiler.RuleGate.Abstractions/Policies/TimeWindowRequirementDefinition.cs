namespace Fotbiler.RuleGate.Abstractions.Policies;

public sealed record TimeWindowRequirementDefinition
    : RequirementDefinition
{
    public TimeWindowRequirementDefinition(
        IEnumerable<DayOfWeek> days,
        TimeOnly start,
        TimeOnly end,
        TimeZoneInfo timeZone,
        string? id = null)
        : base(id)
    {
        ArgumentNullException.ThrowIfNull(days);
        ArgumentNullException.ThrowIfNull(timeZone);

        var copiedDays = days.ToArray();

        if (copiedDays.Length == 0)
        {
            throw new ArgumentException(
                "A time window must contain at least one day.",
                nameof(days));
        }

        if (copiedDays.Any(
                static day => !Enum.IsDefined(day)))
        {
            throw new ArgumentOutOfRangeException(
                nameof(days),
                "A time-window day is not supported.");
        }

        if (copiedDays.Distinct().Count() !=
            copiedDays.Length)
        {
            throw new ArgumentException(
                "A time window cannot contain duplicate days.",
                nameof(days));
        }

        if (start == end)
        {
            throw new ArgumentException(
                "A time-window start and end cannot be equal.");
        }

        Days = Array.AsReadOnly(
            copiedDays
                .OrderBy(static day => day)
                .ToArray());

        Start = start;
        End = end;
        TimeZone = timeZone;
    }

    public IReadOnlyList<DayOfWeek> Days { get; }

    public TimeOnly Start { get; }

    public TimeOnly End { get; }

    public TimeZoneInfo TimeZone { get; }

    public bool CrossesMidnight => Start > End;
}
