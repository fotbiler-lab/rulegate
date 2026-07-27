using Fotbiler.RuleGate.Abstractions.Evaluation;
using Fotbiler.RuleGate.Abstractions.Policies;
using Fotbiler.RuleGate.Core.Diagnostics;

namespace Fotbiler.RuleGate.Core.Evaluation;

internal interface
    IRequirementEvaluationDiagnosticsDispatcher
{
    ValueTask<RequirementEvaluationResult>
        EvaluateWithDiagnosticsAsync(
            RequirementDefinition requirement,
            RequirementEvaluationContext context,
            AuthorizationDiagnosticsSession session,
            Guid? parentEvaluationId,
            CancellationToken cancellationToken);
}
