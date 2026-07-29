namespace Fotbiler.RuleGate.Manifest.Models;

public sealed class ManifestRequirement
{
    public string? Id { get; set; }

    public string? Permission { get; set; }

    public string? Role { get; set; }

    public ManifestAttributeRequirement? Attribute
    {
        get;
        set;
    }

    public ManifestAttributeComparisonRequirement?
        AttributeComparison
    {
        get;
        set;
    }

    public ManifestTimeWindowRequirement? TimeWindow
    {
        get;
        set;
    }

    public ManifestDateTimeWindowRequirement? DateTimeWindow
    {
        get;
        set;
    }

    public ManifestContextAgeRequirement? ContextAge
    {
        get;
        set;
    }

    public ManifestContextRequirement? Context
    {
        get;
        set;
    }

    public List<ManifestRequirement?>? All { get; set; }

    public List<ManifestRequirement?>? Any { get; set; }

    public ManifestRequirement? Not { get; set; }
}
