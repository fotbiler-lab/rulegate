using Fotbiler.RuleGate.AspNetCore.Authorization;

namespace Fotbiler.RuleGate.AspNetCore.Tests;

public sealed class
    RuleGateAuthorizationRequirementTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Action_constructor_rejects_blank_action(
        string? action)
    {
        Assert.ThrowsAny<ArgumentException>(
            () =>
                new RuleGateAuthorizationRequirement(
                    action!));
    }

    [Fact]
    public void Action_constructor_sets_action()
    {
        var requirement =
            new RuleGateAuthorizationRequirement(
                action: "read");

        Assert.Null(requirement.ResourceType);

        Assert.Equal(
            "read",
            requirement.Action);
    }

    [Fact]
    public void Resource_constructor_sets_resource_type_and_action()
    {
        var requirement =
            new RuleGateAuthorizationRequirement(
                resourceType: "document",
                action: "read");

        Assert.Equal(
            "document",
            requirement.ResourceType);

        Assert.Equal(
            "read",
            requirement.Action);
    }

    [Theory]
    [InlineData(null, "read")]
    [InlineData("", "read")]
    [InlineData("document type", "read")]
    [InlineData("document", null)]
    [InlineData("document", "")]
    [InlineData("document", "read all")]
    public void Resource_constructor_rejects_invalid_segments(
        string? resourceType,
        string? action)
    {
        Assert.ThrowsAny<ArgumentException>(
            () =>
                new RuleGateAuthorizationRequirement(
                    resourceType!,
                    action!));
    }
}
