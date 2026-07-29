using Fotbiler.RuleGate.Abstractions.Evaluation;
using Fotbiler.RuleGate.Abstractions.Policies;

namespace Fotbiler.RuleGate.Abstractions.Diagnostics;

public sealed record RequirementEvaluationDiagnostic
{
    public RequirementEvaluationDiagnostic(
        Guid evaluationId,
        Guid? parentEvaluationId,
        string? requirementId,
        AuthorizationRequirementKind requirementKind,
        RequirementEvaluationOutcome outcome,
        TimeSpan duration,
        IEnumerable<string> failureCodes,
        AuthorizationAttributeSource? attributeSource = null,
        string? attributeName = null,
        AuthorizationAttributeSource?
            comparedAttributeSource = null,
        string? comparedAttributeName = null)
    {
        if (evaluationId == Guid.Empty)
        {
            throw new ArgumentException(
                "A requirement evaluation identifier cannot be empty.",
                nameof(evaluationId));
        }

        if (parentEvaluationId == Guid.Empty)
        {
            throw new ArgumentException(
                "A parent requirement evaluation identifier cannot be empty.",
                nameof(parentEvaluationId));
        }

        if (requirementId is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(
                requirementId);
        }

        if (!Enum.IsDefined(requirementKind))
        {
            throw new ArgumentOutOfRangeException(
                nameof(requirementKind),
                requirementKind,
                "The authorization requirement kind is not supported.");
        }

        if (!Enum.IsDefined(outcome))
        {
            throw new ArgumentOutOfRangeException(
                nameof(outcome),
                outcome,
                "The requirement evaluation outcome is not supported.");
        }

        if (duration < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(duration),
                duration,
                "A requirement evaluation duration cannot be negative.");
        }

        ArgumentNullException.ThrowIfNull(
            failureCodes);

        var copiedFailureCodes =
            failureCodes.ToArray();

        if (copiedFailureCodes.Any(
                static code =>
                    string.IsNullOrWhiteSpace(code)))
        {
            throw new ArgumentException(
                "Requirement diagnostic failure codes cannot contain null or whitespace values.",
                nameof(failureCodes));
        }

        if (attributeSource is not null &&
            !Enum.IsDefined(attributeSource.Value))
        {
            throw new ArgumentOutOfRangeException(
                nameof(attributeSource),
                attributeSource,
                "The authorization attribute source is not supported.");
        }

        if (attributeName is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(
                attributeName);
        }

        if (comparedAttributeSource is not null &&
            !Enum.IsDefined(
                comparedAttributeSource.Value))
        {
            throw new ArgumentOutOfRangeException(
                nameof(comparedAttributeSource),
                comparedAttributeSource,
                "The compared authorization attribute source is not supported.");
        }

        if (comparedAttributeName is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(
                comparedAttributeName);
        }

        EvaluationId = evaluationId;
        ParentEvaluationId = parentEvaluationId;
        RequirementId = requirementId;
        RequirementKind = requirementKind;
        Outcome = outcome;
        Duration = duration;
        FailureCodes =
            Array.AsReadOnly(copiedFailureCodes);
        AttributeSource = attributeSource;
        AttributeName = attributeName;
        ComparedAttributeSource =
            comparedAttributeSource;
        ComparedAttributeName =
            comparedAttributeName;
    }

    public Guid EvaluationId { get; }

    public Guid? ParentEvaluationId { get; }

    public string? RequirementId { get; }

    public AuthorizationRequirementKind RequirementKind
    {
        get;
    }

    public RequirementEvaluationOutcome Outcome { get; }

    public TimeSpan Duration { get; }

    public IReadOnlyList<string> FailureCodes { get; }

    public AuthorizationAttributeSource? AttributeSource
    {
        get;
    }

    public string? AttributeName { get; }

    public AuthorizationAttributeSource?
        ComparedAttributeSource
    {
        get;
    }

    public string? ComparedAttributeName { get; }
}
