using System.Diagnostics;

namespace Fotbiler.RuleGate.Core.Compatibility;

internal static class StopwatchCompat
{
    internal static TimeSpan GetElapsedTime(
        long startTimestamp)
    {
        var elapsedTimestamp =
            Stopwatch.GetTimestamp() - startTimestamp;

        var elapsedTicks = (long)(
            elapsedTimestamp *
            (TimeSpan.TicksPerSecond /
             (double)Stopwatch.Frequency));

        return TimeSpan.FromTicks(elapsedTicks);
    }
}
