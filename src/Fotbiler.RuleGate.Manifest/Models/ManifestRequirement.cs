namespace Fotbiler.RuleGate.Manifest.Models;

public sealed class ManifestRequirement
{
    public string? Id { get; set; }

    public string? Permission { get; set; }

    public string? Role { get; set; }

    public List<ManifestRequirement?>? All { get; set; }

    public List<ManifestRequirement?>? Any { get; set; }

    public ManifestRequirement? Not { get; set; }
}
