using Fotbiler.RuleGate.Abstractions.Authorization;

namespace Fotbiler.RuleGate.Abstractions.Tests;

public sealed class AuthorizationDecisionTests
{
    [Fact]
    public void Deny_ExposesReadOnlyFailureCollection()
    {
        var decision = AuthorizationDecision.Deny(
            new AuthorizationFailure(
                "sample.failure"));

        var failures =
            Assert.IsAssignableFrom<
                IList<AuthorizationFailure>>(
                decision.Failures);

        Assert.Throws<NotSupportedException>(
            () => failures.Add(
                new AuthorizationFailure(
                    "another.failure")));
    }
}
