using System.Security.Claims;
using Fotbiler.RuleGate.Abstractions.Authorization;

namespace Fotbiler.RuleGate.AspNetCore.Enrichment;

public interface IRuleGateAuthorizationRequestEnricher
{
    ValueTask<
        RuleGateAuthorizationRequestEnrichmentResult>
        EnrichAsync(
            AuthorizationRequest request,
            ClaimsPrincipal principal,
            object? frameworkResource,
            CancellationToken cancellationToken = default);
}
