using Fotbiler.RuleGate.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.Extensions.Options;

namespace Fotbiler.RuleGate.AspNetCore.Tests;

public sealed class
    RuleGateAuthorizationPolicyProviderTests
{
    [Fact]
    public async Task Valid_rulegate_name_creates_policy()
    {
        var provider =
            CreateProvider();

        var policy =
            await provider.GetPolicyAsync(
                "RuleGate:document:read");

        Assert.NotNull(policy);

        Assert.Contains(
            policy.Requirements,
            requirement =>
                requirement is
                    DenyAnonymousAuthorizationRequirement);

        var requirement =
            Assert.Single(
                policy.Requirements
                    .OfType<
                        RuleGateAuthorizationRequirement>());

        Assert.Equal(
            "document",
            requirement.ResourceType);

        Assert.Equal(
            "read",
            requirement.Action);
    }

    [Fact]
    public async Task Non_rulegate_name_uses_fallback_provider()
    {
        var expectedPolicy =
            new AuthorizationPolicyBuilder()
                .RequireClaim("existing-claim")
                .Build();

        var options =
            new AuthorizationOptions();

        options.AddPolicy(
            "existing-policy",
            expectedPolicy);

        var provider =
            CreateProvider(options);

        var actualPolicy =
            await provider.GetPolicyAsync(
                "existing-policy");

        Assert.Same(
            expectedPolicy,
            actualPolicy);
    }

    [Theory]
    [InlineData("RuleGate:")]
    [InlineData("RuleGate:document")]
    [InlineData("RuleGate::read")]
    [InlineData("RuleGate:document:")]
    [InlineData("RuleGate:document:read:extra")]
    [InlineData("RuleGate:document type:read")]
    public async Task Malformed_owned_name_does_not_use_fallback(
        string policyName)
    {
        var fallbackPolicy =
            new AuthorizationPolicyBuilder()
                .RequireClaim("fallback-claim")
                .Build();

        var options =
            new AuthorizationOptions();

        options.AddPolicy(
            policyName,
            fallbackPolicy);

        var provider =
            CreateProvider(options);

        var actualPolicy =
            await provider.GetPolicyAsync(
                policyName);

        Assert.Null(actualPolicy);
    }

    [Fact]
    public async Task Default_policy_uses_fallback_provider()
    {
        var expectedPolicy =
            new AuthorizationPolicyBuilder()
                .RequireClaim("default-claim")
                .Build();

        var options =
            new AuthorizationOptions
            {
                DefaultPolicy =
                    expectedPolicy,
            };

        var provider =
            CreateProvider(options);

        var actualPolicy =
            await provider
                .GetDefaultPolicyAsync();

        Assert.Same(
            expectedPolicy,
            actualPolicy);
    }

    [Fact]
    public async Task Fallback_policy_uses_fallback_provider()
    {
        var expectedPolicy =
            new AuthorizationPolicyBuilder()
                .RequireClaim("fallback-claim")
                .Build();

        var options =
            new AuthorizationOptions
            {
                FallbackPolicy =
                    expectedPolicy,
            };

        var provider =
            CreateProvider(options);

        var actualPolicy =
            await provider
                .GetFallbackPolicyAsync();

        Assert.Same(
            expectedPolicy,
            actualPolicy);
    }

    [Fact]
    public void Provider_allows_policy_caching()
    {
        var provider =
            CreateProvider();

        Assert.True(
            provider.AllowsCachingPolicies);
    }

    private static
        RuleGateAuthorizationPolicyProvider
        CreateProvider(
            AuthorizationOptions? options = null)
    {
        return new RuleGateAuthorizationPolicyProvider(
            Options.Create(
                options
                ?? new AuthorizationOptions()));
    }
}
