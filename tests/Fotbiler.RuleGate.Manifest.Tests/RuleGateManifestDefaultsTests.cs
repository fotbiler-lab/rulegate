using Fotbiler.RuleGate.Manifest.Configuration;

namespace Fotbiler.RuleGate.Manifest.Tests;

public sealed class RuleGateManifestDefaultsTests
{
    [Fact]
    public void DefaultFileName_IsRuleGateYaml()
    {
        Assert.Equal(
            "rulegate.yaml",
            RuleGateManifestDefaults.FileName);
    }

    [Fact]
    public void SupportedSchemaVersion_StartsAtOne()
    {
        Assert.Equal(
            1,
            RuleGateManifestDefaults
                .SupportedSchemaVersion);
    }
}
