namespace Fotbiler.RuleGate.Manifest.Models;

public sealed class ManifestPolicy
{
    public string? Id { get; set; }

    public string? ResourceType { get; set; }

    public string? Action { get; set; }

    public ManifestRequirement? Requirement { get; set; }
}
