using RuleGate.DocumentApproval.Api.Domain;

namespace RuleGate.DocumentApproval.Sample.Tests;

public sealed class OrganizationScheduleTests
{
    private static readonly OrganizationWorkingDays Weekdays =
        OrganizationWorkingDays.Monday |
        OrganizationWorkingDays.Tuesday |
        OrganizationWorkingDays.Wednesday |
        OrganizationWorkingDays.Thursday |
        OrganizationWorkingDays.Friday;

    [Theory]
    [InlineData("2026-07-30T04:59:00Z", false)]
    [InlineData("2026-07-30T05:00:00Z", true)]
    [InlineData("2026-07-30T14:59:00Z", true)]
    [InlineData("2026-07-30T15:00:00Z", false)]
    public void Records_schedule_uses_Istanbul_08_to_18_half_open_window(
        string timestamp,
        bool expected)
    {
        var schedule = Schedule(startHour: 8, endHour: 18);

        var valid = schedule.TryIsOpen(DateTimeOffset.Parse(timestamp), out var isOpen);

        Assert.True(valid);
        Assert.Equal(expected, isOpen);
    }

    [Theory]
    [InlineData("2026-07-30T02:59:00Z", false)]
    [InlineData("2026-07-30T03:00:00Z", true)]
    [InlineData("2026-07-30T16:59:00Z", true)]
    [InlineData("2026-07-30T17:00:00Z", false)]
    public void Legal_schedule_uses_Istanbul_06_to_20_half_open_window(
        string timestamp,
        bool expected)
    {
        var schedule = Schedule(startHour: 6, endHour: 20);

        var valid = schedule.TryIsOpen(DateTimeOffset.Parse(timestamp), out var isOpen);

        Assert.True(valid);
        Assert.Equal(expected, isOpen);
    }

    [Fact]
    public void Weekday_schedule_denies_weekend()
    {
        var schedule = Schedule(startHour: 8, endHour: 18);

        var valid = schedule.TryIsOpen(
            new DateTimeOffset(2026, 8, 1, 9, 0, 0, TimeSpan.Zero),
            out var isOpen);

        Assert.True(valid);
        Assert.False(isOpen);
    }

    [Theory]
    [InlineData("2026-07-31T20:58:00Z", false)]
    [InlineData("2026-07-31T20:59:00Z", true)]
    [InlineData("2026-08-01T02:59:00Z", true)]
    [InlineData("2026-08-01T03:00:00Z", false)]
    public void Overnight_schedule_uses_the_day_on_which_the_window_starts(
        string timestamp,
        bool expected)
    {
        var schedule = Schedule(startHour: 0, endHour: 6);
        schedule.StartMinute = 24 * 60 - 1;

        var valid = schedule.TryIsOpen(DateTimeOffset.Parse(timestamp), out var isOpen);

        Assert.True(valid);
        Assert.Equal(expected, isOpen);
    }

    [Theory]
    [InlineData("Invalid/Zone", 480, 1080, (int)OrganizationWorkingDays.Monday)]
    [InlineData("Europe/Istanbul", 480, 480, (int)OrganizationWorkingDays.Monday)]
    [InlineData("Europe/Istanbul", -1, 1080, (int)OrganizationWorkingDays.Monday)]
    [InlineData("Europe/Istanbul", 480, 1080, (int)OrganizationWorkingDays.None)]
    public void Invalid_schedule_fails_closed(
        string timeZoneId,
        int startMinute,
        int endMinute,
        int workingDays)
    {
        var schedule = new OrganizationSchedule
        {
            OrganizationId = "invalid",
            TimeZoneId = timeZoneId,
            WorkingDays = (OrganizationWorkingDays)workingDays,
            StartMinute = startMinute,
            EndMinute = endMinute,
        };

        var valid = schedule.TryIsOpen(DateTimeOffset.UtcNow, out var isOpen);

        Assert.False(valid);
        Assert.False(isOpen);
    }

    private static OrganizationSchedule Schedule(int startHour, int endHour)
    {
        return new OrganizationSchedule
        {
            OrganizationId = "test",
            TimeZoneId = "Europe/Istanbul",
            WorkingDays = Weekdays,
            StartMinute = startHour * 60,
            EndMinute = endHour * 60,
        };
    }
}
