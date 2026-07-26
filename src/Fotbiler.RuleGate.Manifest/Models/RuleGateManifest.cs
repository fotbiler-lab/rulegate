namespace Fotbiler.RuleGate.Manifest.Models;

public sealed class RuleGateManifest
{
    public int SchemaVersion { get; set; }

    public ManifestApplication? Application { get; set; }

    public List<ManifestPolicy?>? Policies { get; set; }
}
