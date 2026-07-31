using Fotbiler.RuleGate.Abstractions.Authorization;
using Fotbiler.RuleGate.Abstractions.Constants;
using Fotbiler.RuleGate.Abstractions.Evaluation;
using Fotbiler.RuleGate.Abstractions.Policies;
using Fotbiler.RuleGate.Core.Diagnostics;

namespace Fotbiler.RuleGate.Core.Evaluation;

public sealed class RequirementEvaluationDispatcher
    : IRequirementEvaluationDispatcher,
      IRequirementEvaluationDiagnosticsDispatcher
{
    private readonly Dictionary<
        Type,
        IRequirementEvaluator> _evaluators;

    public RequirementEvaluationDispatcher(
        IEnumerable<IRequirementEvaluator> evaluators)
    {
        ArgumentNullException.ThrowIfNull(evaluators);

        var evaluatorMap =
            new Dictionary<
                Type,
                IRequirementEvaluator>();

        foreach (var evaluator in evaluators)
        {
            ArgumentNullException.ThrowIfNull(evaluator);

            if (!typeof(RequirementDefinition)
                    .IsAssignableFrom(
                        evaluator.RequirementType))
            {
                throw new ArgumentException(
                    $"Evaluator requirement type '{evaluator.RequirementType}' must derive from RequirementDefinition.",
                    nameof(evaluators));
            }

            if (evaluatorMap.ContainsKey(
                    evaluator.RequirementType))
            {
                throw new InvalidOperationException(
                    $"Multiple evaluators are registered for requirement type '{evaluator.RequirementType.Name}'.");
            }

            evaluatorMap.Add(
                evaluator.RequirementType,
                evaluator);
        }

        _evaluators = evaluatorMap;
    }

    public ValueTask<RequirementEvaluationResult>
        EvaluateAsync(
            RequirementDefinition requirement,
            RequirementEvaluationContext context,
            CancellationToken cancellationToken = default)
    {
        ValidateArguments(
            requirement,
            context,
            cancellationToken);

        if (!_evaluators.TryGetValue(
                requirement.GetType(),
                out var evaluator))
        {
            return ValueTaskCompat.FromResult(
                CreateEvaluatorNotFoundResult(
                    requirement));
        }

        return evaluator.EvaluateAsync(
            requirement,
            context,
            this,
            cancellationToken);
    }

    async ValueTask<RequirementEvaluationResult>
        IRequirementEvaluationDiagnosticsDispatcher
            .EvaluateWithDiagnosticsAsync(
                RequirementDefinition requirement,
                RequirementEvaluationContext context,
                AuthorizationDiagnosticsSession session,
                Guid? parentEvaluationId,
                CancellationToken cancellationToken)
    {
        return await EvaluateWithDiagnosticsAsync(
            requirement,
            context,
            session,
            parentEvaluationId,
            cancellationToken);
    }

    private async ValueTask<RequirementEvaluationResult>
        EvaluateWithDiagnosticsAsync(
            RequirementDefinition requirement,
            RequirementEvaluationContext context,
            AuthorizationDiagnosticsSession session,
            Guid? parentEvaluationId,
            CancellationToken cancellationToken)
    {
        ValidateArguments(
            requirement,
            context,
            cancellationToken);

        ArgumentNullException.ThrowIfNull(session);

        var token =
            session.Begin(
                requirement,
                parentEvaluationId);

        RequirementEvaluationResult result;

        if (!_evaluators.TryGetValue(
                requirement.GetType(),
                out var evaluator))
        {
            result =
                CreateEvaluatorNotFoundResult(
                    requirement);
        }
        else
        {
            var childDispatcher =
                new DiagnosticsChildDispatcher(
                    this,
                    session,
                    token.EvaluationId);

            result =
                await evaluator.EvaluateAsync(
                    requirement,
                    context,
                    childDispatcher,
                    cancellationToken);
        }

        session.Complete(token, result);

        return result;
    }

    private static void ValidateArguments(
        RequirementDefinition requirement,
        RequirementEvaluationContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(requirement);
        ArgumentNullException.ThrowIfNull(context);

        cancellationToken.ThrowIfCancellationRequested();
    }

    private static RequirementEvaluationResult
        CreateEvaluatorNotFoundResult(
            RequirementDefinition requirement)
    {
        return RequirementEvaluationResult
            .Indeterminate(
                new AuthorizationFailure(
                    AuthorizationFailureCodes
                        .RequirementEvaluatorNotFound,
                    requirement.Id));
    }

    private sealed class DiagnosticsChildDispatcher
        : IRequirementEvaluationDispatcher
    {
        private readonly
            RequirementEvaluationDispatcher _dispatcher;

        private readonly
            AuthorizationDiagnosticsSession _session;

        private readonly Guid _parentEvaluationId;

        internal DiagnosticsChildDispatcher(
            RequirementEvaluationDispatcher dispatcher,
            AuthorizationDiagnosticsSession session,
            Guid parentEvaluationId)
        {
            _dispatcher = dispatcher;
            _session = session;
            _parentEvaluationId =
                parentEvaluationId;
        }

        public ValueTask<RequirementEvaluationResult>
            EvaluateAsync(
                RequirementDefinition requirement,
                RequirementEvaluationContext context,
                CancellationToken cancellationToken = default)
        {
            return _dispatcher
                .EvaluateWithDiagnosticsAsync(
                    requirement,
                    context,
                    _session,
                    _parentEvaluationId,
                    cancellationToken);
        }
    }
}
