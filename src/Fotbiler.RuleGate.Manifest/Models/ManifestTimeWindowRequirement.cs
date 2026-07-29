namespace Fotbiler.RuleGate.Manifest.Models;

public sealed class ManifestTimeWindowRequirement
{
    public List<string?>? Days { get; set; }

    public string? Start { get; set; }

    public string? End { get; set; }

    public string? TimeZone { get; set; }
}
