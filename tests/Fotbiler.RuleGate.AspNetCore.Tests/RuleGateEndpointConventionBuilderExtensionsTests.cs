using Fotbiler.RuleGate.AspNetCore.Authorization;
using Fotbiler.RuleGate.AspNetCore.Endpoints;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;

namespace Fotbiler.RuleGate.AspNetCore.Tests;

public sealed class
    RuleGateEndpointConventionBuilderExtensionsTests
{
    [Fact]
    public void RequireRuleGate_requires_builder()
    {
        RecordingEndpointConventionBuilder
            builder = null!;

        Assert.Throws<ArgumentNullException>(
            () =>
                builder.RequireRuleGate(
                    resourceType: "document",
                    action: "read"));
    }

    [Fact]
    public void RequireRuleGate_returns_original_builder()
    {
        var builder =
            new RecordingEndpointConventionBuilder();

        var result =
            builder.RequireRuleGate(
                resourceType: "document",
                action: "read");

        Assert.Same(builder, result);
    }

    [Fact]
    public void RequireRuleGate_adds_metadata_and_policy()
    {
        var conventions =
            new RecordingEndpointConventionBuilder();

        conventions.RequireRuleGate(
            resourceType: "document",
            action: "approve",
            resourceIdRouteValue: "id");

        var endpointBuilder =
            CreateEndpointBuilder();

        conventions.ApplyTo(
            endpointBuilder);

        var metadata =
            Assert.Single(
                endpointBuilder.Metadata
                    .OfType<
                        IRuleGateAuthorizationMetadata>());

        Assert.Equal(
            "document",
            metadata.ResourceType);

        Assert.Equal(
            "approve",
            metadata.Action);

        Assert.Equal(
            "id",
            metadata.ResourceIdRouteValue);

        var authorizationData =
            Assert.Single(
                endpointBuilder.Metadata
                    .OfType<IAuthorizeData>());

        Assert.Equal(
            "RuleGate:document:approve",
            authorizationData.Policy);
    }

    [Theory]
    [InlineData(null, "read")]
    [InlineData("document type", "read")]
    [InlineData("document", null)]
    [InlineData("document", "read all")]
    public void RequireRuleGate_rejects_invalid_policy_segments(
        string? resourceType,
        string? action)
    {
        var builder =
            new RecordingEndpointConventionBuilder();

        Assert.ThrowsAny<ArgumentException>(
            () =>
                builder.RequireRuleGate(
                    resourceType!,
                    action!));

        Assert.Empty(
            builder.Conventions);
    }

    private static RouteEndpointBuilder
        CreateEndpointBuilder()
    {
        return new RouteEndpointBuilder(
            requestDelegate:
                _ => Task.CompletedTask,
            routePattern:
                RoutePatternFactory.Parse(
                    "/documents/{id}"),
            order: 0);
    }

    private sealed class
        RecordingEndpointConventionBuilder
        : IEndpointConventionBuilder
    {
        public List<Action<EndpointBuilder>>
            Conventions
        { get; } = [];

        public void Add(
            Action<EndpointBuilder> convention)
        {
            ArgumentNullException.ThrowIfNull(
                convention);

            Conventions.Add(convention);
        }

        public void ApplyTo(
            EndpointBuilder endpointBuilder)
        {
            foreach (var convention in Conventions)
            {
                convention(endpointBuilder);
            }
        }
    }
}
