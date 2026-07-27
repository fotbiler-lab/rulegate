namespace Fotbiler.RuleGate.Abstractions.Diagnostics;

public sealed record AuthorizationEvaluationDiagnostic
{
    public AuthorizationEvaluationDiagnostic(
        Guid evaluationId,
        string? policyId,
        bool isAllowed,
        TimeSpan duration,
        IEnumerable<string> failureCodes,
        IEnumerable<RequirementEvaluationDiagnostic>
            requirementEvaluations)
    {
        if (evaluationId == Guid.Empty)
        {
            throw new ArgumentException(
                "An authorization evaluation identifier cannot be empty.",
                nameof(evaluationId));
        }

        if (policyId is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(
                policyId);
        }

        if (duration < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(duration),
                duration,
                "An authorization evaluation duration cannot be negative.");
        }

        ArgumentNullException.ThrowIfNull(
            failureCodes);

        ArgumentNullException.ThrowIfNull(
            requirementEvaluations);

        var copiedFailureCodes =
            failureCodes.ToArray();

        if (copiedFailureCodes.Any(
                static code =>
                    string.IsNullOrWhiteSpace(code)))
        {
            throw new ArgumentException(
                "Authorization diagnostic failure codes cannot contain null or whitespace values.",
                nameof(failureCodes));
        }

        var copiedRequirementEvaluations =
            requirementEvaluations.ToArray();

        if (copiedRequirementEvaluations.Any(
                static diagnostic =>
                    diagnostic is null))
        {
            throw new ArgumentException(
                "Authorization diagnostics cannot contain null requirement evaluations.",
                nameof(requirementEvaluations));
        }

        EvaluationId = evaluationId;
        PolicyId = policyId;
        IsAllowed = isAllowed;
        Duration = duration;
        FailureCodes =
            Array.AsReadOnly(copiedFailureCodes);
        RequirementEvaluations =
            Array.AsReadOnly(
                copiedRequirementEvaluations);
    }

    public Guid EvaluationId { get; }

    public string? PolicyId { get; }

    public bool IsAllowed { get; }

    public TimeSpan Duration { get; }

    public IReadOnlyList<string> FailureCodes { get; }

    public IReadOnlyList<RequirementEvaluationDiagnostic>
        RequirementEvaluations
    {
        get;
    }
}
