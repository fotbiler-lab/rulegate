using YamlDotNet.Serialization;

namespace Fotbiler.RuleGate.Manifest.Models;

public sealed class ManifestAttributeComparisonOperand
{
    private object? _value;

    public string? Source { get; set; }

    public string? Name { get; set; }

    public string? ValueType { get; set; }

    public object? Value
    {
        get => _value;

        set
        {
            HasValue = true;
            _value = value;
        }
    }

    [YamlIgnore]
    public bool HasValue { get; private set; }
}
