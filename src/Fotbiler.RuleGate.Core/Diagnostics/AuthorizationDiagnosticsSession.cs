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

        if (requirement is
            AttributeRequirementDefinition
                attributeRequirement)
        {
            attributeSource =
                attributeRequirement.Source;

            attributeName =
                attributeRequirement.Name;
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
                token.AttributeName);

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

    internal readonly record struct
        RequirementEvaluationToken(
            int Index,
            Guid EvaluationId,
            Guid? ParentEvaluationId,
            string? RequirementId,
            AuthorizationRequirementKind RequirementKind,
            AuthorizationAttributeSource? AttributeSource,
            string? AttributeName,
            long StartTimestamp);
}
