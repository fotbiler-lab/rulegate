using Fotbiler.RuleGate.Abstractions.Authorization;
using Fotbiler.RuleGate.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace Fotbiler.RuleGate.AspNetCore.Tests;

public sealed class
    RuleGateAuthorizationResourceFactoryTests
{
    [Fact]
    public void Create_RejectsMissingResource()
    {
        var factory =
            new RuleGateAuthorizationResourceFactory();

        Assert.Throws<InvalidOperationException>(
            () => factory.Create(null));
    }

    [Fact]
    public void Create_RejectsUnsupportedResource()
    {
        var factory =
            new RuleGateAuthorizationResourceFactory();

        Assert.Throws<InvalidOperationException>(
            () => factory.Create(new object()));
    }

    [Fact]
    public void Create_ReturnsRuleGateResource()
    {
        var factory =
            new RuleGateAuthorizationResourceFactory();

        var expected =
            new AuthorizationResource(
                type: "document",
                id: "document-1");

        var actual =
            factory.Create(expected);

        Assert.Same(expected, actual);
    }

    [Fact]
    public void Requirement_overload_returns_existing_resource()
    {
        var factory =
            new RuleGateAuthorizationResourceFactory();

        var expected =
            new AuthorizationResource(
                type: "document",
                id: "document-1");

        var actual =
            factory.Create(
                expected,
                new RuleGateAuthorizationRequirement(
                    resourceType: "document",
                    action: "read"));

        Assert.Same(expected, actual);
    }

    [Fact]
    public void HttpContext_resource_uses_matching_route_value()
    {
        var factory =
            new RuleGateAuthorizationResourceFactory();

        var context =
            CreateHttpContext(
                new RuleGateAuthorizationMetadata(
                    resourceType: "document",
                    action: "approve",
                    resourceIdRouteValue: "id"));

        context.Request.RouteValues["id"] =
            42;

        var resource =
            factory.Create(
                context,
                new RuleGateAuthorizationRequirement(
                    resourceType: "document",
                    action: "approve"));

        Assert.Equal(
            "document",
            resource.Type);

        Assert.Equal(
            "42",
            resource.Id);
    }

    [Fact]
    public void HttpContext_resource_allows_missing_route_configuration()
    {
        var factory =
            new RuleGateAuthorizationResourceFactory();

        var context =
            CreateHttpContext(
                new RuleGateAuthorizationMetadata(
                    resourceType: "document",
                    action: "create"));

        var resource =
            factory.Create(
                context,
                new RuleGateAuthorizationRequirement(
                    resourceType: "document",
                    action: "create"));

        Assert.Equal(
            "document",
            resource.Type);

        Assert.Null(resource.Id);
    }

    [Fact]
    public void HttpContext_resource_rejects_missing_endpoint()
    {
        var factory =
            new RuleGateAuthorizationResourceFactory();

        var context =
            new DefaultHttpContext();

        Assert.Throws<InvalidOperationException>(
            () =>
                factory.Create(
                    context,
                    new RuleGateAuthorizationRequirement(
                        resourceType: "document",
                        action: "read")));
    }

    [Fact]
    public void HttpContext_resource_rejects_missing_matching_metadata()
    {
        var factory =
            new RuleGateAuthorizationResourceFactory();

        var context =
            CreateHttpContext(
                new RuleGateAuthorizationMetadata(
                    resourceType: "invoice",
                    action: "read",
                    resourceIdRouteValue: "id"));

        Assert.Throws<InvalidOperationException>(
            () =>
                factory.Create(
                    context,
                    new RuleGateAuthorizationRequirement(
                        resourceType: "document",
                        action: "read")));
    }

    [Fact]
    public void HttpContext_resource_rejects_missing_route_value()
    {
        var factory =
            new RuleGateAuthorizationResourceFactory();

        var context =
            CreateHttpContext(
                new RuleGateAuthorizationMetadata(
                    resourceType: "document",
                    action: "read",
                    resourceIdRouteValue: "id"));

        Assert.Throws<InvalidOperationException>(
            () =>
                factory.Create(
                    context,
                    new RuleGateAuthorizationRequirement(
                        resourceType: "document",
                        action: "read")));
    }

    [Fact]
    public void HttpContext_resource_rejects_empty_route_value()
    {
        var factory =
            new RuleGateAuthorizationResourceFactory();

        var context =
            CreateHttpContext(
                new RuleGateAuthorizationMetadata(
                    resourceType: "document",
                    action: "read",
                    resourceIdRouteValue: "id"));

        context.Request.RouteValues["id"] =
            " ";

        Assert.Throws<InvalidOperationException>(
            () =>
                factory.Create(
                    context,
                    new RuleGateAuthorizationRequirement(
                        resourceType: "document",
                        action: "read")));
    }

    [Fact]
    public void HttpContext_resource_allows_equivalent_metadata()
    {
        var factory =
            new RuleGateAuthorizationResourceFactory();

        var context =
            CreateHttpContext(
                new RuleGateAuthorizationMetadata(
                    resourceType: "document",
                    action: "read",
                    resourceIdRouteValue: "id"),
                new RuleGateAuthorizationMetadata(
                    resourceType: "document",
                    action: "read",
                    resourceIdRouteValue: "id"));

        context.Request.RouteValues["id"] =
            "document-1";

        var resource =
            factory.Create(
                context,
                new RuleGateAuthorizationRequirement(
                    resourceType: "document",
                    action: "read"));

        Assert.Equal(
            "document-1",
            resource.Id);
    }

    [Fact]
    public void HttpContext_resource_rejects_conflicting_metadata()
    {
        var factory =
            new RuleGateAuthorizationResourceFactory();

        var context =
            CreateHttpContext(
                new RuleGateAuthorizationMetadata(
                    resourceType: "document",
                    action: "read",
                    resourceIdRouteValue: "id"),
                new RuleGateAuthorizationMetadata(
                    resourceType: "document",
                    action: "read",
                    resourceIdRouteValue: "documentId"));

        context.Request.RouteValues["id"] =
            "document-1";

        context.Request.RouteValues["documentId"] =
            "document-2";

        Assert.Throws<InvalidOperationException>(
            () =>
                factory.Create(
                    context,
                    new RuleGateAuthorizationRequirement(
                        resourceType: "document",
                        action: "read")));
    }

    private static DefaultHttpContext
        CreateHttpContext(
            params object[] metadata)
    {
        var context =
            new DefaultHttpContext();

        context.SetEndpoint(
            new Endpoint(
                requestDelegate:
                    _ => Task.CompletedTask,
                metadata:
                    new EndpointMetadataCollection(
                        metadata),
                displayName:
                    "RuleGate test endpoint"));

        return context;
    }
}
