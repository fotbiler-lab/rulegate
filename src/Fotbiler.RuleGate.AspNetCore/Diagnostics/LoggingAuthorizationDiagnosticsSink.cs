using Fotbiler.RuleGate.Abstractions.Diagnostics;
using Fotbiler.RuleGate.Abstractions.Evaluation;
using Fotbiler.RuleGate.Abstractions.Policies;
using Microsoft.Extensions.Logging;

namespace Fotbiler.RuleGate.AspNetCore.Diagnostics;

internal sealed class
    LoggingAuthorizationDiagnosticsSink
    : IAuthorizationDiagnosticsSink
{
    private readonly ILogger<
        LoggingAuthorizationDiagnosticsSink> _logger;

    public LoggingAuthorizationDiagnosticsSink(
        ILogger<LoggingAuthorizationDiagnosticsSink>
            logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        _logger = logger;
    }

    public bool IsEnabled =>
        _logger.IsEnabled(LogLevel.Information) ||
        _logger.IsEnabled(LogLevel.Debug);

    public ValueTask WriteAsync(
        AuthorizationEvaluationDiagnostic diagnostic,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(diagnostic);

        cancellationToken.ThrowIfCancellationRequested();

        if (_logger.IsEnabled(LogLevel.Information))
        {
            LogAuthorizationEvaluation(
                _logger,
                diagnostic.EvaluationId,
                diagnostic.PolicyId,
                diagnostic.IsAllowed,
                diagnostic.Duration.TotalMilliseconds,
                JoinFailureCodes(
                    diagnostic.FailureCodes),
                diagnostic.RequirementEvaluations.Count);
        }

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            foreach (var requirement in
                     diagnostic.RequirementEvaluations)
            {
                LogRequirementEvaluation(
                    _logger,
                    diagnostic.EvaluationId,
                    requirement.EvaluationId,
                    requirement.ParentEvaluationId,
                    requirement.RequirementId,
                    requirement.RequirementKind,
                    requirement.Outcome,
                    requirement.Duration.TotalMilliseconds,
                    JoinFailureCodes(
                        requirement.FailureCodes),
                    requirement.AttributeSource,
                    requirement.ComparedAttributeSource);
            }
        }

        return default;
    }

    private static string JoinFailureCodes(
        IReadOnlyList<string> failureCodes)
    {
        return failureCodes.Count == 0
            ? string.Empty
            : string.Join(",", failureCodes);
    }

    private static void
        LogAuthorizationEvaluation(
            ILogger logger,
            Guid evaluationId,
            string? policyId,
            bool isAllowed,
            double durationMs,
            string failureCodes,
            int requirementCount)
    {
        logger.LogInformation(
            new EventId(
                2000,
                nameof(LogAuthorizationEvaluation)),
            "RuleGate authorization evaluation {EvaluationId} completed. PolicyId: {PolicyId}; IsAllowed: {IsAllowed}; DurationMs: {DurationMs}; FailureCodes: {FailureCodes}; RequirementCount: {RequirementCount}.",
            evaluationId,
            policyId,
            isAllowed,
            durationMs,
            failureCodes,
            requirementCount);
    }

    private static void
        LogRequirementEvaluation(
            ILogger logger,
            Guid authorizationEvaluationId,
            Guid requirementEvaluationId,
            Guid? parentEvaluationId,
            string? requirementId,
            AuthorizationRequirementKind requirementKind,
            RequirementEvaluationOutcome outcome,
            double durationMs,
            string failureCodes,
            AuthorizationAttributeSource?
                attributeSource,
            AuthorizationAttributeSource?
                comparedAttributeSource)
    {
        logger.LogDebug(
            new EventId(
                2001,
                nameof(LogRequirementEvaluation)),
            "RuleGate requirement evaluation {RequirementEvaluationId} completed for authorization evaluation {AuthorizationEvaluationId}. ParentEvaluationId: {ParentEvaluationId}; RequirementId: {RequirementId}; RequirementKind: {RequirementKind}; Outcome: {Outcome}; DurationMs: {DurationMs}; FailureCodes: {FailureCodes}; AttributeSource: {AttributeSource}; ComparedAttributeSource: {ComparedAttributeSource}.",
            requirementEvaluationId,
            authorizationEvaluationId,
            parentEvaluationId,
            requirementId,
            requirementKind,
            outcome,
            durationMs,
            failureCodes,
            attributeSource,
            comparedAttributeSource);
    }
}
