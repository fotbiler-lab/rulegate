using System.Collections.Concurrent;
using Fotbiler.RuleGate.Abstractions.Authorization;
using Fotbiler.RuleGate.Abstractions.Policies;
using Fotbiler.RuleGate.Core.Engine;
using Fotbiler.RuleGate.Core.Evaluation;
using Fotbiler.RuleGate.Core.Evaluation.Evaluators;
using Fotbiler.RuleGate.Core.Policies;

var duration = ParseDuration(args);

if (duration is null)
{
    return 2;
}

var failures = new ConcurrentQueue<string>();
var source = new StressPolicySource();
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
        "stress-subject",
        permissions: ["document.read"]),
    new AuthorizationResource(
        "document",
        "stress-resource"),
    "read",
    new AuthorizationContext(
        DateTimeOffset.UtcNow));

using var stopping = new CancellationTokenSource(duration.Value);
var evaluationCount = 0L;
var reloadCount = 0L;
var cancellationCount = 0L;

var evaluatorCount = Math.Clamp(
    Environment.ProcessorCount,
    4,
    16);

var evaluators = Enumerable.Range(0, evaluatorCount)
    .Select(
        _ => Task.Run(
            async () =>
            {
                var completedSinceYield = 0;

                while (!stopping.IsCancellationRequested)
                {
                    try
                    {
                        var decision = await engine.EvaluateAsync(
                            request,
                            stopping.Token);

                        if (!decision.IsAllowed)
                        {
                            failures.Enqueue(
                                "An evaluation was denied.");
                        }

                        Interlocked.Increment(
                            ref evaluationCount);

                        completedSinceYield++;

                        if (completedSinceYield == 1_024)
                        {
                            completedSinceYield = 0;
                            await Task.Yield();
                        }
                    }
                    catch (OperationCanceledException)
                        when (stopping.IsCancellationRequested)
                    {
                        return;
                    }
                    catch (Exception exception)
                    {
                        failures.Enqueue(
                            $"Evaluation failed: {exception.GetType().Name}");
                    }
                }
            }))
    .ToArray();

var reloader = Task.Run(
    async () =>
    {
        var version = 0L;

        while (!stopping.IsCancellationRequested)
        {
            source.PolicyId =
                $"stress-policy-{Interlocked.Increment(ref version)}";

            try
            {
                var result = await provider.ReloadAsync(
                    stopping.Token);

                if (!result.IsActivated ||
                    result.ActiveSnapshot.PolicyCount != 1)
                {
                    failures.Enqueue(
                        "A reload did not activate one complete policy.");
                }

                Interlocked.Increment(ref reloadCount);
            }
            catch (OperationCanceledException)
                when (stopping.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                failures.Enqueue(
                    $"Reload failed: {exception.GetType().Name}");
            }
        }
    });

var canceller = Task.Run(
    async () =>
    {
        while (!stopping.IsCancellationRequested)
        {
            using var cancellation =
                new CancellationTokenSource();
            cancellation.Cancel();

            try
            {
                await provider.ReloadAsync(
                    cancellation.Token);
                failures.Enqueue(
                    "A cancelled reload unexpectedly completed.");
            }
            catch (OperationCanceledException)
            {
                Interlocked.Increment(
                    ref cancellationCount);
            }
            catch (Exception exception)
            {
                failures.Enqueue(
                    $"Cancellation failed: {exception.GetType().Name}");
            }

            await Task.Yield();
        }
    });

await Task.WhenAll(
    evaluators.Append(reloader).Append(canceller));

if (evaluationCount == 0 ||
    reloadCount == 0 ||
    cancellationCount == 0)
{
    failures.Enqueue(
        "The stress run did not exercise every operation family.");
}

if (provider.CurrentSnapshot.PolicyCount != 1)
{
    failures.Enqueue(
        "The final snapshot is incomplete.");
}

if (!failures.IsEmpty)
{
    foreach (var failure in failures.Take(20))
    {
        Console.Error.WriteLine(failure);
    }

    return 1;
}

Console.WriteLine(
    $"RuleGate concurrency stress passed. Duration={duration.Value.TotalSeconds:F0}s; Evaluations={evaluationCount}; Reloads={reloadCount}; Cancellations={cancellationCount}; SnapshotVersion={provider.CurrentSnapshot.Version}.");

return 0;

static TimeSpan? ParseDuration(string[] arguments)
{
    const int defaultSeconds = 60;

    if (arguments.Length == 0)
    {
        return TimeSpan.FromSeconds(defaultSeconds);
    }

    if (arguments.Length != 2 ||
        !string.Equals(
            arguments[0],
            "--duration-seconds",
            StringComparison.Ordinal) ||
        !int.TryParse(arguments[1], out var seconds) ||
        seconds is < 1 or > 3_600)
    {
        Console.Error.WriteLine(
            "Usage: dotnet run -- --duration-seconds <1-3600>");
        return null;
    }

    return TimeSpan.FromSeconds(seconds);
}

sealed class StressPolicySource : IPolicySource
{
    private string _policyId = "stress-policy-0";

    public string Name => "stress";

    public string PolicyId
    {
        get => Volatile.Read(ref _policyId);
        set => Volatile.Write(ref _policyId, value);
    }

    public async ValueTask<PolicySourceLoadResult> LoadAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Task.Yield();
        cancellationToken.ThrowIfCancellationRequested();

        return PolicySourceLoadResult.Success(
        [
            new PolicyDefinition(
                PolicyId,
                "document",
                "read",
                new PermissionRequirementDefinition(
                    "document.read"))
        ]);
    }
}
