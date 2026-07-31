using Fotbiler.RuleGate.Abstractions.Policies;

namespace Fotbiler.RuleGate.Abstractions.Tests;

public sealed class PolicySourceContractTests
{
    [Fact]
    public void SourceLoadSuccess_CreatesImmutablePolicyCollection()
    {
        var policies = new List<PolicyDefinition>
        {
            CreatePolicy("document-read"),
        };

        var result = PolicySourceLoadResult.Success(
            policies);

        policies.Clear();

        Assert.True(result.IsSuccess);
        Assert.Single(result.Policies);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void SourceLoadFailure_RequiresAndOrdersDiagnostics()
    {
        var result = PolicySourceLoadResult.Failure(
        [
            new PolicySourceDiagnostic(
                "Z_CODE",
                "Second",
                "z"),
            new PolicySourceDiagnostic(
                "A_CODE",
                "First",
                "a")
        ]);

        Assert.False(result.IsSuccess);
        Assert.Empty(result.Policies);
        Assert.Equal(
            ["A_CODE", "Z_CODE"],
            result.Diagnostics.Select(
                static item => item.Code));

        Assert.Throws<ArgumentException>(
            () => PolicySourceLoadResult.Failure([]));
    }

    [Fact]
    public void ReloadRejected_PreservesSnapshotAndOrdersDiagnostics()
    {
        var snapshot = new PolicySnapshotInfo(
            3,
            2,
            ["primary"]);

        var result = PolicyReloadResult.Rejected(
            snapshot,
        [
            new PolicyReloadDiagnostic(
                "z-source",
                "Z_CODE",
                "Second"),
            new PolicyReloadDiagnostic(
                "a-source",
                "A_CODE",
                "First")
        ]);

        Assert.False(result.IsSuccess);
        Assert.False(result.IsActivated);
        Assert.Same(snapshot, result.ActiveSnapshot);
        Assert.Equal(
            ["A_CODE", "Z_CODE"],
            result.Diagnostics.Select(
                static item => item.Code));
    }

    [Fact]
    public void SnapshotInfo_CopiesSourceNames()
    {
        var names = new List<string>
        {
            "primary",
        };

        var snapshot = new PolicySnapshotInfo(
            1,
            1,
            names);

        names.Clear();

        Assert.Equal(["primary"], snapshot.SourceNames);
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new PolicySnapshotInfo(-1, 0, []));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new PolicySnapshotInfo(0, -1, []));
    }

    private static PolicyDefinition CreatePolicy(
        string id)
    {
        return new PolicyDefinition(
            id,
            "document",
            "read",
            new PermissionRequirementDefinition(
                "document.read"));
    }
}
