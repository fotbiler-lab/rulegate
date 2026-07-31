namespace Fotbiler.RuleGate.Abstractions.Policies;

public sealed record TimeWindowRequirementDefinition
    : RequirementDefinition
{
    public TimeWindowRequirementDefinition(
        IEnumerable<DayOfWeek> days,
        TimeSpan start,
        TimeSpan end,
        TimeZoneInfo timeZone,
        string? id = null)
        : base(id)
    {
        ArgumentNullException.ThrowIfNull(days);
        ArgumentNullException.ThrowIfNull(timeZone);

        if (start < TimeSpan.Zero ||
            start >= TimeSpan.FromDays(1))
        {
            throw new ArgumentOutOfRangeException(
                nameof(start),
                "A time-window start must be within one day.");
        }

        if (end < TimeSpan.Zero ||
            end >= TimeSpan.FromDays(1))
        {
            throw new ArgumentOutOfRangeException(
                nameof(end),
                "A time-window end must be within one day.");
        }

        var copiedDays = days.ToArray();

        if (copiedDays.Length == 0)
        {
            throw new ArgumentException(
                "A time window must contain at least one day.",
                nameof(days));
        }

        if (copiedDays.Any(
                static day => !Enum.IsDefined(
                    typeof(DayOfWeek),
                    day)))
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

    public TimeSpan Start { get; }

    public TimeSpan End { get; }

    public TimeZoneInfo TimeZone { get; }

    public bool CrossesMidnight => Start > End;
}
