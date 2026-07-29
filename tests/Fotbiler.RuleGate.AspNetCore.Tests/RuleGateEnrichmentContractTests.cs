using System.Security.Claims;
using Fotbiler.RuleGate.Abstractions.Attributes;
using Fotbiler.RuleGate.Abstractions.Authorization;
using Fotbiler.RuleGate.Abstractions.Policies;
using Fotbiler.RuleGate.AspNetCore.Enrichment;
using Microsoft.AspNetCore.Http;
using RuleGateAuthorizationContext =
    Fotbiler.RuleGate.Abstractions.Authorization.AuthorizationContext;

namespace Fotbiler.RuleGate.AspNetCore.Tests;

public sealed class RuleGateEnrichmentContractTests
{
    [Fact]
    public void Provider_context_preserves_request_state()
    {
        var principal = new ClaimsPrincipal();
        var httpContext = new DefaultHttpContext();
        var subject = new AuthorizationSubject("user-1");
        var resource = new AuthorizationResource(
            "document",
            "document-1");
        var authorizationContext =
            new RuleGateAuthorizationContext(
                DateTimeOffset.UnixEpoch);

        var context =
            new RuleGateAttributeProviderContext(
                principal,
                httpContext,
                subject,
                resource,
                "read",
                authorizationContext);

        Assert.Same(principal, context.Principal);
        Assert.Same(
            httpContext,
            context.FrameworkResource);
        Assert.Same(httpContext, context.HttpContext);
        Assert.Same(subject, context.Subject);
        Assert.Same(resource, context.Resource);
        Assert.Equal("read", context.Action);
        Assert.Same(
            authorizationContext,
            context.Context);
    }

    [Fact]
    public void Provider_results_expose_closed_statuses()
    {
        var attributes = new AuthorizationAttributes(
        [
            new KeyValuePair<string, object?>(
                "tenant",
                "tenant-1"),
        ]);

        var success =
            RuleGateAttributeProviderResult.Success(
                attributes);

        var missing =
            RuleGateAttributeProviderResult
                .MissingRequiredData();

        var failure =
            RuleGateAttributeProviderResult.Fail();

        Assert.True(success.IsSuccessful);
        Assert.Same(attributes, success.Attributes);

        Assert.False(missing.IsSuccessful);
        Assert.Equal(
            RuleGateAttributeProviderResultStatus
                .MissingRequiredData,
            missing.Status);

        Assert.False(failure.IsSuccessful);
        Assert.Equal(
            RuleGateAttributeProviderResultStatus.Failed,
            failure.Status);
    }

    [Fact]
    public void Enrichment_result_requires_success_request()
    {
        var request = new AuthorizationRequest(
            new AuthorizationSubject("user-1"),
            new AuthorizationResource("document"),
            "read",
            new RuleGateAuthorizationContext(
                DateTimeOffset.UnixEpoch));

        var success =
            RuleGateAuthorizationRequestEnrichmentResult
                .Success(request);

        var failure =
            RuleGateAuthorizationRequestEnrichmentResult
                .Fail();

        Assert.True(success.IsSuccessful);
        Assert.Same(request, success.Request);
        Assert.False(failure.IsSuccessful);
        Assert.Null(failure.Request);
    }

    [Fact]
    public void Diagnostic_exposes_only_safe_metadata()
    {
        var diagnostic =
            new RuleGateEnrichmentDiagnostic(
                AuthorizationAttributeSource.Subject,
                "SampleTenantProvider",
                order: 10,
                RuleGateAttributeCollisionBehavior
                    .KeepExisting,
                RuleGateEnrichmentOutcome.Succeeded,
                attributeCount: 2,
                TimeSpan.FromMilliseconds(5));

        Assert.Equal(
            AuthorizationAttributeSource.Subject,
            diagnostic.AttributeSource);
        Assert.Equal(
            "SampleTenantProvider",
            diagnostic.ProviderName);
        Assert.Equal(10, diagnostic.Order);
        Assert.Equal(
            RuleGateAttributeCollisionBehavior
                .KeepExisting,
            diagnostic.CollisionBehavior);
        Assert.Equal(
            RuleGateEnrichmentOutcome.Succeeded,
            diagnostic.Outcome);
        Assert.Equal(2, diagnostic.AttributeCount);
        Assert.Equal(
            TimeSpan.FromMilliseconds(5),
            diagnostic.Duration);
    }

    [Fact]
    public void Diagnostic_rejects_invalid_metadata()
    {
        Assert.ThrowsAny<ArgumentException>(
            () => new RuleGateEnrichmentDiagnostic(
                AuthorizationAttributeSource.Context,
                providerName: " ",
                order: 0,
                RuleGateAttributeCollisionBehavior.Fail,
                RuleGateEnrichmentOutcome.ProviderFailed,
                attributeCount: 0,
                TimeSpan.Zero));

        Assert.Throws<ArgumentOutOfRangeException>(
            () => new RuleGateEnrichmentDiagnostic(
                AuthorizationAttributeSource.Context,
                providerName: "Provider",
                order: 0,
                RuleGateAttributeCollisionBehavior.Fail,
                RuleGateEnrichmentOutcome.ProviderFailed,
                attributeCount: -1,
                TimeSpan.Zero));
    }
}
