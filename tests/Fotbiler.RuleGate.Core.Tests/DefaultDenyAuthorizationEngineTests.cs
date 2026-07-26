using Fotbiler.RuleGate.Abstractions.Authorization;
using Fotbiler.RuleGate.Abstractions.Constants;
using Fotbiler.RuleGate.Core.Engine;

namespace Fotbiler.RuleGate.Core.Tests;

public sealed class DefaultDenyAuthorizationEngineTests
{
    [Fact]
    public async Task EvaluateAsync_DeniesWhenNoPolicyExists()
    {
        var engine =
            new DefaultDenyAuthorizationEngine();

        var request = CreateRequest();

        var decision =
            await engine.EvaluateAsync(request);

        Assert.False(decision.IsAllowed);

        var failure = Assert.Single(
            decision.Failures);

        Assert.Equal(
            AuthorizationFailureCodes.NoMatchingPolicy,
            failure.Code);
    }

    [Fact]
    public async Task EvaluateAsync_RejectsNullRequest()
    {
        var engine =
            new DefaultDenyAuthorizationEngine();

        await Assert.ThrowsAsync<ArgumentNullException>(
            async () =>
                await engine.EvaluateAsync(null!));
    }

    [Fact]
    public async Task EvaluateAsync_HonorsCancellation()
    {
        var engine =
            new DefaultDenyAuthorizationEngine();

        using var cancellation =
            new CancellationTokenSource();

        cancellation.Cancel();

        await Assert.ThrowsAsync<
            OperationCanceledException>(
            async () =>
                await engine.EvaluateAsync(
                    CreateRequest(),
                    cancellation.Token));
    }

    private static AuthorizationRequest CreateRequest()
    {
        return new AuthorizationRequest(
            subject: new AuthorizationSubject(
                id: "user-1"),
            resource: new AuthorizationResource(
                type: "sample-resource",
                id: "resource-1"),
            action: "read",
            context: new AuthorizationContext(
                DateTimeOffset.UtcNow));
    }
}
