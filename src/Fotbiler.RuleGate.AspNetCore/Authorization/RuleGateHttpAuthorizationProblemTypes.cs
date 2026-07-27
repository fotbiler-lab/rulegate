namespace Fotbiler.RuleGate.AspNetCore.Authorization;

public static class RuleGateHttpAuthorizationProblemTypes
{
    public const string AuthenticationRequired =
        "urn:fotbiler:rulegate:authorization:authentication-required";

    public const string AccessForbidden =
        "urn:fotbiler:rulegate:authorization:access-forbidden";
}
