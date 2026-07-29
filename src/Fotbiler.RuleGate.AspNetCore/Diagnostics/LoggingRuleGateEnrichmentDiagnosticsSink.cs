using Fotbiler.RuleGate.Abstractions.Policies;
using Fotbiler.RuleGate.AspNetCore.Enrichment;
using Microsoft.Extensions.Logging;

namespace Fotbiler.RuleGate.AspNetCore.Diagnostics;

internal sealed partial class
    LoggingRuleGateEnrichmentDiagnosticsSink
    : IRuleGateEnrichmentDiagnosticsSink
{
    private readonly ILogger<
        LoggingRuleGateEnrichmentDiagnosticsSink>
        _logger;

    public LoggingRuleGateEnrichmentDiagnosticsSink(
        ILogger<
            LoggingRuleGateEnrichmentDiagnosticsSink>
            logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        _logger = logger;
    }

    public ValueTask WriteAsync(
        RuleGateEnrichmentDiagnostic diagnostic,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(diagnostic);

        cancellationToken.ThrowIfCancellationRequested();

        if (diagnostic.Outcome ==
            RuleGateEnrichmentOutcome.Succeeded)
        {
            LogProviderSucceeded(
                _logger,
                diagnostic.ProviderName,
                diagnostic.AttributeSource,
                diagnostic.Order,
                diagnostic.CollisionBehavior,
                diagnostic.AttributeCount,
                diagnostic.Duration.TotalMilliseconds);
        }
        else
        {
            LogProviderFailed(
                _logger,
                diagnostic.ProviderName,
                diagnostic.AttributeSource,
                diagnostic.Order,
                diagnostic.CollisionBehavior,
                diagnostic.Outcome,
                diagnostic.AttributeCount,
                diagnostic.Duration.TotalMilliseconds);
        }

        return ValueTask.CompletedTask;
    }

    [LoggerMessage(
        EventId = 2010,
        Level = LogLevel.Debug,
        Message =
            "RuleGate enrichment provider {ProviderName} completed. AttributeSource: {AttributeSource}; Order: {Order}; CollisionBehavior: {CollisionBehavior}; AttributeCount: {AttributeCount}; DurationMs: {DurationMs}.")]
    private static partial void LogProviderSucceeded(
        ILogger logger,
        string providerName,
        AuthorizationAttributeSource attributeSource,
        int order,
        RuleGateAttributeCollisionBehavior collisionBehavior,
        int attributeCount,
        double durationMs);

    [LoggerMessage(
        EventId = 2011,
        Level = LogLevel.Warning,
        Message =
            "RuleGate enrichment provider {ProviderName} failed closed. AttributeSource: {AttributeSource}; Order: {Order}; CollisionBehavior: {CollisionBehavior}; Outcome: {Outcome}; AttributeCount: {AttributeCount}; DurationMs: {DurationMs}.")]
    private static partial void LogProviderFailed(
        ILogger logger,
        string providerName,
        AuthorizationAttributeSource attributeSource,
        int order,
        RuleGateAttributeCollisionBehavior collisionBehavior,
        RuleGateEnrichmentOutcome outcome,
        int attributeCount,
        double durationMs);
}
