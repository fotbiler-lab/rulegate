namespace Fotbiler.RuleGate.AspNetCore.Enrichment;

public interface IRuleGateEnrichmentDiagnosticsSink
{
    ValueTask WriteAsync(
        RuleGateEnrichmentDiagnostic diagnostic,
        CancellationToken cancellationToken = default);
}
