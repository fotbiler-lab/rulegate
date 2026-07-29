using YamlDotNet.Serialization;

namespace Fotbiler.RuleGate.Manifest.Models;

public sealed class ManifestContextRequirement
{
    private object? _value;

    public string? Property { get; set; }

    public string? Operator { get; set; }

    public string? StringComparison { get; set; }

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
