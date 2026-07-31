using Fotbiler.RuleGate.Abstractions.Diagnostics;

namespace Fotbiler.RuleGate.Abstractions.Tests;

public sealed class RuleGateTelemetryTests
{
    [Fact]
    public void Public_names_are_stable_and_non_empty()
    {
        Assert.Equal(
            "Fotbiler.RuleGate",
            RuleGateTelemetry.ActivitySourceName);
        Assert.Equal(
            "Fotbiler.RuleGate",
            RuleGateTelemetry.MeterName);
        Assert.Equal(
            "rulegate.authorization.evaluate",
            RuleGateTelemetry.AuthorizationActivityName);
        Assert.Equal(
            "rulegate.policy.reload",
            RuleGateTelemetry.PolicyReloadActivityName);
        Assert.Equal(
            "rulegate.policy.source.load",
            RuleGateTelemetry.PolicySourceLoadActivityName);
    }
}
