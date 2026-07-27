using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Fotbiler.RuleGate.AspNetCore.Authorization;

internal sealed class
    RuleGateAuthorizationMiddlewareResultHandler
    : IAuthorizationMiddlewareResultHandler
{
    private const string ProblemContentType =
        "application/problem+json; charset=utf-8";

    private static readonly JsonSerializerOptions
        JsonOptions =
            new(JsonSerializerDefaults.Web);

    private readonly AuthorizationMiddlewareResultHandler
        _defaultHandler = new();

    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentNullException.ThrowIfNull(authorizeResult);

        if (authorizeResult.Succeeded ||
            !ContainsRuleGateRequirement(policy))
        {
            await _defaultHandler.HandleAsync(
                next,
                context,
                policy,
                authorizeResult);

            return;
        }

        await _defaultHandler.HandleAsync(
            next,
            context,
            policy,
            authorizeResult);

        if (context.Response.HasStarted)
        {
            return;
        }

        if (authorizeResult.Challenged &&
            context.Response.StatusCode ==
            StatusCodes.Status401Unauthorized)
        {
            await WriteProblemAsync(
                context,
                status:
                    StatusCodes.Status401Unauthorized,
                type:
                    RuleGateHttpAuthorizationProblemTypes
                        .AuthenticationRequired,
                title:
                    "Authentication is required.",
                detail:
                    "The request requires an authenticated identity.",
                code:
                    RuleGateHttpAuthorizationProblemCodes
                        .AuthenticationRequired);

            return;
        }

        if (authorizeResult.Forbidden &&
            context.Response.StatusCode ==
            StatusCodes.Status403Forbidden)
        {
            await WriteProblemAsync(
                context,
                status:
                    StatusCodes.Status403Forbidden,
                type:
                    RuleGateHttpAuthorizationProblemTypes
                        .AccessForbidden,
                title:
                    "Access is forbidden.",
                detail:
                    "The authenticated identity is not authorized to access this resource.",
                code:
                    RuleGateHttpAuthorizationProblemCodes
                        .AccessForbidden);
        }
    }

    private static bool ContainsRuleGateRequirement(
        AuthorizationPolicy policy)
    {
        return policy.Requirements.Any(
            static requirement =>
                requirement is
                    RuleGateAuthorizationRequirement);
    }

    private static Task WriteProblemAsync(
        HttpContext context,
        int status,
        string type,
        string title,
        string detail,
        string code)
    {
        var response = context.Response;

        response.StatusCode = status;
        response.ContentLength = null;

        var problem =
            new ProblemDetails
            {
                Type = type,
                Title = title,
                Status = status,
                Detail = detail,
            };

        problem.Extensions["code"] = code;

        problem.Extensions["traceId"] =
            Activity.Current?.Id ??
            context.TraceIdentifier;

        return response.WriteAsJsonAsync(
            problem,
            JsonOptions,
            ProblemContentType,
            context.RequestAborted);
    }
}
