namespace Fotbiler.RuleGate.AspNetCore.Time;

internal sealed class SystemRuleGateClock
    : IRuleGateClock
{
    internal static SystemRuleGateClock Instance { get; } =
        new();

    private SystemRuleGateClock()
    {
    }

    public DateTimeOffset GetUtcNow()
    {
        return DateTimeOffset.UtcNow;
    }
}
