using Fotbiler.RuleGate.Abstractions.Authorization;
using Fotbiler.RuleGate.Abstractions.Policies;

namespace Fotbiler.RuleGate.Abstractions.Tests;

public sealed class TimeContextRequirementDefinitionTests
{
    [Fact]
    public void Time_window_preserves_explicit_schedule()
    {
        var requirement = new TimeWindowRequirementDefinition(
            [DayOfWeek.Friday, DayOfWeek.Monday],
            new TimeOnly(8, 0),
            new TimeOnly(18, 0),
            TimeZoneInfo.Utc,
            "business-hours");

        Assert.Equal(
            [DayOfWeek.Monday, DayOfWeek.Friday],
            requirement.Days);
        Assert.Equal(new TimeOnly(8, 0), requirement.Start);
        Assert.Equal(new TimeOnly(18, 0), requirement.End);
        Assert.Same(TimeZoneInfo.Utc, requirement.TimeZone);
        Assert.False(requirement.CrossesMidnight);
        Assert.Equal("business-hours", requirement.Id);
    }

    [Fact]
    public void Time_window_rejects_invalid_schedule()
    {
        Assert.Throws<ArgumentException>(
            () => new TimeWindowRequirementDefinition(
                [],
                new TimeOnly(8, 0),
                new TimeOnly(18, 0),
                TimeZoneInfo.Utc));

        Assert.Throws<ArgumentException>(
            () => new TimeWindowRequirementDefinition(
                [DayOfWeek.Monday, DayOfWeek.Monday],
                new TimeOnly(8, 0),
                new TimeOnly(18, 0),
                TimeZoneInfo.Utc));

        Assert.Throws<ArgumentException>(
            () => new TimeWindowRequirementDefinition(
                [DayOfWeek.Monday],
                new TimeOnly(8, 0),
                new TimeOnly(8, 0),
                TimeZoneInfo.Utc));
    }

    [Fact]
    public void Date_time_window_normalizes_boundaries_to_utc()
    {
        var requirement =
            new DateTimeWindowRequirementDefinition(
                new DateTimeOffset(
                    2026, 7, 29, 12, 0, 0,
                    TimeSpan.FromHours(3)),
                new DateTimeOffset(
                    2026, 7, 29, 14, 0, 0,
                    TimeSpan.FromHours(3)));

        Assert.Equal(
            new DateTimeOffset(2026, 7, 29, 9, 0, 0, TimeSpan.Zero),
            requirement.StartsAt);
        Assert.Equal(
            new DateTimeOffset(2026, 7, 29, 11, 0, 0, TimeSpan.Zero),
            requirement.EndsAt);
    }

    [Fact]
    public void Context_age_uses_canonical_attribute_name()
    {
        var requirement = new ContextAgeRequirementDefinition(
            AuthorizationContextTimestamp
                .MultiFactorAuthenticationTime,
            TimeSpan.FromMinutes(15));

        Assert.Equal(
            AuthorizationContextAttributeNames
                .MultiFactorAuthenticationTime,
            requirement.AttributeName);
        Assert.Equal(TimeSpan.FromMinutes(15), requirement.MaximumAge);
    }

    [Theory]
    [InlineData(
        AuthorizationContextProperty.AuthenticationMethod,
        AuthorizationContextAttributeNames.AuthenticationMethod)]
    [InlineData(
        AuthorizationContextProperty.RequestChannel,
        AuthorizationContextAttributeNames.RequestChannel)]
    [InlineData(
        AuthorizationContextProperty.NetworkZone,
        AuthorizationContextAttributeNames.NetworkZone)]
    [InlineData(
        AuthorizationContextProperty.TenantId,
        AuthorizationContextAttributeNames.TenantId)]
    [InlineData(
        AuthorizationContextProperty.OrganizationId,
        AuthorizationContextAttributeNames.OrganizationId)]
    [InlineData(
        AuthorizationContextProperty.TrustedDevice,
        AuthorizationContextAttributeNames.TrustedDevice)]
    [InlineData(
        AuthorizationContextProperty.IdentityType,
        AuthorizationContextAttributeNames.IdentityType)]
    public void Context_requirement_uses_canonical_attribute_name(
        AuthorizationContextProperty property,
        string expectedName)
    {
        var value = property ==
            AuthorizationContextProperty.TrustedDevice
                ? (object)true
                : "expected";

        var requirement = new ContextRequirementDefinition(
            property,
            AuthorizationAttributeOperator.Equal,
            value);

        Assert.Equal(expectedName, requirement.AttributeName);
    }

    [Fact]
    public void Context_requirement_rejects_incompatible_contracts()
    {
        Assert.Throws<ArgumentException>(
            () => new ContextRequirementDefinition(
                AuthorizationContextProperty.TrustedDevice,
                AuthorizationAttributeOperator.Equal,
                "true"));

        Assert.Throws<ArgumentException>(
            () => new ContextRequirementDefinition(
                AuthorizationContextProperty.TenantId,
                AuthorizationAttributeOperator.GreaterThan,
                "tenant-1"));
    }
}
