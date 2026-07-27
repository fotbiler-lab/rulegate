using Fotbiler.RuleGate.Abstractions.Diagnostics;
using Fotbiler.RuleGate.Abstractions.Evaluation;
using Fotbiler.RuleGate.Abstractions.Policies;

namespace Fotbiler.RuleGate.Abstractions.Tests;

public sealed class AuthorizationDiagnosticsTests
{
    [Fact]
    public void
        RequirementDiagnostic_copies_failure_codes()
    {
        var failureCodes =
            new List<string>
            {
                "RULEGATE_SAMPLE_FAILURE"
            };

        var diagnostic =
            new RequirementEvaluationDiagnostic(
                Guid.NewGuid(),
                parentEvaluationId: null,
                requirementId: "sample-requirement",
                AuthorizationRequirementKind.Attribute,
                RequirementEvaluationOutcome
                    .NotSatisfied,
                TimeSpan.FromMilliseconds(1),
                failureCodes,
                AuthorizationAttributeSource.Subject,
                "department");

        failureCodes.Clear();

        Assert.Equal(
            ["RULEGATE_SAMPLE_FAILURE"],
            diagnostic.FailureCodes);

        Assert.Equal(
            AuthorizationAttributeSource.Subject,
            diagnostic.AttributeSource);

        Assert.Equal(
            "department",
            diagnostic.AttributeName);
    }

    [Fact]
    public void
        AuthorizationDiagnostic_copies_collections()
    {
        var requirementDiagnostics =
            new List<RequirementEvaluationDiagnostic>
            {
                new(
                    Guid.NewGuid(),
                    parentEvaluationId: null,
                    requirementId: "sample",
                    AuthorizationRequirementKind.Permission,
                    RequirementEvaluationOutcome.Satisfied,
                    TimeSpan.Zero,
                    [])
            };

        var failureCodes =
            new List<string>();

        var diagnostic =
            new AuthorizationEvaluationDiagnostic(
                Guid.NewGuid(),
                "sample-policy",
                isAllowed: true,
                TimeSpan.Zero,
                failureCodes,
                requirementDiagnostics);

        failureCodes.Add("MUTATED");
        requirementDiagnostics.Clear();

        Assert.Empty(diagnostic.FailureCodes);

        Assert.Single(
            diagnostic.RequirementEvaluations);
    }

    [Fact]
    public void
        RequirementDiagnostic_does_not_expose_attribute_values()
    {
        var propertyNames =
            typeof(RequirementEvaluationDiagnostic)
                .GetProperties()
                .Select(
                    static property =>
                        property.Name)
                .ToArray();

        Assert.DoesNotContain(
            "Value",
            propertyNames);

        Assert.DoesNotContain(
            "ActualValue",
            propertyNames);

        Assert.DoesNotContain(
            "ExpectedValue",
            propertyNames);
    }

    [Fact]
    public void
        AuthorizationDiagnostic_rejects_empty_evaluation_id()
    {
        Assert.Throws<ArgumentException>(
            () =>
                new AuthorizationEvaluationDiagnostic(
                    Guid.Empty,
                    policyId: null,
                    isAllowed: false,
                    TimeSpan.Zero,
                    [],
                    []));
    }
}
