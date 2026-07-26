using Fotbiler.RuleGate.AspNetCore.Authorization;

namespace Fotbiler.RuleGate.AspNetCore.Tests;

public sealed class RuleGatePolicyNameTests
{
    [Fact]
    public void Constructor_sets_segments_and_formatted_name()
    {
        var policyName =
            new RuleGatePolicyName(
                resourceType: "document",
                action: "read");

        Assert.Equal(
            "document",
            policyName.ResourceType);

        Assert.Equal(
            "read",
            policyName.Action);

        Assert.Equal(
            "RuleGate:document:read",
            policyName.ToString());
    }

    [Theory]
    [InlineData(null, "read")]
    [InlineData("", "read")]
    [InlineData(" ", "read")]
    [InlineData("document type", "read")]
    [InlineData("document:private", "read")]
    [InlineData("document", null)]
    [InlineData("document", "")]
    [InlineData("document", " ")]
    [InlineData("document", "read all")]
    [InlineData("document", "read:private")]
    public void Constructor_rejects_invalid_segments(
        string? resourceType,
        string? action)
    {
        Assert.ThrowsAny<ArgumentException>(
            () =>
                new RuleGatePolicyName(
                    resourceType!,
                    action!));
    }

    [Fact]
    public void TryParse_returns_policy_name_for_valid_value()
    {
        var succeeded =
            RuleGatePolicyName.TryParse(
                "RuleGate:document:read",
                out var policyName);

        Assert.True(succeeded);
        Assert.NotNull(policyName);

        Assert.Equal(
            "document",
            policyName.ResourceType);

        Assert.Equal(
            "read",
            policyName.Action);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("RuleGate")]
    [InlineData("RuleGate:")]
    [InlineData("RuleGate:document")]
    [InlineData("RuleGate::read")]
    [InlineData("RuleGate:document:")]
    [InlineData("RuleGate:document:read:extra")]
    [InlineData("rulegate:document:read")]
    [InlineData("RULEGATE:document:read")]
    [InlineData("RuleGate:document type:read")]
    [InlineData("RuleGate:document:read all")]
    public void TryParse_rejects_invalid_value(
        string? value)
    {
        var succeeded =
            RuleGatePolicyName.TryParse(
                value,
                out var policyName);

        Assert.False(succeeded);
        Assert.Null(policyName);
    }

    [Theory]
    [InlineData(
        "document",
        "read")]
    [InlineData(
        "document-template",
        "version.read")]
    [InlineData(
        "invoice",
        "approve")]
    public void Formatted_name_round_trips(
        string resourceType,
        string action)
    {
        var original =
            new RuleGatePolicyName(
                resourceType,
                action);

        var succeeded =
            RuleGatePolicyName.TryParse(
                original.ToString(),
                out var parsed);

        Assert.True(succeeded);
        Assert.NotNull(parsed);

        Assert.Equal(
            original.ResourceType,
            parsed.ResourceType);

        Assert.Equal(
            original.Action,
            parsed.Action);
    }
}
