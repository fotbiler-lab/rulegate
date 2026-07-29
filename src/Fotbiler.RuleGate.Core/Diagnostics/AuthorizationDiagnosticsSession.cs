using System.Diagnostics;
using Fotbiler.RuleGate.Abstractions.Diagnostics;
using Fotbiler.RuleGate.Abstractions.Evaluation;
using Fotbiler.RuleGate.Abstractions.Policies;

namespace Fotbiler.RuleGate.Core.Diagnostics;

internal sealed class AuthorizationDiagnosticsSession
{
    private readonly object _syncRoot = new();

    private readonly List<
        RequirementEvaluationDiagnostic?>
        _requirementEvaluations = [];

    internal RequirementEvaluationToken Begin(
        RequirementDefinition requirement,
        Guid? parentEvaluationId)
    {
        ArgumentNullException.ThrowIfNull(requirement);

        var evaluationId = Guid.NewGuid();

        var requirementKind =
            GetRequirementKind(requirement);

        AuthorizationAttributeSource? attributeSource =
            null;

        string? attributeName = null;

        AuthorizationAttributeSource?
            comparedAttributeSource = null;

        string? comparedAttributeName = null;

        if (requirement is
            AttributeRequirementDefinition
                attributeRequirement)
        {
            attributeSource =
                attributeRequirement.Source;

            attributeName =
                attributeRequirement.Name;
        }

        if (requirement is
            AttributeComparisonRequirementDefinition
                comparisonRequirement)
        {
            (attributeSource, attributeName) =
                GetAttributeStructure(
                    comparisonRequirement.Left);

            (comparedAttributeSource,
             comparedAttributeName) =
                GetAttributeStructure(
                    comparisonRequirement.Right);
        }

        if (requirement is
            ContextRequirementDefinition
                contextRequirement)
        {
            attributeSource =
                AuthorizationAttributeSource.Context;

            attributeName =
                contextRequirement.AttributeName;
        }

        if (requirement is
            ContextAgeRequirementDefinition
                contextAgeRequirement)
        {
            attributeSource =
                AuthorizationAttributeSource.Context;

            attributeName =
                contextAgeRequirement.AttributeName;
        }

        int index;

        lock (_syncRoot)
        {
            index = _requirementEvaluations.Count;
            _requirementEvaluations.Add(null);
        }

        return new RequirementEvaluationToken(
            index,
            evaluationId,
            parentEvaluationId,
            requirement.Id,
            requirementKind,
            attributeSource,
            attributeName,
            comparedAttributeSource,
            comparedAttributeName,
            Stopwatch.GetTimestamp());
    }

    internal void Complete(
        RequirementEvaluationToken token,
        RequirementEvaluationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        var diagnostic =
            new RequirementEvaluationDiagnostic(
                token.EvaluationId,
                token.ParentEvaluationId,
                token.RequirementId,
                token.RequirementKind,
                result.Outcome,
                Stopwatch.GetElapsedTime(
                    token.StartTimestamp),
                result.Failures.Select(
                    static failure =>
                        failure.Code),
                token.AttributeSource,
                token.AttributeName,
                token.ComparedAttributeSource,
                token.ComparedAttributeName);

        lock (_syncRoot)
        {
            _requirementEvaluations[token.Index] =
                diagnostic;
        }
    }

    internal IReadOnlyList<
        RequirementEvaluationDiagnostic>
        CreateSnapshot()
    {
        lock (_syncRoot)
        {
            return Array.AsReadOnly(
                _requirementEvaluations
                    .OfType<
                        RequirementEvaluationDiagnostic>()
                    .ToArray());
        }
    }

    private static AuthorizationRequirementKind
        GetRequirementKind(
            RequirementDefinition requirement)
    {
        return requirement switch
        {
            PermissionRequirementDefinition =>
                AuthorizationRequirementKind.Permission,

            RoleRequirementDefinition =>
                AuthorizationRequirementKind.Role,

            AttributeRequirementDefinition =>
                AuthorizationRequirementKind.Attribute,

            AttributeComparisonRequirementDefinition =>
                AuthorizationRequirementKind
                    .AttributeComparison,

            TimeWindowRequirementDefinition =>
                AuthorizationRequirementKind.TimeWindow,

            DateTimeWindowRequirementDefinition =>
                AuthorizationRequirementKind.DateTimeWindow,

            ContextAgeRequirementDefinition =>
                AuthorizationRequirementKind.ContextAge,

            ContextRequirementDefinition =>
                AuthorizationRequirementKind.Context,

            AllRequirementDefinition =>
                AuthorizationRequirementKind.All,

            AnyRequirementDefinition =>
                AuthorizationRequirementKind.Any,

            NotRequirementDefinition =>
                AuthorizationRequirementKind.Not,

            _ =>
                AuthorizationRequirementKind.Custom
        };
    }

    private static (
        AuthorizationAttributeSource? Source,
        string? Name) GetAttributeStructure(
            AuthorizationAttributeOperand operand)
    {
        return operand.Kind switch
        {
            AuthorizationAttributeOperandKind.Subject =>
                (AuthorizationAttributeSource.Subject,
                 operand.Name),

            AuthorizationAttributeOperandKind.Resource =>
                (AuthorizationAttributeSource.Resource,
                 operand.Name),

            AuthorizationAttributeOperandKind.Context =>
                (AuthorizationAttributeSource.Context,
                 operand.Name),

            AuthorizationAttributeOperandKind.Literal =>
                (null, null),

            _ => throw new ArgumentOutOfRangeException(
                nameof(operand),
                operand.Kind,
                "The authorization attribute operand kind is not supported.")
        };
    }

    internal readonly record struct
        RequirementEvaluationToken(
            int Index,
            Guid EvaluationId,
            Guid? ParentEvaluationId,
            string? RequirementId,
            AuthorizationRequirementKind RequirementKind,
            AuthorizationAttributeSource? AttributeSource,
            string? AttributeName,
            AuthorizationAttributeSource?
                ComparedAttributeSource,
            string? ComparedAttributeName,
            long StartTimestamp);
}
