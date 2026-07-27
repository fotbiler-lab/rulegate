using Fotbiler.RuleGate.Abstractions.Diagnostics;
using Fotbiler.RuleGate.Abstractions.Evaluation;
using Fotbiler.RuleGate.Abstractions.Policies;
using Microsoft.Extensions.Logging;

namespace Fotbiler.RuleGate.AspNetCore.Diagnostics;

internal sealed partial class
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
                    requirement.AttributeSource);
            }
        }

        return ValueTask.CompletedTask;
    }

    private static string JoinFailureCodes(
        IReadOnlyList<string> failureCodes)
    {
        return failureCodes.Count == 0
            ? string.Empty
            : string.Join(",", failureCodes);
    }

    [LoggerMessage(
        EventId = 2000,
        Level = LogLevel.Information,
        Message =
            "RuleGate authorization evaluation {EvaluationId} completed. PolicyId: {PolicyId}; IsAllowed: {IsAllowed}; DurationMs: {DurationMs}; FailureCodes: {FailureCodes}; RequirementCount: {RequirementCount}.")]
    private static partial void
        LogAuthorizationEvaluation(
            ILogger logger,
            Guid evaluationId,
            string? policyId,
            bool isAllowed,
            double durationMs,
            string failureCodes,
            int requirementCount);

    [LoggerMessage(
        EventId = 2001,
        Level = LogLevel.Debug,
        Message =
            "RuleGate requirement evaluation {RequirementEvaluationId} completed for authorization evaluation {AuthorizationEvaluationId}. ParentEvaluationId: {ParentEvaluationId}; RequirementId: {RequirementId}; RequirementKind: {RequirementKind}; Outcome: {Outcome}; DurationMs: {DurationMs}; FailureCodes: {FailureCodes}; AttributeSource: {AttributeSource}.")]
    private static partial void
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
                attributeSource);
}
