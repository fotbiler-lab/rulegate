using System.Security.Claims;
using Fotbiler.RuleGate.Abstractions.Authorization;
using Microsoft.AspNetCore.Http;
using RuleGateAuthorizationContext =
    Fotbiler.RuleGate.Abstractions.Authorization.AuthorizationContext;

namespace Fotbiler.RuleGate.AspNetCore.Enrichment;

public sealed class RuleGateAttributeProviderContext
{
    public RuleGateAttributeProviderContext(
        ClaimsPrincipal principal,
        object? frameworkResource,
        AuthorizationSubject subject,
        AuthorizationResource resource,
        string action,
        RuleGateAuthorizationContext context)
    {
        ArgumentNullException.ThrowIfNull(principal);
        ArgumentNullException.ThrowIfNull(subject);
        ArgumentNullException.ThrowIfNull(resource);
        ArgumentException.ThrowIfNullOrWhiteSpace(action);
        ArgumentNullException.ThrowIfNull(context);

        Principal = principal;
        FrameworkResource = frameworkResource;
        Subject = subject;
        Resource = resource;
        Action = action;
        Context = context;
    }

    public ClaimsPrincipal Principal { get; }

    public object? FrameworkResource { get; }

    public HttpContext? HttpContext =>
        FrameworkResource as HttpContext;

    public AuthorizationSubject Subject { get; }

    public AuthorizationResource Resource { get; }

    public string Action { get; }

    public RuleGateAuthorizationContext Context { get; }
}
