using System.Security.Claims;
using Fotbiler.RuleGate.Abstractions.Authorization;
using Fotbiler.RuleGate.AspNetCore.Enrichment;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using RuleGate.DocumentApproval.Api.Authorization;
using RuleGate.DocumentApproval.Api.Data;
using RuleGate.DocumentApproval.Api.Domain;

namespace RuleGate.DocumentApproval.Sample.Tests;

public sealed class ApiRequestContextProviderTests
{
    [Theory]
    [InlineData("sample-manager", "records", "2026-07-30T04:59:00Z", false)]
    [InlineData("sample-manager", "records", "2026-07-30T05:00:00Z", true)]
    [InlineData("sample-legal-approver", "legal", "2026-07-30T03:00:00Z", true)]
    public async Task Read_adds_database_backed_organization_business_hours(
        string username,
        string organizationId,
        string timestamp,
        bool expected)
    {
        await using var database = await CreateDatabaseAsync();
        database.UserProfiles.Add(Profile(username, organizationId));
        database.OrganizationSchedules.Add(
            organizationId == "records"
                ? Schedule(organizationId, startHour: 8, endHour: 18)
                : Schedule(organizationId, startHour: 6, endHour: 20));
        await database.SaveChangesAsync();
        var provider = new ApiRequestContextProvider(database);

        var result = await provider.ProvideAttributesAsync(
            Context(username, "read", DateTimeOffset.Parse(timestamp)));

        Assert.True(result.IsSuccessful);
        Assert.True(result.Attributes.TryGetValue("organizationBusinessHoursOpen", out var value));
        Assert.Equal(expected, Assert.IsType<bool>(value));
        Assert.Equal("Europe/Istanbul", result.Attributes["organizationScheduleTimeZone"]);
    }

    [Fact]
    public async Task Read_fails_closed_when_organization_schedule_is_missing()
    {
        await using var database = await CreateDatabaseAsync();
        database.UserProfiles.Add(Profile("sample-user", "unknown"));
        await database.SaveChangesAsync();
        var provider = new ApiRequestContextProvider(database);

        var result = await provider.ProvideAttributesAsync(
            Context(
                "sample-user",
                "read",
                new DateTimeOffset(2026, 7, 30, 12, 0, 0, TimeSpan.Zero)));

        Assert.False(result.IsSuccessful);
    }

    [Fact]
    public async Task Non_read_action_keeps_trusted_api_channel_without_schedule_lookup()
    {
        await using var database = await CreateDatabaseAsync();
        var provider = new ApiRequestContextProvider(database);

        var result = await provider.ProvideAttributesAsync(
            Context(
                "unmapped-user",
                "create",
                new DateTimeOffset(2026, 7, 30, 12, 0, 0, TimeSpan.Zero)));

        Assert.True(result.IsSuccessful);
        Assert.Equal("api", result.Attributes["requestChannel"]);
        Assert.False(result.Attributes.ContainsKey("organizationBusinessHoursOpen"));
    }

    private static async Task<SampleDbContext> CreateDatabaseAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<SampleDbContext>()
            .UseSqlite(connection)
            .Options;
        var database = new SampleDbContext(options);
        await database.Database.EnsureCreatedAsync();
        return database;
    }

    private static RuleGateAttributeProviderContext Context(
        string username,
        string action,
        DateTimeOffset evaluationTime)
    {
        var principal = new ClaimsPrincipal(
            new ClaimsIdentity(
            [
                new Claim("sub", username),
                new Claim("preferred_username", username),
            ],
            authenticationType: "test"));

        return new RuleGateAttributeProviderContext(
            principal,
            frameworkResource: null,
            new AuthorizationSubject(username),
            new AuthorizationResource("document", "1"),
            action,
            new AuthorizationContext(evaluationTime));
    }

    private static UserProfile Profile(string username, string organizationId)
    {
        return new UserProfile
        {
            Username = username,
            DisplayName = username,
            OrganizationId = organizationId,
            Clearance = "confidential",
        };
    }

    private static OrganizationSchedule Schedule(
        string organizationId,
        int startHour,
        int endHour)
    {
        return new OrganizationSchedule
        {
            OrganizationId = organizationId,
            TimeZoneId = "Europe/Istanbul",
            WorkingDays =
                OrganizationWorkingDays.Monday |
                OrganizationWorkingDays.Tuesday |
                OrganizationWorkingDays.Wednesday |
                OrganizationWorkingDays.Thursday |
                OrganizationWorkingDays.Friday,
            StartMinute = startHour * 60,
            EndMinute = endHour * 60,
        };
    }
}
