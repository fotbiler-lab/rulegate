namespace Fotbiler.RuleGate.Abstractions.Authorization;

public sealed class AuthorizationRequest
{
    public AuthorizationRequest(
        AuthorizationSubject subject,
        AuthorizationResource resource,
        string action,
        AuthorizationContext context)
    {
        ArgumentNullException.ThrowIfNull(subject);
        ArgumentNullException.ThrowIfNull(resource);
        ArgumentException.ThrowIfNullOrWhiteSpace(action);
        ArgumentNullException.ThrowIfNull(context);

        Subject = subject;
        Resource = resource;
        Action = action;
        Context = context;
    }

    public AuthorizationSubject Subject { get; }

    public AuthorizationResource Resource { get; }

    public string Action { get; }

    public AuthorizationContext Context { get; }
}
