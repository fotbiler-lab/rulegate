using System.Collections;
using System.Globalization;
using System.Text;
using Fotbiler.RuleGate.Manifest.Models;

namespace Fotbiler.RuleGate.Cli.Linting;

internal sealed class ManifestLinter
{
    private const int MaximumRecommendedDepth = 8;
    private const int MaximumRecommendedNodeCount = 32;

    public IReadOnlyList<ManifestLintFinding> Analyze(
        RuleGateManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        var findings =
            new List<ManifestLintFinding>();

        var requirementIds =
            new Dictionary<string, string>(
                StringComparer.Ordinal);

        var policies = manifest.Policies!;

        var policyIds =
            policies
                .Select(
                    static item => item!.Id!)
                .ToHashSet(
                    StringComparer.Ordinal);

        for (var index = 0;
             index < policies.Count;
             index++)
        {
            var policy = policies[index]!;
            var path =
                $"policies[{index}].requirement";

            var nodeCount =
                CountNodes(
                    policy.Requirement!);

            if (nodeCount > MaximumRecommendedNodeCount)
            {
                findings.Add(
                    new ManifestLintFinding(
                        ManifestLintCodes
                            .ExcessiveComplexity,
                        "warning",
                        path,
                        $"Policy requirement contains {nodeCount} nodes; the recommended maximum is {MaximumRecommendedNodeCount}."));
            }

            AnalyzeRequirement(
                policy.Requirement!,
                path,
                depth: 1,
                insideAny: false,
                policyIds,
                requirementIds,
                findings);
        }

        return findings
            .OrderBy(
                static item => item.Path,
                StringComparer.Ordinal)
            .ThenBy(
                static item => item.Code,
                StringComparer.Ordinal)
            .ThenBy(
                static item => item.Message,
                StringComparer.Ordinal)
            .ToArray();
    }

    private static void AnalyzeRequirement(
        ManifestRequirement requirement,
        string path,
        int depth,
        bool insideAny,
        IReadOnlySet<string> policyIds,
        IDictionary<string, string> requirementIds,
        ICollection<ManifestLintFinding> findings)
    {
        if (depth > MaximumRecommendedDepth)
        {
            findings.Add(
                new ManifestLintFinding(
                    ManifestLintCodes.ExcessiveDepth,
                    "warning",
                    path,
                    $"Requirement depth {depth} exceeds the recommended maximum of {MaximumRecommendedDepth}."));
        }

        AnalyzeIdentifier(
            requirement,
            path,
            policyIds,
            requirementIds,
            findings);

        AnalyzeRiskyOperator(
            requirement,
            path,
            insideAny,
            findings);

        if (requirement.All is not null)
        {
            AnalyzeLogicalChildren(
                requirement.All!,
                path,
                "all",
                findings);

            AnalyzeChildren(
                requirement.All!,
                path,
                "all",
                depth,
                insideAny,
                policyIds,
                requirementIds,
                findings);
        }

        if (requirement.Any is not null)
        {
            AnalyzeLogicalChildren(
                requirement.Any!,
                path,
                "any",
                findings);

            AnalyzeChildren(
                requirement.Any!,
                path,
                "any",
                depth,
                insideAny: true,
                policyIds,
                requirementIds,
                findings);
        }

        if (requirement.Not is not null)
        {
            if (requirement.Not.Not is not null ||
                requirement.Not.All is not null ||
                requirement.Not.Any is not null)
            {
                findings.Add(
                    new ManifestLintFinding(
                        ManifestLintCodes
                            .UnnecessaryComplexity,
                        "warning",
                        $"{path}.not",
                        "A nested logical requirement under not is harder to audit and can be simplified using explicit positive conditions."));
            }

            AnalyzeRequirement(
                requirement.Not,
                $"{path}.not",
                depth + 1,
                insideAny,
                policyIds,
                requirementIds,
                findings);
        }
    }

    private static void AnalyzeIdentifier(
        ManifestRequirement requirement,
        string path,
        IReadOnlySet<string> policyIds,
        IDictionary<string, string> requirementIds,
        ICollection<ManifestLintFinding> findings)
    {
        if (requirement.Id is null)
        {
            return;
        }

        if (requirementIds.TryGetValue(
                requirement.Id,
                out var previousPath))
        {
            findings.Add(
                new ManifestLintFinding(
                    ManifestLintCodes
                        .DuplicateRequirementId,
                    "error",
                    $"{path}.id",
                    $"Requirement identifier '{requirement.Id}' is already used at '{previousPath}.id'."));
        }
        else
        {
            requirementIds.Add(
                requirement.Id,
                path);
        }

        if (policyIds.Contains(requirement.Id))
        {
            findings.Add(
                new ManifestLintFinding(
                    ManifestLintCodes
                        .IdentifierCollision,
                    "warning",
                    $"{path}.id",
                    $"Requirement identifier '{requirement.Id}' collides with a policy identifier."));
        }
    }

    private static void AnalyzeLogicalChildren(
        IReadOnlyList<ManifestRequirement?> children,
        string path,
        string kind,
        ICollection<ManifestLintFinding> findings)
    {
        if (children.Count == 1)
        {
            findings.Add(
                new ManifestLintFinding(
                    ManifestLintCodes
                        .UnnecessaryComplexity,
                    "warning",
                    path,
                    $"A {kind} requirement with one child can be replaced by that child."));
        }

        var fingerprints =
            children
                .Select(
                    static child =>
                        CreateFingerprint(child!))
                .ToArray();

        var firstIndexes =
            new Dictionary<string, int>(
                StringComparer.Ordinal);

        for (var index = 0;
             index < children.Count;
             index++)
        {
            var child = children[index]!;
            var childPath =
                $"{path}.{kind}[{index}]";

            if (firstIndexes.TryGetValue(
                    fingerprints[index],
                    out var firstIndex))
            {
                findings.Add(
                    new ManifestLintFinding(
                        ManifestLintCodes
                            .DuplicateRequirement,
                        "warning",
                        childPath,
                        $"Requirement duplicates '{path}.{kind}[{firstIndex}]'."));
            }
            else
            {
                firstIndexes.Add(
                    fingerprints[index],
                    index);
            }

            if ((kind == "all" && child.All is not null) ||
                (kind == "any" && child.Any is not null))
            {
                findings.Add(
                    new ManifestLintFinding(
                        ManifestLintCodes
                            .UnnecessaryComplexity,
                        "warning",
                        childPath,
                        $"Nested {kind} requirements can be flattened."));
            }
        }

        AnalyzeContradictions(
            children,
            fingerprints,
            path,
            kind,
            findings);

        AnalyzeAbsorption(
            children,
            fingerprints,
            path,
            kind,
            findings);
    }

    private static void AnalyzeContradictions(
        IReadOnlyList<ManifestRequirement?> children,
        IReadOnlyList<string> fingerprints,
        string path,
        string kind,
        ICollection<ManifestLintFinding> findings)
    {
        if (kind != "all")
        {
            return;
        }

        for (var index = 0;
             index < children.Count;
             index++)
        {
            var child = children[index]!;

            if (child.Not is not null)
            {
                var negatedFingerprint =
                    CreateFingerprint(child.Not);

                var matchingIndex =
                    IndexOf(
                        fingerprints,
                        negatedFingerprint,
                        excludedIndex: index);

                if (matchingIndex >= 0)
                {
                    findings.Add(
                        new ManifestLintFinding(
                            ManifestLintCodes
                                .ContradictoryRequirement,
                            "error",
                            $"{path}.all[{index}]",
                            $"Requirement contradicts '{path}.all[{matchingIndex}]'."));
                }
            }

            AnalyzeConflictingEquality(
                children,
                index,
                path,
                findings);
        }
    }

    private static void AnalyzeConflictingEquality(
        IReadOnlyList<ManifestRequirement?> children,
        int index,
        string path,
        ICollection<ManifestLintFinding> findings)
    {
        var current = children[index]!;
        var currentEquality =
            GetEqualityConstraint(current);

        if (currentEquality is null)
        {
            return;
        }

        for (var previousIndex = 0;
             previousIndex < index;
             previousIndex++)
        {
            var previousEquality =
                GetEqualityConstraint(
                    children[previousIndex]!);

            if (previousEquality is null ||
                !string.Equals(
                    currentEquality.Value.Target,
                    previousEquality.Value.Target,
                    StringComparison.Ordinal) ||
                string.Equals(
                    currentEquality.Value.Value,
                    previousEquality.Value.Value,
                    StringComparison.Ordinal))
            {
                continue;
            }

            findings.Add(
                new ManifestLintFinding(
                    ManifestLintCodes
                        .ContradictoryRequirement,
                    "error",
                    $"{path}.all[{index}]",
                    $"Equality constraint conflicts with '{path}.all[{previousIndex}]'."));

            return;
        }
    }

    private static void AnalyzeAbsorption(
        IReadOnlyList<ManifestRequirement?> children,
        IReadOnlyList<string> fingerprints,
        string path,
        string kind,
        ICollection<ManifestLintFinding> findings)
    {
        for (var index = 0;
             index < children.Count;
             index++)
        {
            var child = children[index]!;
            var nested =
                kind == "any"
                    ? child.All
                    : child.Any;

            if (nested is null)
            {
                continue;
            }

            foreach (var nestedChild in nested)
            {
                var matchingIndex =
                    IndexOf(
                        fingerprints,
                        CreateFingerprint(
                            nestedChild!),
                        excludedIndex: index);

                if (matchingIndex < 0)
                {
                    continue;
                }

                findings.Add(
                    new ManifestLintFinding(
                        ManifestLintCodes
                            .UnreachableRequirement,
                        "warning",
                        $"{path}.{kind}[{index}]",
                        $"Logical branch is absorbed by '{path}.{kind}[{matchingIndex}]' and cannot change the decision."));

                break;
            }
        }
    }

    private static void AnalyzeChildren(
        IReadOnlyList<ManifestRequirement?> children,
        string path,
        string kind,
        int depth,
        bool insideAny,
        IReadOnlySet<string> policyIds,
        IDictionary<string, string> requirementIds,
        ICollection<ManifestLintFinding> findings)
    {
        for (var index = 0;
             index < children.Count;
             index++)
        {
            AnalyzeRequirement(
                children[index]!,
                $"{path}.{kind}[{index}]",
                depth + 1,
                insideAny,
                policyIds,
                requirementIds,
                findings);
        }
    }

    private static void AnalyzeRiskyOperator(
        ManifestRequirement requirement,
        string path,
        bool insideAny,
        ICollection<ManifestLintFinding> findings)
    {
        if (!insideAny)
        {
            return;
        }

        var @operator =
            requirement.Attribute?.Operator ??
            requirement.AttributeComparison?.Operator ??
            requirement.Context?.Operator;

        if (@operator is not (
                "notEqual" or
                "notIn" or
                "notExists" or
                "isNotNull" or
                "isNotEmpty"))
        {
            return;
        }

        findings.Add(
            new ManifestLintFinding(
                ManifestLintCodes
                    .RiskyNegativeOperator,
                "warning",
                path,
                $"Negative operator '{@operator}' inside any can grant access broadly; verify missing and null attribute behavior explicitly."));
    }

    private static (
        string Target,
        string Value)? GetEqualityConstraint(
            ManifestRequirement requirement)
    {
        if (requirement.Attribute is
            { Operator: "equal" } attribute)
        {
            return (
                $"attribute:{attribute.Source}:" +
                $"{attribute.Name}:" +
                $"{attribute.ValueType}:" +
                attribute.StringComparison,
                FormatValue(attribute.Value));
        }

        if (requirement.Context is
            { Operator: "equal" } context)
        {
            return (
                $"context:{context.Property}:" +
                $"{context.ValueType}:" +
                context.StringComparison,
                FormatValue(context.Value));
        }

        return null;
    }

    private static int CountNodes(
        ManifestRequirement requirement)
    {
        var children =
            requirement.All ??
            requirement.Any;

        var childCount =
            children?.Sum(
                static child => CountNodes(child!)) ??
            0;

        return 1 +
               childCount +
               (requirement.Not is null
                   ? 0
                   : CountNodes(requirement.Not));
    }

    private static int IndexOf(
        IReadOnlyList<string> values,
        string expected,
        int excludedIndex)
    {
        for (var index = 0;
             index < values.Count;
             index++)
        {
            if (index != excludedIndex &&
                string.Equals(
                    values[index],
                    expected,
                    StringComparison.Ordinal))
            {
                return index;
            }
        }

        return -1;
    }

    private static string CreateFingerprint(
        ManifestRequirement requirement)
    {
        var builder = new StringBuilder();

        if (requirement.Permission is not null)
        {
            return "permission:" +
                   requirement.Permission;
        }

        if (requirement.Role is not null)
        {
            return "role:" + requirement.Role;
        }

        if (requirement.Attribute is not null)
        {
            var attribute = requirement.Attribute;

            return "attribute:" +
                   attribute.Source + ":" +
                   attribute.Name + ":" +
                   attribute.Operator + ":" +
                   attribute.ValueType + ":" +
                   attribute.StringComparison + ":" +
                   FormatValue(attribute.Value);
        }

        if (requirement.AttributeComparison is not null)
        {
            var comparison =
                requirement.AttributeComparison;

            return "comparison:" +
                   FormatOperand(comparison.Left!) + ":" +
                   comparison.Operator + ":" +
                   FormatOperand(comparison.Right!) + ":" +
                   comparison.StringComparison;
        }

        if (requirement.TimeWindow is not null)
        {
            return "timeWindow:" +
                   string.Join(
                       ",",
                       requirement.TimeWindow.Days!) + ":" +
                   requirement.TimeWindow.Start + ":" +
                   requirement.TimeWindow.End + ":" +
                   requirement.TimeWindow.TimeZone;
        }

        if (requirement.DateTimeWindow is not null)
        {
            return "dateTimeWindow:" +
                   requirement.DateTimeWindow.StartsAt + ":" +
                   requirement.DateTimeWindow.EndsAt;
        }

        if (requirement.ContextAge is not null)
        {
            return "contextAge:" +
                   requirement.ContextAge.Timestamp + ":" +
                   requirement.ContextAge.MaximumAge;
        }

        if (requirement.Context is not null)
        {
            var context = requirement.Context;

            return "context:" +
                   context.Property + ":" +
                   context.Operator + ":" +
                   context.ValueType + ":" +
                   context.StringComparison + ":" +
                   FormatValue(context.Value);
        }

        if (requirement.Not is not null)
        {
            return "not(" +
                   CreateFingerprint(requirement.Not) +
                   ")";
        }

        var kind =
            requirement.All is not null
                ? "all"
                : "any";

        var logicalChildren =
            requirement.All ?? requirement.Any!;

        builder.Append(kind);
        builder.Append('(');
        builder.AppendJoin(
            ",",
            logicalChildren
                .Select(
                    static child =>
                        CreateFingerprint(child!))
                .Order(
                    StringComparer.Ordinal));
        builder.Append(')');

        return builder.ToString();
    }

    private static string FormatOperand(
        ManifestAttributeComparisonOperand operand)
    {
        return operand.Source + ":" +
               operand.Name + ":" +
               operand.ValueType + ":" +
               FormatValue(operand.Value);
    }

    private static string FormatValue(
        object? value)
    {
        if (value is null)
        {
            return "<null>";
        }

        if (value is IEnumerable enumerable &&
            value is not string)
        {
            return "[" +
                   string.Join(
                       ",",
                       enumerable.Cast<object?>()
                           .Select(FormatValue)) +
                   "]";
        }

        return Convert.ToString(
                   value,
                   CultureInfo.InvariantCulture) ??
               string.Empty;
    }
}
