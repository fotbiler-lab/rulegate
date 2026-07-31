namespace Fotbiler.RuleGate.Core.Compatibility;

internal static class ValueTaskCompat
{
    internal static ValueTask<T> FromResult<T>(
        T result)
    {
        return new ValueTask<T>(result);
    }

    internal static ValueTask CompletedTask => default;
}
