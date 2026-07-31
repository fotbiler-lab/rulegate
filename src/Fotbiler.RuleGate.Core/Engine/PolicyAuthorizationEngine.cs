using System.Diagnostics;
using Fotbiler.RuleGate.Abstractions.Authorization;
using Fotbiler.RuleGate.Abstractions.Constants;
using Fotbiler.RuleGate.Abstractions.Diagnostics;
using Fotbiler.RuleGate.Abstractions.Evaluation;
using Fotbiler.RuleGate.Abstractions.Policies;
using Fotbiler.RuleGate.Core.Diagnostics;
using Fotbiler.RuleGate.Core.Evaluation;

namespace Fotbiler.RuleGate.Core.Engine;

public sealed class PolicyAuthorizationEngine
    : IAuthorizationEngine
{
    private readonly IPolicyProvider _policyProvider;

    private readonly IRequirementEvaluationDispatcher
        _requirementDispatcher;

    private readonly IAuthorizationDiagnosticsSink
        _diagnosticsSink;

    public PolicyAuthorizationEngine(
        IPolicyProvider policyProvider,
        IRequirementEvaluationDispatcher
            requirementDispatcher)
        : this(
            policyProvider,
            requirementDispatcher,
            NullAuthorizationDiagnosticsSink.Instance)
    {
    }

    public PolicyAuthorizationEngine(
        IPolicyProvider policyProvider,
        IRequirementEvaluationDispatcher
            requirementDispatcher,
        IAuthorizationDiagnosticsSink diagnosticsSink)
    {
        ArgumentNullException.ThrowIfNull(
            policyProvider);

        ArgumentNullException.ThrowIfNull(
            requirementDispatcher);

        ArgumentNullException.ThrowIfNull(
            diagnosticsSink);

        _policyProvider = policyProvider;
        _requirementDispatcher = requirementDispatcher;
        _diagnosticsSink = diagnosticsSink;
    }

    public ValueTask<AuthorizationDecision>
        EvaluateAsync(
            AuthorizationRequest request,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return _diagnosticsSink.IsEnabled
            ? EvaluateWithDiagnosticsAsync(
                request,
                cancellationToken)
            : EvaluateWithoutDiagnosticsAsync(
                request,
                cancellationToken);
    }

    private async ValueTask<AuthorizationDecision>
        EvaluateWithoutDiagnosticsAsync(
            AuthorizationRequest request,
            CancellationToken cancellationToken)
    {
        using var activity =
            RuleGateInstrumentation
                .StartAuthorizationActivity();

        var startTimestamp =
            Stopwatch.GetTimestamp();

        try
        {
            var policy = await FindPolicyAsync(
                request,
                cancellationToken);

            if (policy is null)
            {
                var missingPolicyDecision =
                    AuthorizationDecision.Deny(
                        new AuthorizationFailure(
                            AuthorizationFailureCodes
                                .NoMatchingPolicy));

                RuleGateInstrumentation.RecordAuthorization(
                    activity,
                    startTimestamp,
                    missingPolicyDecision.IsAllowed,
                    policyMatched: false,
                    requirementOutcome: null);

                return missingPolicyDecision;
            }

            var result =
                await _requirementDispatcher
                    .EvaluateAsync(
                        policy.Requirement,
                        new RequirementEvaluationContext(
                            request),
                        cancellationToken);

            var decision = CreateDecision(result);

            RuleGateInstrumentation.RecordAuthorization(
                activity,
                startTimestamp,
                decision.IsAllowed,
                policyMatched: true,
                requirementOutcome: result.Outcome);

            return decision;
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            RuleGateInstrumentation
                .RecordAuthorizationCancellation(
                    activity,
                    startTimestamp);

            throw;
        }
        catch (Exception)
        {
            RuleGateInstrumentation.RecordAuthorizationError(
                activity,
                startTimestamp);

            throw;
        }
    }

    private async ValueTask<AuthorizationDecision>
        EvaluateWithDiagnosticsAsync(
            AuthorizationRequest request,
            CancellationToken cancellationToken)
    {
        using var activity =
            RuleGateInstrumentation
                .StartAuthorizationActivity();

        var evaluationId = Guid.NewGuid();
        var startTimestamp =
            Stopwatch.GetTimestamp();

        var session =
            new AuthorizationDiagnosticsSession();

        string? policyId = null;

        AuthorizationDecision decision;

        RequirementEvaluationOutcome?
            requirementOutcome = null;

        try
        {
            var policy = await FindPolicyAsync(
                request,
                cancellationToken);

            if (policy is null)
            {
                decision =
                    AuthorizationDecision.Deny(
                        new AuthorizationFailure(
                            AuthorizationFailureCodes
                                .NoMatchingPolicy));
            }
            else
            {
                policyId = policy.Id;

                RequirementEvaluationResult result;

                var context =
                    new RequirementEvaluationContext(
                        request);

                if (_requirementDispatcher is
                    IRequirementEvaluationDiagnosticsDispatcher
                        diagnosticsDispatcher)
                {
                    result =
                        await diagnosticsDispatcher
                            .EvaluateWithDiagnosticsAsync(
                                policy.Requirement,
                                context,
                                session,
                                parentEvaluationId: null,
                                cancellationToken);
                }
                else
                {
                    result =
                        await _requirementDispatcher
                            .EvaluateAsync(
                                policy.Requirement,
                                context,
                                cancellationToken);
                }

                requirementOutcome = result.Outcome;
                decision = CreateDecision(result);
            }

            RuleGateInstrumentation.RecordAuthorization(
                activity,
                startTimestamp,
                decision.IsAllowed,
                policyMatched: policyId is not null,
                requirementOutcome: requirementOutcome);

            activity?.Stop();
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            RuleGateInstrumentation
                .RecordAuthorizationCancellation(
                    activity,
                    startTimestamp);

            throw;
        }
        catch (Exception)
        {
            RuleGateInstrumentation.RecordAuthorizationError(
                activity,
                startTimestamp);

            throw;
        }

        var diagnostic =
            new AuthorizationEvaluationDiagnostic(
                evaluationId,
                policyId,
                decision.IsAllowed,
                Stopwatch.GetElapsedTime(
                    startTimestamp),
                decision.Failures.Select(
                    static failure =>
                        failure.Code),
                session.CreateSnapshot());

        await WriteDiagnosticsSafelyAsync(
            diagnostic);

        return decision;
    }

    private async ValueTask<PolicyDefinition?> FindPolicyAsync(
        AuthorizationRequest request,
        CancellationToken cancellationToken)
    {
        var startTimestamp = Stopwatch.GetTimestamp();

        var policy = await _policyProvider.FindAsync(
            request.Resource.Type,
            request.Action,
            cancellationToken);

        RuleGateInstrumentation.RecordPolicyLookup(
            startTimestamp,
            policy is not null);

        return policy;
    }

    private async ValueTask
        WriteDiagnosticsSafelyAsync(
            AuthorizationEvaluationDiagnostic diagnostic)
    {
        try
        {
            await _diagnosticsSink.WriteAsync(
                diagnostic,
                CancellationToken.None);
        }
        catch (Exception)
        {
            // Diagnostics must not change an authorization
            // decision or make authorization unavailable.
        }
    }

    private static AuthorizationDecision
        CreateDecision(
            RequirementEvaluationResult result)
    {
        return result.IsSatisfied
            ? AuthorizationDecision.Allow()
            : AuthorizationDecision.Deny(
                result.Failures.ToArray());
    }
}
