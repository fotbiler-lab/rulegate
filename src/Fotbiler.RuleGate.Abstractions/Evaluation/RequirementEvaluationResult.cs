using Fotbiler.RuleGate.Abstractions.Authorization;

namespace Fotbiler.RuleGate.Abstractions.Evaluation;

public sealed class RequirementEvaluationResult
{
    private RequirementEvaluationResult(
        RequirementEvaluationOutcome outcome,
        IReadOnlyList<AuthorizationFailure> failures)
    {
        Outcome = outcome;
        Failures = failures;
    }

    public RequirementEvaluationOutcome Outcome { get; }

    public bool IsSatisfied =>
        Outcome == RequirementEvaluationOutcome.Satisfied;

    public bool IsNotSatisfied =>
        Outcome == RequirementEvaluationOutcome.NotSatisfied;

    public bool IsIndeterminate =>
        Outcome == RequirementEvaluationOutcome.Indeterminate;

    public IReadOnlyList<AuthorizationFailure> Failures { get; }

    public static RequirementEvaluationResult Satisfied()
    {
        return new RequirementEvaluationResult(
            RequirementEvaluationOutcome.Satisfied,
            Array.Empty<AuthorizationFailure>());
    }

    public static RequirementEvaluationResult NotSatisfied(
        params AuthorizationFailure[] failures)
    {
        return CreateFailureResult(
            RequirementEvaluationOutcome.NotSatisfied,
            failures);
    }

    public static RequirementEvaluationResult Indeterminate(
        params AuthorizationFailure[] failures)
    {
        return CreateFailureResult(
            RequirementEvaluationOutcome.Indeterminate,
            failures);
    }

    private static RequirementEvaluationResult CreateFailureResult(
        RequirementEvaluationOutcome outcome,
        AuthorizationFailure[] failures)
    {
        ArgumentNullException.ThrowIfNull(failures);

        if (failures.Length == 0)
        {
            throw new ArgumentException(
                "A non-successful requirement evaluation must contain at least one failure.",
                nameof(failures));
        }

        if (failures.Any(static failure => failure is null))
        {
            throw new ArgumentException(
                "Requirement evaluation failures cannot contain null values.",
                nameof(failures));
        }

        return new RequirementEvaluationResult(
            outcome,
            Array.AsReadOnly(failures.ToArray()));
    }
}
