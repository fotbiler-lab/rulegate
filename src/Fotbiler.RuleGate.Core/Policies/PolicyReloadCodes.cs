namespace Fotbiler.RuleGate.Core.Policies;

public static class PolicyReloadCodes
{
    public const string DuplicateSourceName =
        "POLICY_SOURCE_DUPLICATE_NAME";

    public const string SourceLoadException =
        "POLICY_SOURCE_LOAD_EXCEPTION";

    public const string SourceReturnedNull =
        "POLICY_SOURCE_RETURNED_NULL";

    public const string DuplicatePolicyId =
        "POLICY_SNAPSHOT_DUPLICATE_POLICY_ID";

    public const string DuplicatePolicyRoute =
        "POLICY_SNAPSHOT_DUPLICATE_POLICY_ROUTE";
}
