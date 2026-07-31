using System.Diagnostics;

namespace Fotbiler.RuleGate.AspNetCore.Compatibility;

internal static class StopwatchCompat
{
    internal static TimeSpan GetElapsedTime(long startingTimestamp)
    {
        var elapsedTicks = Stopwatch.GetTimestamp() - startingTimestamp;
        return TimeSpan.FromSeconds(
            (double)elapsedTicks / Stopwatch.Frequency);
    }
}
