using Fotbiler.RuleGate.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;

namespace Fotbiler.RuleGate.AspNetCore.Endpoints;

public static class
    RuleGateEndpointConventionBuilderExtensions
{
    public static TBuilder RequireRuleGate<TBuilder>(
        this TBuilder builder,
        string resourceType,
        string action,
        string? resourceIdRouteValue = null)
        where TBuilder : IEndpointConventionBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        var metadata =
            new RuleGateAuthorizationMetadata(
                resourceType,
                action,
                resourceIdRouteValue);

        builder.Add(
            endpointBuilder =>
                endpointBuilder.Metadata.Add(
                    metadata));

        builder.RequireAuthorization(
            metadata.PolicyName);

        return builder;
    }
}
