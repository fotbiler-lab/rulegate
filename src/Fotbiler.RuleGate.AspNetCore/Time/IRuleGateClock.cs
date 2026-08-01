namespace Fotbiler.RuleGate.AspNetCore.Time;

/// <summary>
/// Provides the trusted UTC time used when RuleGate creates
/// authorization evaluation context.
/// </summary>
public interface IRuleGateClock
{
    /// <summary>
    /// Gets the current trusted UTC time.
    /// </summary>
    DateTimeOffset GetUtcNow();
}
