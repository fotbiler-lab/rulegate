using Fotbiler.RuleGate.AspNetCore.Authorization;

namespace Fotbiler.RuleGate.AspNetCore.Tests;

public sealed class RuleGateAuthorizationMetadataTests
{
    [Fact]
    public void Constructor_sets_metadata_and_policy_name()
    {
        var metadata =
            new RuleGateAuthorizationMetadata(
                resourceType: "document",
                action: "approve",
                resourceIdRouteValue: "id");

        Assert.Equal(
            "document",
            metadata.ResourceType);

        Assert.Equal(
            "approve",
            metadata.Action);

        Assert.Equal(
            "id",
            metadata.ResourceIdRouteValue);

        Assert.Equal(
            "RuleGate:document:approve",
            metadata.PolicyName);
    }

    [Fact]
    public void Constructor_allows_missing_route_value_name()
    {
        var metadata =
            new RuleGateAuthorizationMetadata(
                resourceType: "document",
                action: "create");

        Assert.Null(
            metadata.ResourceIdRouteValue);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void Constructor_rejects_blank_route_value_name(
        string resourceIdRouteValue)
    {
        Assert.Throws<ArgumentException>(
            () =>
                new RuleGateAuthorizationMetadata(
                    resourceType: "document",
                    action: "read",
                    resourceIdRouteValue));
    }

    [Theory]
    [InlineData(null, "read")]
    [InlineData("", "read")]
    [InlineData("document type", "read")]
    [InlineData("document", null)]
    [InlineData("document", "read all")]
    public void Constructor_rejects_invalid_policy_segments(
        string? resourceType,
        string? action)
    {
        Assert.ThrowsAny<ArgumentException>(
            () =>
                new RuleGateAuthorizationMetadata(
                    resourceType!,
                    action!));
    }

    [Fact]
    public void Attribute_sets_authorization_policy_and_metadata()
    {
        var attribute =
            new RuleGateAuthorizeAttribute(
                resourceType: "invoice",
                action: "approve",
                resourceIdRouteValue: "invoiceId");

        Assert.Equal(
            "RuleGate:invoice:approve",
            attribute.Policy);

        Assert.Equal(
            "invoice",
            attribute.ResourceType);

        Assert.Equal(
            "approve",
            attribute.Action);

        Assert.Equal(
            "invoiceId",
            attribute.ResourceIdRouteValue);
    }
}
