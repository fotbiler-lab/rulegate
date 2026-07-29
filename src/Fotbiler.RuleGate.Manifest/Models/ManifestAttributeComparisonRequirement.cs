namespace Fotbiler.RuleGate.Manifest.Models;

public sealed class ManifestAttributeComparisonRequirement
{
    public ManifestAttributeComparisonOperand? Left { get; set; }

    public string? Operator { get; set; }

    public ManifestAttributeComparisonOperand? Right { get; set; }

    public string? StringComparison { get; set; }
}
