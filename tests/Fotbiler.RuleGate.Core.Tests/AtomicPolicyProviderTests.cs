using System.Collections.Concurrent;
using Fotbiler.RuleGate.Abstractions.Policies;
using Fotbiler.RuleGate.Core.Policies;

namespace Fotbiler.RuleGate.Core.Tests;

public sealed class AtomicPolicyProviderTests
{
    [Fact]
    public async Task FindAsync_LazilyActivatesInitialSnapshot()
    {
        var policy = CreatePolicy("document-read", "read");
        var source = new MutablePolicySource(
            "primary",
            PolicySourceLoadResult.Success([policy]));
        var provider = new AtomicPolicyProvider([source]);

        Assert.False(provider.HasReloaded);

        var actual = await provider.FindAsync(
            "document",
            "read");

        Assert.Same(policy, actual);
        Assert.Equal(1, provider.CurrentSnapshot.Version);
        Assert.Equal(1, provider.CurrentSnapshot.PolicyCount);
        Assert.Equal(["primary"], provider.CurrentSnapshot.SourceNames);
        Assert.True(provider.LastReload.IsSuccess);
        Assert.True(provider.LastReload.IsActivated);
        Assert.True(provider.HasReloaded);
        Assert.Equal(1, source.LoadCount);
    }

    [Fact]
    public async Task ReloadAsync_MergesAllSuccessfulSources()
    {
        var provider = new AtomicPolicyProvider(
        [
            new MutablePolicySource(
                "write-source",
                PolicySourceLoadResult.Success(
                [
                    CreatePolicy(
                        "document-write",
                        "write")
                ])),
            new MutablePolicySource(
                "read-source",
                PolicySourceLoadResult.Success(
                [
                    CreatePolicy(
                        "document-read",
                        "read")
                ]))
        ]);

        var result = await provider.ReloadAsync();

        Assert.True(result.IsSuccess);
        Assert.True(result.IsActivated);
        Assert.Equal(2, result.ActiveSnapshot.PolicyCount);
        Assert.Equal(
            ["read-source", "write-source"],
            result.ActiveSnapshot.SourceNames);
        Assert.NotNull(
            await provider.FindAsync("document", "read"));
        Assert.NotNull(
            await provider.FindAsync("document", "write"));
    }

    [Fact]
    public async Task ReloadAsync_IncludesRegisteredInMemoryPolicies()
    {
        var inMemoryPolicy = CreatePolicy(
            "document-read",
            "read");
        var provider = new AtomicPolicyProvider(
            [],
            [inMemoryPolicy]);

        var result = await provider.ReloadAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(["in-memory"], result.ActiveSnapshot.SourceNames);
        Assert.Same(
            inMemoryPolicy,
            await provider.FindAsync("document", "read"));
    }

    [Fact]
    public async Task ReloadAsync_PreservesLastValidSnapshotWhenSourceFails()
    {
        var original = CreatePolicy(
            "document-read-v1",
            "read");
        var source = new MutablePolicySource(
            "primary",
            PolicySourceLoadResult.Success([original]));
        var provider = new AtomicPolicyProvider([source]);

        var first = await provider.ReloadAsync();

        source.Result = PolicySourceLoadResult.Failure(
        [
            new PolicySourceDiagnostic(
                "SOURCE_INVALID",
                "The candidate is invalid.",
                "policies[0]")
        ]);

        var rejected = await provider.ReloadAsync();

        Assert.False(rejected.IsSuccess);
        Assert.False(rejected.IsActivated);
        Assert.Equal(
            first.ActiveSnapshot,
            rejected.ActiveSnapshot);
        Assert.Same(
            original,
            await provider.FindAsync("document", "read"));
        var diagnostic = Assert.Single(
            rejected.Diagnostics);
        Assert.Equal("primary", diagnostic.SourceName);
        Assert.Equal("SOURCE_INVALID", diagnostic.Code);
        Assert.Equal("policies[0]", diagnostic.Path);
    }

    [Fact]
    public async Task ReloadAsync_ConvertsSourceExceptionsToStableDiagnostics()
    {
        var provider = new AtomicPolicyProvider(
        [
            new ThrowingPolicySource("unstable")
        ]);

        var result = await provider.ReloadAsync();

        Assert.False(result.IsSuccess);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("unstable", diagnostic.SourceName);
        Assert.Equal(
            PolicyReloadCodes.SourceLoadException,
            diagnostic.Code);
        Assert.Equal(
            "The policy source threw an exception while loading.",
            diagnostic.Message);
        Assert.DoesNotContain(
            "sensitive",
            diagnostic.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ReloadAsync_RejectsDuplicateSourceNamesDeterministically()
    {
        var provider = new AtomicPolicyProvider(
        [
            new MutablePolicySource(
                "duplicate",
                PolicySourceLoadResult.Success([])),
            new MutablePolicySource(
                "duplicate",
                PolicySourceLoadResult.Success([]))
        ]);

        var result = await provider.ReloadAsync();

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(
            PolicyReloadCodes.DuplicateSourceName,
            diagnostic.Code);
        Assert.Equal("duplicate", diagnostic.SourceName);
    }

    [Fact]
    public async Task ReloadAsync_RejectsDuplicateIdsAndRoutesAcrossSources()
    {
        var provider = new AtomicPolicyProvider(
        [
            new MutablePolicySource(
                "z-source",
                PolicySourceLoadResult.Success(
                [
                    CreatePolicy("shared", "read")
                ])),
            new MutablePolicySource(
                "a-source",
                PolicySourceLoadResult.Success(
                [
                    CreatePolicy("shared", "read")
                ]))
        ]);

        var result = await provider.ReloadAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal(
            [
                PolicyReloadCodes.DuplicatePolicyId,
                PolicyReloadCodes.DuplicatePolicyRoute,
            ],
            result.Diagnostics.Select(
                static item => item.Code));
        Assert.All(
            result.Diagnostics,
            diagnostic => Assert.Equal(
                "a-source",
                diagnostic.SourceName));
    }

    [Fact]
    public async Task ReloadAsync_CancellationPreservesActiveSnapshot()
    {
        var original = CreatePolicy(
            "document-read-v1",
            "read");
        var source = new MutablePolicySource(
            "primary",
            PolicySourceLoadResult.Success([original]));
        var provider = new AtomicPolicyProvider([source]);
        var active = await provider.ReloadAsync();

        using var cancellation =
            new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(
            async () => await provider.ReloadAsync(
                cancellation.Token));

        Assert.Equal(
            active.ActiveSnapshot,
            provider.CurrentSnapshot);
        Assert.Same(
            original,
            await provider.FindAsync("document", "read"));
    }

    [Fact]
    public async Task ConcurrentReadsObserveOnlyCompleteSnapshots()
    {
        var first = CreatePolicy("version-a", "read");
        var second = CreatePolicy("version-b", "read");
        var source = new MutablePolicySource(
            "primary",
            PolicySourceLoadResult.Success([first]));
        var provider = new AtomicPolicyProvider([source]);
        await provider.ReloadAsync();

        var observed = new ConcurrentBag<string>();

        var readers = Enumerable.Range(0, 8)
            .Select(
                _ => Task.Run(
                    async () =>
                    {
                        for (var index = 0;
                             index < 500;
                             index++)
                        {
                            var policy = await provider.FindAsync(
                                "document",
                                "read");

                            observed.Add(
                                policy?.Id ?? "missing");
                        }
                    }))
            .ToArray();

        for (var index = 0; index < 50; index++)
        {
            source.Result = PolicySourceLoadResult.Success(
                index % 2 == 0
                    ? [second]
                    : [first]);

            var result = await provider.ReloadAsync();
            Assert.True(result.IsSuccess);
        }

        await Task.WhenAll(readers);

        Assert.NotEmpty(observed);
        Assert.DoesNotContain("missing", observed);
        Assert.All(
            observed,
            id => Assert.Contains(
                id,
                new[] { "version-a", "version-b" }));
    }

    private static PolicyDefinition CreatePolicy(
        string id,
        string action)
    {
        return new PolicyDefinition(
            id,
            "document",
            action,
            new PermissionRequirementDefinition(
                $"document.{action}"));
    }

    private sealed class MutablePolicySource : IPolicySource
    {
        private int _loadCount;

        public MutablePolicySource(
            string name,
            PolicySourceLoadResult result)
        {
            Name = name;
            Result = result;
        }

        public string Name { get; }

        public PolicySourceLoadResult Result { get; set; }

        public int LoadCount => Volatile.Read(ref _loadCount);

        public async ValueTask<PolicySourceLoadResult> LoadAsync(
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Interlocked.Increment(ref _loadCount);

            await Task.Yield();

            cancellationToken.ThrowIfCancellationRequested();

            return Result;
        }
    }

    private sealed class ThrowingPolicySource : IPolicySource
    {
        public ThrowingPolicySource(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public ValueTask<PolicySourceLoadResult> LoadAsync(
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException(
                "Sensitive implementation detail.");
        }
    }
}
