namespace Fotbiler.RuleGate.Abstractions.Authorization;

public sealed class AuthorizationDecision
{
    private AuthorizationDecision(
        bool isAllowed,
        IReadOnlyList<AuthorizationFailure> failures)
    {
        IsAllowed = isAllowed;
        Failures = failures;
    }

    public bool IsAllowed { get; }

    public IReadOnlyList<AuthorizationFailure> Failures { get; }

    public static AuthorizationDecision Allow()
    {
        return new AuthorizationDecision(
            isAllowed: true,
            failures: Array.Empty<AuthorizationFailure>());
    }

    public static AuthorizationDecision Deny(
        params AuthorizationFailure[] failures)
    {
        ArgumentNullException.ThrowIfNull(failures);

        if (failures.Length == 0)
        {
            throw new ArgumentException(
                "A denied authorization decision must contain at least one failure.",
                nameof(failures));
        }

        return new AuthorizationDecision(
            isAllowed: false,
            failures: Array.AsReadOnly(failures.ToArray()));
    }
}
