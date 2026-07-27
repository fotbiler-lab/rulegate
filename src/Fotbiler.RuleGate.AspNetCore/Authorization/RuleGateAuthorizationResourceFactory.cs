using System.Globalization;
using Fotbiler.RuleGate.Abstractions.Authorization;
using Microsoft.AspNetCore.Http;

namespace Fotbiler.RuleGate.AspNetCore.Authorization;

public sealed class RuleGateAuthorizationResourceFactory
    : IRuleGateAuthorizationResourceFactory
{
    public AuthorizationResource Create(
        object? resource)
    {
        if (resource is AuthorizationResource
            authorizationResource)
        {
            return authorizationResource;
        }

        throw new InvalidOperationException(
            "The ASP.NET Core authorization resource must be a RuleGate AuthorizationResource.");
    }

    public AuthorizationResource Create(
        object? resource,
        RuleGateAuthorizationRequirement requirement)
    {
        ArgumentNullException.ThrowIfNull(requirement);

        if (resource is AuthorizationResource
            authorizationResource)
        {
            return authorizationResource;
        }

        if (resource is HttpContext httpContext)
        {
            return CreateFromHttpContext(
                httpContext,
                requirement);
        }

        return Create(resource);
    }

    private static AuthorizationResource
        CreateFromHttpContext(
            HttpContext httpContext,
            RuleGateAuthorizationRequirement
                requirement)
    {
        if (requirement.ResourceType is null)
        {
            throw new InvalidOperationException(
                "HTTP endpoint authorization requires a RuleGate resource type.");
        }

        var endpoint =
            httpContext.GetEndpoint()
            ?? throw new InvalidOperationException(
                "The current HTTP request does not have an endpoint.");

        var matchingMetadata =
            endpoint.Metadata
                .OfType<
                    IRuleGateAuthorizationMetadata>()
                .Where(
                    metadata =>
                        string.Equals(
                            metadata.ResourceType,
                            requirement.ResourceType,
                            StringComparison.Ordinal) &&
                        string.Equals(
                            metadata.Action,
                            requirement.Action,
                            StringComparison.Ordinal))
                .ToArray();

        if (matchingMetadata.Length == 0)
        {
            throw new InvalidOperationException(
                "The endpoint does not contain RuleGate metadata matching the current authorization requirement.");
        }

        var resourceIdRouteValue =
            matchingMetadata[0]
                .ResourceIdRouteValue;

        if (matchingMetadata.Any(
                metadata =>
                    !string.Equals(
                        metadata.ResourceIdRouteValue,
                        resourceIdRouteValue,
                        StringComparison.Ordinal)))
        {
            throw new InvalidOperationException(
                "The endpoint contains conflicting RuleGate resource route-value metadata.");
        }

        if (resourceIdRouteValue is null)
        {
            return new AuthorizationResource(
                requirement.ResourceType);
        }

        if (!httpContext.Request.RouteValues
                .TryGetValue(
                    resourceIdRouteValue,
                    out var routeValue) ||
            routeValue is null)
        {
            throw new InvalidOperationException(
                $"The route value '{resourceIdRouteValue}' required by RuleGate authorization is missing.");
        }

        var resourceId =
            Convert.ToString(
                routeValue,
                CultureInfo.InvariantCulture);

        if (string.IsNullOrWhiteSpace(resourceId))
        {
            throw new InvalidOperationException(
                $"The route value '{resourceIdRouteValue}' required by RuleGate authorization is empty.");
        }

        return new AuthorizationResource(
            type: requirement.ResourceType,
            id: resourceId);
    }
}
