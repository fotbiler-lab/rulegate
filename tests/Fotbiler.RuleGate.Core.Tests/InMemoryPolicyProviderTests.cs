using Fotbiler.RuleGate.Abstractions.Policies;
using Fotbiler.RuleGate.Core.Policies;

namespace Fotbiler.RuleGate.Core.Tests;

public sealed class InMemoryPolicyProviderTests
{
    [Fact]
    public async Task FindAsync_ReturnsMatchingPolicy()
    {
        var policy = CreatePolicy(
            id: "sample-read",
            resourceType: "sample-resource",
            action: "read");

        var provider = new InMemoryPolicyProvider(
        [
            policy
        ]);

        var result = await provider.FindAsync(
            "sample-resource",
            "read");

        Assert.Same(policy, result);
    }

    [Fact]
    public async Task FindAsync_ReturnsNullWhenNoPolicyMatches()
    {
        var provider = new InMemoryPolicyProvider(
        [
            CreatePolicy(
                id: "sample-read",
                resourceType: "sample-resource",
                action: "read")
        ]);

        var result = await provider.FindAsync(
            "another-resource",
            "read");

        Assert.Null(result);
    }

    [Fact]
    public async Task FindAsync_UsesCaseSensitiveMatching()
    {
        var provider = new InMemoryPolicyProvider(
        [
            CreatePolicy(
                id: "sample-read",
                resourceType: "sample-resource",
                action: "read")
        ]);

        var resourceTypeResult =
            await provider.FindAsync(
                "SAMPLE-RESOURCE",
                "read");

        var actionResult =
            await provider.FindAsync(
                "sample-resource",
                "READ");

        Assert.Null(resourceTypeResult);
        Assert.Null(actionResult);
    }

    [Fact]
    public void Constructor_RejectsDuplicateRoutes()
    {
        Assert.Throws<InvalidOperationException>(
            () => new InMemoryPolicyProvider(
            [
                CreatePolicy(
                    id: "first-policy",
                    resourceType: "sample-resource",
                    action: "read"),
                CreatePolicy(
                    id: "second-policy",
                    resourceType: "sample-resource",
                    action: "read")
            ]));
    }

    [Fact]
    public void Constructor_RejectsDuplicatePolicyIdentifiers()
    {
        Assert.Throws<InvalidOperationException>(
            () => new InMemoryPolicyProvider(
            [
                CreatePolicy(
                    id: "sample-policy",
                    resourceType: "sample-resource",
                    action: "read"),
                CreatePolicy(
                    id: "sample-policy",
                    resourceType: "another-resource",
                    action: "write")
            ]));
    }

    [Fact]
    public void Constructor_RejectsNullCollection()
    {
        Assert.Throws<ArgumentNullException>(
            () => new InMemoryPolicyProvider(null!));
    }

    [Fact]
    public void Constructor_RejectsNullPolicy()
    {
        PolicyDefinition[] policies =
        [
            null!
        ];

        Assert.Throws<ArgumentNullException>(
            () => new InMemoryPolicyProvider(
                policies));
    }

    [Fact]
    public async Task FindAsync_HonorsCancellation()
    {
        var provider = new InMemoryPolicyProvider([]);

        using var cancellation =
            new CancellationTokenSource();

        cancellation.Cancel();

        await Assert.ThrowsAsync<
            OperationCanceledException>(
            async () => await provider.FindAsync(
                "sample-resource",
                "read",
                cancellation.Token));
    }

    private static PolicyDefinition CreatePolicy(
        string id,
        string resourceType,
        string action)
    {
        return new PolicyDefinition(
            id,
            resourceType,
            action,
            new PermissionRequirementDefinition(
                "sample.read"));
    }
}
