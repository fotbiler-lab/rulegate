namespace Fotbiler.RuleGate.Abstractions.Diagnostics;

public static class RuleGateTelemetry
{
    public const string ActivitySourceName =
        "Fotbiler.RuleGate";

    public const string MeterName =
        "Fotbiler.RuleGate";

    public const string AuthorizationActivityName =
        "rulegate.authorization.evaluate";

    public const string PolicyReloadActivityName =
        "rulegate.policy.reload";

    public const string PolicySourceLoadActivityName =
        "rulegate.policy.source.load";
}
