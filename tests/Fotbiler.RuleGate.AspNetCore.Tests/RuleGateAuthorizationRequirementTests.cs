using Fotbiler.RuleGate.AspNetCore.Authorization;

namespace Fotbiler.RuleGate.AspNetCore.Tests;

public sealed class
    RuleGateAuthorizationRequirementTests
{
    [Fact]
    public void Constructor_RejectsBlankAction()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new RuleGateAuthorizationRequirement(
                    " "));
    }

    [Fact]
    public void Constructor_PreservesAction()
    {
        var requirement =
            new RuleGateAuthorizationRequirement(
                "document.read");

        Assert.Equal(
            "document.read",
            requirement.Action);
    }
}
