using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Fotbiler.RuleGate.AspNetCore.Authorization;

public sealed class RuleGateAuthorizationPolicyProvider
    : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider
        _fallbackProvider;

    public RuleGateAuthorizationPolicyProvider(
        IOptions<AuthorizationOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _fallbackProvider =
            new DefaultAuthorizationPolicyProvider(
                options);
    }

    public bool AllowsCachingPolicies => true;

    public Task<AuthorizationPolicy?>
        GetPolicyAsync(
            string policyName)
    {
        ArgumentNullException.ThrowIfNull(
            policyName);

        if (RuleGatePolicyName.TryParse(
                policyName,
                out var ruleGatePolicyName))
        {
            var policy =
                new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .AddRequirements(
                        new RuleGateAuthorizationRequirement(
                            resourceType:
                                ruleGatePolicyName
                                    .ResourceType,
                            action:
                                ruleGatePolicyName
                                    .Action))
                    .Build();

            return Task.FromResult<
                AuthorizationPolicy?>(
                    policy);
        }

        if (RuleGatePolicyName.HasPrefix(
                policyName))
        {
            return Task.FromResult<
                AuthorizationPolicy?>(
                    null);
        }

        return _fallbackProvider
            .GetPolicyAsync(policyName);
    }

    public Task<AuthorizationPolicy>
        GetDefaultPolicyAsync()
    {
        return _fallbackProvider
            .GetDefaultPolicyAsync();
    }

    public Task<AuthorizationPolicy?>
        GetFallbackPolicyAsync()
    {
        return _fallbackProvider
            .GetFallbackPolicyAsync();
    }
}
