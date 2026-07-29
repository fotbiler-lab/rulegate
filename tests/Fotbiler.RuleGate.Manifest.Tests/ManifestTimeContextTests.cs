using Fotbiler.RuleGate.Abstractions.Authorization;
using Fotbiler.RuleGate.Abstractions.Policies;
using Fotbiler.RuleGate.Manifest.Configuration;
using Fotbiler.RuleGate.Manifest.Loading;
using Fotbiler.RuleGate.Manifest.Mapping;
using Fotbiler.RuleGate.Manifest.Models;
using Fotbiler.RuleGate.Manifest.Validation;

namespace Fotbiler.RuleGate.Manifest.Tests;

public sealed class ManifestTimeContextTests
{
    [Fact]
    public void LoadAndMap_supports_all_time_and_context_requirements()
    {
        const string yaml = """
            schemaVersion: 1
            application:
              id: secure-portal
              name: Secure Portal
            policies:
              - id: secure-access
                resourceType: portal
                action: access
                requirement:
                  all:
                    - id: business-hours
                      timeWindow:
                        days: [monday, tuesday, wednesday, thursday, friday]
                        start: "08:00"
                        end: "18:00"
                        timeZone: Europe/Istanbul
                    - id: campaign-window
                      dateTimeWindow:
                        startsAt: "2026-07-29T00:00:00+03:00"
                        endsAt: "2026-08-01T00:00:00+03:00"
                    - id: recent-mfa
                      contextAge:
                        timestamp: mfa
                        maximumAge: "00:15:00"
                    - id: trusted-device
                      context:
                        property: trustedDevice
                        operator: equal
                        valueType: boolean
                        value: true
                    - id: allowed-tenant
                      context:
                        property: tenantId
                        operator: in
                        valueType: stringCollection
                        value: [tenant-1, tenant-2]
            """;

        var load = new RuleGateManifestYamlLoader()
            .LoadFromText(yaml);

        Assert.True(load.IsSuccess);

        var mapping = new RuleGateManifestMapper(
            new RuleGateManifestValidator())
            .Map(load.Manifest!);

        Assert.True(mapping.IsSuccess);
        var all = Assert.IsType<AllRequirementDefinition>(
            Assert.Single(mapping.Policies).Requirement);
        Assert.Collection(
            all.Requirements,
            requirement => Assert.IsType<TimeWindowRequirementDefinition>(requirement),
            requirement => Assert.IsType<DateTimeWindowRequirementDefinition>(requirement),
            requirement => Assert.IsType<ContextAgeRequirementDefinition>(requirement),
            requirement => Assert.IsType<ContextRequirementDefinition>(requirement),
            requirement => Assert.IsType<ContextRequirementDefinition>(requirement));

        var contextAge = Assert.IsType<ContextAgeRequirementDefinition>(
            all.Requirements[2]);
        Assert.Equal(
            AuthorizationContextTimestamp.MultiFactorAuthenticationTime,
            contextAge.Timestamp);
        Assert.Equal(TimeSpan.FromMinutes(15), contextAge.MaximumAge);
    }

    [Fact]
    public void Validate_rejects_invalid_time_window_fields()
    {
        var result = Validate(
            new ManifestRequirement
            {
                TimeWindow = new ManifestTimeWindowRequirement
                {
                    Days = ["monday", "monday", "funday"],
                    Start = "8am",
                    End = "8am",
                    TimeZone = "Not/A-Time-Zone"
                }
            });

        AssertErrors(
            result,
            ManifestValidationCodes.TimeWindowDayDuplicate,
            ManifestValidationCodes.TimeWindowDayInvalid,
            ManifestValidationCodes.TimeWindowStartInvalid,
            ManifestValidationCodes.TimeWindowEndInvalid,
            ManifestValidationCodes.TimeWindowTimeZoneInvalid);
    }

    [Fact]
    public void Validate_rejects_equal_time_window_boundaries()
    {
        var result = Validate(
            new ManifestRequirement
            {
                TimeWindow = new ManifestTimeWindowRequirement
                {
                    Days = ["monday"],
                    Start = "08:00",
                    End = "08:00",
                    TimeZone = "UTC"
                }
            });

        AssertErrors(
            result,
            ManifestValidationCodes.TimeWindowRangeInvalid);
    }

    [Fact]
    public void Validate_rejects_missing_or_reversed_date_time_boundaries()
    {
        var missing = Validate(
            new ManifestRequirement
            {
                DateTimeWindow =
                    new ManifestDateTimeWindowRequirement()
            });

        var reversed = Validate(
            new ManifestRequirement
            {
                DateTimeWindow =
                    new ManifestDateTimeWindowRequirement
                    {
                        StartsAt = "2026-08-01T00:00:00Z",
                        EndsAt = "2026-07-01T00:00:00Z"
                    }
            });

        AssertErrors(
            missing,
            ManifestValidationCodes.DateTimeWindowBoundaryRequired);
        AssertErrors(
            reversed,
            ManifestValidationCodes.DateTimeWindowRangeInvalid);
    }

    [Fact]
    public void Validate_rejects_invalid_context_age()
    {
        var result = Validate(
            new ManifestRequirement
            {
                ContextAge = new ManifestContextAgeRequirement
                {
                    Timestamp = "lastLogin",
                    MaximumAge = "00:00:00"
                }
            });

        AssertErrors(
            result,
            ManifestValidationCodes.ContextAgeTimestampInvalid,
            ManifestValidationCodes.ContextAgeMaximumAgeInvalid);
    }

    [Fact]
    public void Validate_rejects_incompatible_context_contract()
    {
        var result = Validate(
            new ManifestRequirement
            {
                Context = new ManifestContextRequirement
                {
                    Property = "trustedDevice",
                    Operator = "contains",
                    StringComparison = "ordinalIgnoreCase",
                    ValueType = "string",
                    Value = "true"
                }
            });

        AssertErrors(
            result,
            ManifestValidationCodes.ContextPropertyOperatorValueInvalid,
            ManifestValidationCodes.ContextStringComparisonNotAllowed);
    }

    private static ManifestValidationResult Validate(
        ManifestRequirement requirement)
    {
        return new RuleGateManifestValidator().Validate(
            new RuleGateManifest
            {
                SchemaVersion =
                    RuleGateManifestDefaults.SupportedSchemaVersion,
                Application = new ManifestApplication
                {
                    Id = "test",
                    Name = "Test"
                },
                Policies =
                [
                    new ManifestPolicy
                    {
                        Id = "test-policy",
                        ResourceType = "test",
                        Action = "read",
                        Requirement = requirement
                    }
                ]
            });
    }

    private static void AssertErrors(
        ManifestValidationResult result,
        params string[] expectedCodes)
    {
        Assert.False(result.IsValid);

        foreach (var expectedCode in expectedCodes)
        {
            Assert.Contains(
                result.Errors,
                error => error.Code == expectedCode);
        }
    }
}
