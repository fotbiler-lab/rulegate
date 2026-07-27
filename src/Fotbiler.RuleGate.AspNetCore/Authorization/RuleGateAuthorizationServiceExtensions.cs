using System.Security.Claims;
using Fotbiler.RuleGate.Abstractions.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace Fotbiler.RuleGate.AspNetCore.Authorization;

public static class
    RuleGateAuthorizationServiceExtensions
{
    public static Task<AuthorizationResult>
        AuthorizeRuleGateAsync(
            this IAuthorizationService
                authorizationService,
            ClaimsPrincipal user,
            AuthorizationResource resource,
            string action)
    {
        ArgumentNullException.ThrowIfNull(
            authorizationService);

        ArgumentNullException.ThrowIfNull(
            user);

        ArgumentNullException.ThrowIfNull(
            resource);

        return authorizationService
            .AuthorizeRuleGateAsync(
                user,
                resource,
                resourceType: resource.Type,
                action);
    }

    public static Task<AuthorizationResult>
        AuthorizeRuleGateAsync(
            this IAuthorizationService
                authorizationService,
            ClaimsPrincipal user,
            object resource,
            string resourceType,
            string action)
    {
        ArgumentNullException.ThrowIfNull(
            authorizationService);

        ArgumentNullException.ThrowIfNull(
            user);

        ArgumentNullException.ThrowIfNull(
            resource);

        var policyName =
            new RuleGatePolicyName(
                resourceType,
                action);

        return authorizationService
            .AuthorizeAsync(
                user,
                resource,
                policyName.ToString());
    }
}
