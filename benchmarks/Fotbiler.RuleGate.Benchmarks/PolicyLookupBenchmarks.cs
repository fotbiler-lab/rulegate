using BenchmarkDotNet.Attributes;
using Fotbiler.RuleGate.Abstractions.Policies;
using Fotbiler.RuleGate.Core.Policies;

namespace Fotbiler.RuleGate.Benchmarks;

[MemoryDiagnoser]
public class PolicyLookupBenchmarks
{
    private InMemoryPolicyProvider _inMemory = null!;
    private AtomicPolicyProvider _atomic = null!;
    private string _targetResource = string.Empty;

    [Params(10, 100, 1_000, 10_000)]
    public int PolicyCount { get; set; }

    [GlobalSetup]
    public async Task Setup()
    {
        var policies = Enumerable.Range(0, PolicyCount)
            .Select(
                index => new PolicyDefinition(
                    $"policy-{index}",
                    $"resource-{index}",
                    "read",
                    new PermissionRequirementDefinition(
                        "document.read")))
            .ToArray();

        _targetResource =
            $"resource-{PolicyCount - 1}";
        _inMemory = new InMemoryPolicyProvider(policies);
        _atomic = new AtomicPolicyProvider(
        [
            new InMemoryPolicySource(
                policies,
                "benchmark-source")
        ]);

        await _atomic.ReloadAsync();
    }

    [Benchmark(Baseline = true)]
    public ValueTask<PolicyDefinition?> InMemoryHit()
    {
        return _inMemory.FindAsync(
            _targetResource,
            "read");
    }

    [Benchmark]
    public ValueTask<PolicyDefinition?> InMemoryMiss()
    {
        return _inMemory.FindAsync(
            "missing-resource",
            "read");
    }

    [Benchmark]
    public ValueTask<PolicyDefinition?> AtomicSnapshotHit()
    {
        return _atomic.FindAsync(
            _targetResource,
            "read");
    }
}
