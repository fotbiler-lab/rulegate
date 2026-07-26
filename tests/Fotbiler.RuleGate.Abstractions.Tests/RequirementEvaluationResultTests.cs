using Fotbiler.RuleGate.Abstractions.Authorization;
using Fotbiler.RuleGate.Abstractions.Evaluation;

namespace Fotbiler.RuleGate.Abstractions.Tests;

public sealed class RequirementEvaluationResultTests
{
    [Fact]
    public void DefaultOutcome_IsIndeterminate()
    {
        Assert.Equal(
            RequirementEvaluationOutcome.Indeterminate,
            default(RequirementEvaluationOutcome));
    }

    [Fact]
    public void Satisfied_HasNoFailures()
    {
        var result =
            RequirementEvaluationResult.Satisfied();

        Assert.True(result.IsSatisfied);
        Assert.Empty(result.Failures);
    }

    [Fact]
    public void NotSatisfied_RequiresFailure()
    {
        Assert.Throws<ArgumentException>(
            () => RequirementEvaluationResult
                .NotSatisfied());
    }

    [Fact]
    public void Indeterminate_RequiresFailure()
    {
        Assert.Throws<ArgumentException>(
            () => RequirementEvaluationResult
                .Indeterminate());
    }

    [Fact]
    public void FailureCollection_IsReadOnly()
    {
        var result =
            RequirementEvaluationResult.NotSatisfied(
                new AuthorizationFailure(
                    "sample.failure"));

        var failures =
            Assert.IsAssignableFrom<
                IList<AuthorizationFailure>>(
                result.Failures);

        Assert.Throws<NotSupportedException>(
            () => failures.Add(
                new AuthorizationFailure(
                    "another.failure")));
    }
}
