using Fotbiler.RuleGate.Abstractions.Attributes;
using Fotbiler.RuleGate.Abstractions.Authorization;
using Fotbiler.RuleGate.Abstractions.Constants;
using Fotbiler.RuleGate.Abstractions.Evaluation;
using Fotbiler.RuleGate.Abstractions.Policies;
using Fotbiler.RuleGate.Core.Evaluation;
using Fotbiler.RuleGate.Core.Evaluation.Evaluators;

namespace Fotbiler.RuleGate.Core.Tests;

public sealed class TimeContextRequirementEvaluatorTests
{
    [Theory]
    [InlineData(8, 0, true)]
    [InlineData(17, 59, true)]
    [InlineData(18, 0, false)]
    public async Task Time_window_is_start_inclusive_and_end_exclusive(
        int hour,
        int minute,
        bool expected)
    {
        var requirement = new TimeWindowRequirementDefinition(
            [DayOfWeek.Monday],
            TimeSpan.FromHours(8),
            TimeSpan.FromHours(18),
            TimeZoneInfo.Utc);

        var result = await EvaluateAsync(
            requirement,
            new DateTimeOffset(
                2026, 7, 27, hour, minute, 0,
                TimeSpan.Zero));

        Assert.Equal(expected, result.IsSatisfied);
        Assert.Equal(!expected, result.IsNotSatisfied);
    }

    [Theory]
    [InlineData(2026, 7, 31, 23, 0, true)]
    [InlineData(2026, 8, 1, 1, 59, true)]
    [InlineData(2026, 8, 1, 2, 0, false)]
    public async Task Time_window_can_cross_midnight(
        int year,
        int month,
        int day,
        int hour,
        int minute,
        bool expected)
    {
        var requirement = new TimeWindowRequirementDefinition(
            [DayOfWeek.Friday],
            TimeSpan.FromHours(22),
            TimeSpan.FromHours(2),
            TimeZoneInfo.Utc);

        var result = await EvaluateAsync(
            requirement,
            new DateTimeOffset(
                year, month, day, hour, minute, 0,
                TimeSpan.Zero));

        Assert.Equal(expected, result.IsSatisfied);
    }

    [Fact]
    public async Task Time_window_converts_evaluation_time_to_explicit_zone()
    {
        var utcPlusThree = TimeZoneInfo.CreateCustomTimeZone(
            "RuleGate-Test-UTC+3",
            TimeSpan.FromHours(3),
            "RuleGate Test UTC+3",
            "RuleGate Test UTC+3");

        var requirement = new TimeWindowRequirementDefinition(
            [DayOfWeek.Monday],
            TimeSpan.FromHours(8),
            TimeSpan.FromHours(9),
            utcPlusThree);

        var result = await EvaluateAsync(
            requirement,
            new DateTimeOffset(
                2026, 7, 27, 5, 30, 0,
                TimeSpan.Zero));

        Assert.True(result.IsSatisfied);
    }

    [Theory]
    [InlineData(9, true)]
    [InlineData(10, true)]
    [InlineData(11, false)]
    public async Task Date_time_window_uses_half_open_boundaries(
        int hour,
        bool expected)
    {
        var requirement =
            new DateTimeWindowRequirementDefinition(
                new DateTimeOffset(
                    2026, 7, 29, 9, 0, 0,
                    TimeSpan.Zero),
                new DateTimeOffset(
                    2026, 7, 29, 11, 0, 0,
                    TimeSpan.Zero));

        var result = await EvaluateAsync(
            requirement,
            new DateTimeOffset(
                2026, 7, 29, hour, 0, 0,
                TimeSpan.Zero));

        Assert.Equal(expected, result.IsSatisfied);
    }

    [Fact]
    public async Task Context_age_satisfies_recent_timestamp()
    {
        var evaluationTime =
            new DateTimeOffset(
                2026, 7, 29, 12, 0, 0,
                TimeSpan.Zero);

        var result = await EvaluateAsync(
            new ContextAgeRequirementDefinition(
                AuthorizationContextTimestamp.AuthenticationTime,
                TimeSpan.FromMinutes(30)),
            evaluationTime,
            Attributes(
                (AuthorizationContextAttributeNames.AuthenticationTime,
                 evaluationTime.AddMinutes(-20))));

        Assert.True(result.IsSatisfied);
    }

    [Fact]
    public async Task Context_age_denies_missing_or_stale_timestamp()
    {
        var evaluationTime = DateTimeOffset.UnixEpoch.AddHours(2);
        var requirement = new ContextAgeRequirementDefinition(
            AuthorizationContextTimestamp.AuthenticationTime,
            TimeSpan.FromMinutes(30));

        var missing = await EvaluateAsync(
            requirement,
            evaluationTime);
        var stale = await EvaluateAsync(
            requirement,
            evaluationTime,
            Attributes(
                (AuthorizationContextAttributeNames.AuthenticationTime,
                 evaluationTime.AddHours(-1))));

        Assert.Equal(
            AuthorizationFailureCodes.AttributeNotFound,
            Assert.Single(missing.Failures).Code);
        Assert.Equal(
            AuthorizationFailureCodes.ContextAgeNotSatisfied,
            Assert.Single(stale.Failures).Code);
    }

    [Fact]
    public async Task Future_context_timestamp_is_indeterminate()
    {
        var evaluationTime = DateTimeOffset.UnixEpoch;

        var result = await EvaluateAsync(
            new ContextAgeRequirementDefinition(
                AuthorizationContextTimestamp
                    .MultiFactorAuthenticationTime,
                TimeSpan.FromMinutes(10)),
            evaluationTime,
            Attributes(
                (AuthorizationContextAttributeNames
                    .MultiFactorAuthenticationTime,
                 evaluationTime.AddSeconds(1))));

        Assert.True(result.IsIndeterminate);
        Assert.Equal(
            AuthorizationFailureCodes.ContextTimestampInFuture,
            Assert.Single(result.Failures).Code);
    }

    [Fact]
    public async Task Context_requirement_compares_canonical_value()
    {
        var result = await EvaluateAsync(
            new ContextRequirementDefinition(
                AuthorizationContextProperty.AuthenticationMethod,
                AuthorizationAttributeOperator.Equal,
                "mfa",
                stringComparison:
                    AuthorizationStringComparison.OrdinalIgnoreCase),
            DateTimeOffset.UnixEpoch,
            Attributes(
                (AuthorizationContextAttributeNames.AuthenticationMethod,
                 "MFA")));

        Assert.True(result.IsSatisfied);
    }

    [Fact]
    public async Task Context_requirement_denies_missing_value()
    {
        var result = await EvaluateAsync(
            new ContextRequirementDefinition(
                AuthorizationContextProperty.TrustedDevice,
                AuthorizationAttributeOperator.Equal,
                true),
            DateTimeOffset.UnixEpoch);

        Assert.True(result.IsNotSatisfied);
        Assert.Equal(
            AuthorizationFailureCodes.AttributeNotFound,
            Assert.Single(result.Failures).Code);
    }

    private static AuthorizationAttributes Attributes(
        params (string Name, object? Value)[] values)
    {
        return new AuthorizationAttributes(
            values.Select(
                static pair =>
                    new KeyValuePair<string, object?>(
                        pair.Name,
                        pair.Value)));
    }

    private static async Task<RequirementEvaluationResult> EvaluateAsync(
        RequirementDefinition requirement,
        DateTimeOffset evaluationTime,
        AuthorizationAttributes? contextAttributes = null)
    {
        var request = new AuthorizationRequest(
            new AuthorizationSubject("user-1"),
            new AuthorizationResource("document"),
            "read",
            new AuthorizationContext(
                evaluationTime,
                contextAttributes));

        var dispatcher = new RequirementEvaluationDispatcher(
        [
            new TimeWindowRequirementEvaluator(),
            new DateTimeWindowRequirementEvaluator(),
            new ContextAgeRequirementEvaluator(),
            new ContextRequirementEvaluator()
        ]);

        return await dispatcher.EvaluateAsync(
            requirement,
            new RequirementEvaluationContext(request));
    }
}
