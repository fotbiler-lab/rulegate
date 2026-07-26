using System.Collections;

namespace Fotbiler.RuleGate.Abstractions.Attributes;

public sealed class AuthorizationAttributes
    : IReadOnlyDictionary<string, object?>
{
    private readonly Dictionary<string, object?> _values;

    public AuthorizationAttributes(
        IEnumerable<KeyValuePair<string, object?>>? values = null)
    {
        _values = values is null
            ? new Dictionary<string, object?>(StringComparer.Ordinal)
            : new Dictionary<string, object?>(
                values,
                StringComparer.Ordinal);
    }

    public static AuthorizationAttributes Empty { get; } = new();

    public object? this[string key] => _values[key];

    public IEnumerable<string> Keys => _values.Keys;

    public IEnumerable<object?> Values => _values.Values;

    public int Count => _values.Count;

    public bool ContainsKey(string key)
    {
        ArgumentNullException.ThrowIfNull(key);

        return _values.ContainsKey(key);
    }

    public bool TryGetValue(
        string key,
        out object? value)
    {
        ArgumentNullException.ThrowIfNull(key);

        return _values.TryGetValue(key, out value);
    }

    public IEnumerator<KeyValuePair<string, object?>>
        GetEnumerator()
    {
        return _values.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
