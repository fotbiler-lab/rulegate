namespace Fotbiler.RuleGate.Manifest.Configuration;

internal static class RuleGateManifestResourceLimits
{
    internal const int MaximumManifestContentByteCount =
        1_048_576;

    internal const int MaximumPolicyCount =
        4_096;

    internal const int MaximumRequirementDepth =
        64;

    internal const int MaximumRequirementNodeCountPerPolicy =
        4_096;

    internal const int MaximumCompositeChildCount =
        1_024;

    internal const int MaximumTotalRequirementNodeCount =
        65_536;
}
