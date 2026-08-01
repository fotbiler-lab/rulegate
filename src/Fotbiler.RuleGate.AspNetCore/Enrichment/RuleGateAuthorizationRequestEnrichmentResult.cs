using Fotbiler.RuleGate.Abstractions.Authorization;

namespace Fotbiler.RuleGate.AspNetCore.Enrichment;

public sealed class
    RuleGateAuthorizationRequestEnrichmentResult
{
    private RuleGateAuthorizationRequestEnrichmentResult(
        AuthorizationRequest? request)
    {
        Request = request;
    }

    [System.Diagnostics.CodeAnalysis.MemberNotNullWhen(
        true,
        nameof(Request))]
    public bool IsSuccessful => Request is not null;

    public AuthorizationRequest? Request { get; }

    public static
        RuleGateAuthorizationRequestEnrichmentResult
        Success(AuthorizationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new(
            request);
    }

    public static
        RuleGateAuthorizationRequestEnrichmentResult
        Fail()
    {
        return new(
            request: null);
    }
}
