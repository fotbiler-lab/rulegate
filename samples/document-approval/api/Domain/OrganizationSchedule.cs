namespace RuleGate.DocumentApproval.Api.Domain;

[Flags]
public enum OrganizationWorkingDays
{
    None = 0,
    Sunday = 1 << 0,
    Monday = 1 << 1,
    Tuesday = 1 << 2,
    Wednesday = 1 << 3,
    Thursday = 1 << 4,
    Friday = 1 << 5,
    Saturday = 1 << 6,
}

public sealed class OrganizationSchedule
{
    private const int MinutesPerDay = 24 * 60;

    private const OrganizationWorkingDays AllDays =
        OrganizationWorkingDays.Sunday |
        OrganizationWorkingDays.Monday |
        OrganizationWorkingDays.Tuesday |
        OrganizationWorkingDays.Wednesday |
        OrganizationWorkingDays.Thursday |
        OrganizationWorkingDays.Friday |
        OrganizationWorkingDays.Saturday;

    public required string OrganizationId { get; set; }

    public required string TimeZoneId { get; set; }

    public OrganizationWorkingDays WorkingDays { get; set; }

    public int StartMinute { get; set; }

    public int EndMinute { get; set; }

    public bool TryIsOpen(DateTimeOffset utcNow, out bool isOpen)
    {
        isOpen = false;

        if (WorkingDays == OrganizationWorkingDays.None ||
            (WorkingDays & ~AllDays) != OrganizationWorkingDays.None ||
            StartMinute is < 0 or >= MinutesPerDay ||
            EndMinute is < 0 or >= MinutesPerDay ||
            StartMinute == EndMinute)
        {
            return false;
        }

        TimeZoneInfo timeZone;

        try
        {
            timeZone = TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            return false;
        }
        catch (InvalidTimeZoneException)
        {
            return false;
        }

        var localNow = TimeZoneInfo.ConvertTime(utcNow, timeZone);
        var localMinute = (localNow.Hour * 60) + localNow.Minute;
        var localDay = ToWorkingDay(localNow.DayOfWeek);

        if (StartMinute < EndMinute)
        {
            isOpen = Includes(localDay) &&
                     localMinute >= StartMinute &&
                     localMinute < EndMinute;
            return true;
        }

        if (localMinute >= StartMinute)
        {
            isOpen = Includes(localDay);
            return true;
        }

        if (localMinute < EndMinute)
        {
            isOpen = Includes(ToWorkingDay(localNow.AddDays(-1).DayOfWeek));
            return true;
        }

        return true;
    }

    private bool Includes(OrganizationWorkingDays day)
    {
        return (WorkingDays & day) == day;
    }

    private static OrganizationWorkingDays ToWorkingDay(DayOfWeek day)
    {
        return day switch
        {
            DayOfWeek.Sunday => OrganizationWorkingDays.Sunday,
            DayOfWeek.Monday => OrganizationWorkingDays.Monday,
            DayOfWeek.Tuesday => OrganizationWorkingDays.Tuesday,
            DayOfWeek.Wednesday => OrganizationWorkingDays.Wednesday,
            DayOfWeek.Thursday => OrganizationWorkingDays.Thursday,
            DayOfWeek.Friday => OrganizationWorkingDays.Friday,
            DayOfWeek.Saturday => OrganizationWorkingDays.Saturday,
            _ => OrganizationWorkingDays.None,
        };
    }
}
