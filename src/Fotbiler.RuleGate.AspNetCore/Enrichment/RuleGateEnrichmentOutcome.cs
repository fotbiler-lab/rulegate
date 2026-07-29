namespace Fotbiler.RuleGate.AspNetCore.Enrichment;

public enum RuleGateEnrichmentOutcome
{
    Succeeded = 0,

    MissingRequiredData = 1,

    ProviderFailed = 2,

    ProviderException = 3,

    AttributeCollision = 4,

    InvalidAttribute = 5,

    Cancelled = 6,
}
