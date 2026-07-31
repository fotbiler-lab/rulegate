using Fotbiler.RuleGate.Core.Evaluation;
using Fotbiler.RuleGate.Core.Evaluation.Evaluators;

namespace Fotbiler.RuleGate.Cli.Evaluation;

internal static class RequirementEvaluationDispatcherFactory
{
    public static RequirementEvaluationDispatcher Create()
    {
        return new RequirementEvaluationDispatcher(
        [
            new PermissionRequirementEvaluator(),
            new RoleRequirementEvaluator(),
            new AttributeRequirementEvaluator(),
            new AttributeComparisonRequirementEvaluator(),
            new TimeWindowRequirementEvaluator(),
            new DateTimeWindowRequirementEvaluator(),
            new ContextAgeRequirementEvaluator(),
            new ContextRequirementEvaluator(),
            new AllRequirementEvaluator(),
            new AnyRequirementEvaluator(),
            new NotRequirementEvaluator()
        ]);
    }
}
