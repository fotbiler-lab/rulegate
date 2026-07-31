using System.Collections.Concurrent;
using Fotbiler.RuleGate.Abstractions.Authorization;
using Fotbiler.RuleGate.Abstractions.Policies;
using Fotbiler.RuleGate.Core.Engine;
using Fotbiler.RuleGate.Core.Evaluation;
using Fotbiler.RuleGate.Core.Evaluation.Evaluators;
using Fotbiler.RuleGate.Core.Policies;

namespace Fotbiler.RuleGate.Core.Tests;

public sealed class ConcurrencyHardeningTests
{
    [Fact]
    public async Task Parallel_evaluations_remain_deterministic_during_reload()
    {
        var source = new ConcurrentPolicySource(
            CreateLoadResult("version-a"));
        var provider = new AtomicPolicyProvider([source]);
        await provider.ReloadAsync();

        var engine = new PolicyAuthorizationEngine(
            provider,
            new RequirementEvaluationDispatcher(
            [
                new PermissionRequirementEvaluator()
            ]));

        var request = new AuthorizationRequest(
            new AuthorizationSubject(
                "subject",
                permissions: ["document.read"]),
            new AuthorizationResource(
                "document",
                "resource"),
            "read",
            new AuthorizationContext(
                DateTimeOffset.UtcNow));

        var failures = new ConcurrentQueue<string>();
        var start = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);

        var readers = Enumerable.Range(0, 16)
            .Select(
                _ => Task.Run(
                    async () =>
                    {
                        await start.Task;

                        for (var index = 0;
                             index < 1_000;
                             index++)
                        {
                            try
                            {
                                var decision =
                                    await engine.EvaluateAsync(
                                        request);

                                if (!decision.IsAllowed)
                                {
                                    failures.Enqueue("denied");
                                }
                            }
                            catch (Exception exception)
                            {
                                failures.Enqueue(
                                    exception.GetType().Name);
                            }
                        }
                    }))
            .ToArray();

        var reloader = Task.Run(
            async () =>
            {
                await start.Task;

                for (var index = 0;
                     index < 200;
                     index++)
                {
                    source.Result = CreateLoadResult(
                        index % 2 == 0
                            ? "version-a"
                            : "version-b");

                    var result = await provider.ReloadAsync();

                    if (!result.IsActivated)
                    {
                        failures.Enqueue("reload-rejected");
                    }
                }
            });

        start.SetResult();

        await Task.WhenAll(
            readers.Append(reloader));

        Assert.Empty(failures);
        Assert.Equal(201, provider.CurrentSnapshot.Version);
        Assert.Equal(1, provider.CurrentSnapshot.PolicyCount);
    }

    [Fact]
    public async Task Concurrent_reload_requests_are_serialized()
    {
        var source = new SerializedPolicySource();
        var provider = new AtomicPolicyProvider([source]);

        var reloads = Enumerable.Range(0, 40)
            .Select(
                _ => provider.ReloadAsync().AsTask())
            .ToArray();

        var results = await Task.WhenAll(reloads);

        Assert.All(
            results,
            static result => Assert.True(
                result.IsActivated));
        Assert.Equal(1, source.MaximumConcurrentLoads);
        Assert.Equal(40, source.LoadCount);
        Assert.Equal(
            Enumerable.Range(1, 40).Select(
                static version => (long)version),
            results.Select(
                    static result =>
                        result.ActiveSnapshot.Version)
                .Order());
    }

    [Fact]
    public async Task Cancelled_waiting_reload_does_not_corrupt_lock_or_snapshot()
    {
        var source = new BlockingPolicySource();
        var provider = new AtomicPolicyProvider([source]);
        var initial = await provider.ReloadAsync();

        source.BlockNextLoad();

        var activeReload = provider.ReloadAsync().AsTask();
        await source.WaitUntilBlockedAsync();

        using var cancellation =
            new CancellationTokenSource();

        var cancelledReload = provider.ReloadAsync(
            cancellation.Token).AsTask();

        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => cancelledReload);

        source.ReleaseBlockedLoad();
        var activated = await activeReload;
        var final = await provider.ReloadAsync();

        Assert.True(activated.IsActivated);
        Assert.True(final.IsActivated);
        Assert.Equal(1, source.MaximumConcurrentLoads);
        Assert.Equal(
            initial.ActiveSnapshot.Version + 2,
            final.ActiveSnapshot.Version);
        Assert.NotNull(
            await provider.FindAsync(
                "document",
                "read"));
    }

    private static PolicySourceLoadResult CreateLoadResult(
        string policyId)
    {
        return PolicySourceLoadResult.Success(
        [
            new PolicyDefinition(
                policyId,
                "document",
                "read",
                new PermissionRequirementDefinition(
                    "document.read"))
        ]);
    }

    private sealed class ConcurrentPolicySource : IPolicySource
    {
        private PolicySourceLoadResult _result;

        public ConcurrentPolicySource(
            PolicySourceLoadResult result)
        {
            _result = result;
        }

        public string Name => "concurrent";

        public PolicySourceLoadResult Result
        {
            get => Volatile.Read(ref _result);
            set => Volatile.Write(ref _result, value);
        }

        public ValueTask<PolicySourceLoadResult> LoadAsync(
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(Result);
        }
    }

    private sealed class SerializedPolicySource : IPolicySource
    {
        private int _activeLoads;
        private int _loadCount;
        private int _maximumConcurrentLoads;

        public string Name => "serialized";

        public int LoadCount => Volatile.Read(ref _loadCount);

        public int MaximumConcurrentLoads =>
            Volatile.Read(ref _maximumConcurrentLoads);

        public async ValueTask<PolicySourceLoadResult> LoadAsync(
            CancellationToken cancellationToken = default)
        {
            var active = Interlocked.Increment(
                ref _activeLoads);

            UpdateMaximum(active);

            try
            {
                Interlocked.Increment(ref _loadCount);
                await Task.Delay(2, cancellationToken);
                return CreateLoadResult("serialized-policy");
            }
            finally
            {
                Interlocked.Decrement(ref _activeLoads);
            }
        }

        private void UpdateMaximum(int candidate)
        {
            var current = Volatile.Read(
                ref _maximumConcurrentLoads);

            while (candidate > current)
            {
                var previous = Interlocked.CompareExchange(
                    ref _maximumConcurrentLoads,
                    candidate,
                    current);

                if (previous == current)
                {
                    return;
                }

                current = previous;
            }
        }
    }

    private sealed class BlockingPolicySource : IPolicySource
    {
        private readonly object _sync = new();
        private int _activeLoads;
        private int _maximumConcurrentLoads;
        private TaskCompletionSource? _blocked;
        private TaskCompletionSource? _release;

        public string Name => "blocking";

        public int MaximumConcurrentLoads =>
            Volatile.Read(ref _maximumConcurrentLoads);

        public void BlockNextLoad()
        {
            lock (_sync)
            {
                _blocked = new TaskCompletionSource(
                    TaskCreationOptions
                        .RunContinuationsAsynchronously);
                _release = new TaskCompletionSource(
                    TaskCreationOptions
                        .RunContinuationsAsynchronously);
            }
        }

        public Task WaitUntilBlockedAsync()
        {
            lock (_sync)
            {
                return (_blocked ?? throw new InvalidOperationException())
                    .Task;
            }
        }

        public void ReleaseBlockedLoad()
        {
            lock (_sync)
            {
                (_release ?? throw new InvalidOperationException())
                    .TrySetResult();
            }
        }

        public async ValueTask<PolicySourceLoadResult> LoadAsync(
            CancellationToken cancellationToken = default)
        {
            var active = Interlocked.Increment(
                ref _activeLoads);

            UpdateMaximum(active);

            try
            {
                TaskCompletionSource? blocked;
                TaskCompletionSource? release;

                lock (_sync)
                {
                    blocked = _blocked;
                    release = _release;
                }

                if (release is not null)
                {
                    blocked!.TrySetResult();
                    await release.Task.WaitAsync(
                        cancellationToken);

                    lock (_sync)
                    {
                        if (ReferenceEquals(
                                _release,
                                release))
                        {
                            _blocked = null;
                            _release = null;
                        }
                    }
                }

                return CreateLoadResult("blocking-policy");
            }
            finally
            {
                Interlocked.Decrement(ref _activeLoads);
            }
        }

        private void UpdateMaximum(int candidate)
        {
            var current = Volatile.Read(
                ref _maximumConcurrentLoads);

            while (candidate > current)
            {
                var previous = Interlocked.CompareExchange(
                    ref _maximumConcurrentLoads,
                    candidate,
                    current);

                if (previous == current)
                {
                    return;
                }

                current = previous;
            }
        }
    }
}
