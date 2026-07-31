using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using Fotbiler.RuleGate.Abstractions.Authorization;
using Fotbiler.RuleGate.Abstractions.Diagnostics;
using Fotbiler.RuleGate.Abstractions.Evaluation;
using Fotbiler.RuleGate.Abstractions.Policies;
using Fotbiler.RuleGate.Core.Engine;
using Fotbiler.RuleGate.Core.Evaluation;
using Fotbiler.RuleGate.Core.Evaluation.Evaluators;
using Fotbiler.RuleGate.Core.Policies;

namespace Fotbiler.RuleGate.Core.Tests;

[Collection(nameof(RuleGateTelemetryCollection))]
public sealed class RuleGateTelemetryTests
{
    [Fact]
    public async Task Authorization_emits_low_cardinality_activity_and_metrics()
    {
        using var activities = new ActivityCollector();
        using var metrics = new MetricCollector();

        var engine = CreateEngine(
            new PermissionRequirementDefinition(
                "sensitive.permission"));

        var decision = await engine.EvaluateAsync(
            CreateRequest(
                resourceType: "secret-resource-type",
                subjectId: "secret-subject-id",
                resourceId: "secret-resource-id"));

        Assert.False(decision.IsAllowed);

        var activity = Assert.Single(
            activities.Completed,
            item => item.OperationName ==
                RuleGateTelemetry.AuthorizationActivityName);

        Assert.Equal(
            "deny",
            activity.GetTagItem(
                "rulegate.authorization.outcome"));
        Assert.Equal(
            "not_satisfied",
            activity.GetTagItem(
                "rulegate.authorization.failure_category"));
        Assert.Equal(
            true,
            activity.GetTagItem(
                "rulegate.policy.matched"));

        AssertTelemetryDoesNotContain(
            activity,
            "secret-subject-id",
            "secret-resource-id",
            "secret-resource-type",
            "sensitive.permission",
            "sample-policy");

        Assert.Contains(
            metrics.Measurements,
            item =>
                item.Name ==
                    "rulegate.authorization.evaluations" &&
                item.LongValue == 1 &&
                item.Tags[
                    "rulegate.authorization.outcome"] ==
                    "deny" &&
                item.Tags[
                    "rulegate.authorization.failure_category"] ==
                    "not_satisfied");

        Assert.Contains(
            metrics.Measurements,
            item =>
                item.Name ==
                    "rulegate.authorization.duration" &&
                item.DoubleValue >= 0);

        Assert.Contains(
            metrics.Measurements,
            item =>
                item.Name == "rulegate.policy.lookups" &&
                item.Tags["rulegate.policy.matched"] ==
                    "True");

        Assert.All(
            metrics.Measurements,
            measurement => AssertTelemetryDoesNotContain(
                measurement,
                "secret-subject-id",
                "secret-resource-id",
                "secret-resource-type",
                "sensitive.permission",
                "sample-policy"));
    }

    [Fact]
    public async Task Missing_policy_uses_bounded_failure_category()
    {
        using var activities = new ActivityCollector();
        using var metrics = new MetricCollector();

        var engine = CreateEngine(
            new PermissionRequirementDefinition(
                "document.read"));

        var decision = await engine.EvaluateAsync(
            CreateRequest(resourceType: "missing"));

        Assert.False(decision.IsAllowed);

        var activity = Assert.Single(
            activities.Completed,
            item => item.OperationName ==
                RuleGateTelemetry.AuthorizationActivityName);

        Assert.Equal(
            "no_matching_policy",
            activity.GetTagItem(
                "rulegate.authorization.failure_category"));
        Assert.Equal(
            false,
            activity.GetTagItem(
                "rulegate.policy.matched"));

        Assert.Contains(
            metrics.Measurements,
            item =>
                item.Name == "rulegate.policy.lookups" &&
                item.Tags["rulegate.policy.matched"] ==
                    "False");
    }

    [Fact]
    public async Task Authorization_activity_preserves_ambient_trace_context()
    {
        using var activities = new ActivityCollector();
        using var requestActivity =
            new Activity("sample.aspnetcore.request").Start();

        var engine = CreateEngine(
            new PermissionRequirementDefinition(
                "document.read"));

        _ = await engine.EvaluateAsync(
            CreateRequest());

        var authorizationActivity = Assert.Single(
            activities.Completed,
            item => item.OperationName ==
                RuleGateTelemetry.AuthorizationActivityName);

        Assert.Equal(
            requestActivity.TraceId,
            authorizationActivity.TraceId);
        Assert.Equal(
            requestActivity.SpanId,
            authorizationActivity.ParentSpanId);
    }

    [Fact]
    public async Task Indeterminate_cancelled_and_error_states_are_bounded()
    {
        using var activities = new ActivityCollector();

        var indeterminateEngine = CreateEngine(
            new UnsupportedRequirementDefinition());

        var indeterminate = await indeterminateEngine.EvaluateAsync(
            CreateRequest());

        Assert.False(indeterminate.IsAllowed);

        using var cancellation =
            new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await indeterminateEngine.EvaluateAsync(
                CreateRequest(),
                cancellation.Token));

        var errorEngine = new PolicyAuthorizationEngine(
            new ThrowingPolicyProvider(),
            new RequirementEvaluationDispatcher([]));

        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await errorEngine.EvaluateAsync(
                CreateRequest()));

        var authorizationActivities = activities.Completed
            .Where(
                item => item.OperationName ==
                    RuleGateTelemetry.AuthorizationActivityName)
            .ToArray();

        Assert.Equal(3, authorizationActivities.Length);
        Assert.Contains(
            authorizationActivities,
            activity => Equals(
                "indeterminate",
                activity.GetTagItem(
                    "rulegate.authorization.failure_category")));
        Assert.Contains(
            authorizationActivities,
            activity => Equals(
                "cancelled",
                activity.GetTagItem(
                    "rulegate.authorization.failure_category")));
        Assert.Contains(
            authorizationActivities,
            activity => Equals(
                "error",
                activity.GetTagItem(
                    "rulegate.authorization.failure_category")));
        Assert.DoesNotContain(
            authorizationActivities,
            activity => activity.TagObjects.Any(
                tag => string.Equals(
                    tag.Value?.ToString(),
                    "Sensitive provider detail.",
                    StringComparison.Ordinal)));
    }

    [Fact]
    public async Task Reload_emits_safe_source_and_snapshot_telemetry()
    {
        using var activities = new ActivityCollector();
        using var metrics = new MetricCollector();

        var source = new TestPolicySource(
            "secret-source-name",
            PolicySourceLoadResult.Success(
            [
                new PolicyDefinition(
                    "secret-policy-id",
                    "document",
                    "read",
                    new PermissionRequirementDefinition(
                        "document.read"))
            ]));

        var provider = new AtomicPolicyProvider([source]);
        var activated = await provider.ReloadAsync();

        source.Result = PolicySourceLoadResult.Failure(
        [
            new PolicySourceDiagnostic(
                "SECRET_DIAGNOSTIC",
                "Sensitive source detail.")
        ]);

        var rejected = await provider.ReloadAsync();

        using var cancellation =
            new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () => await provider.ReloadAsync(
                cancellation.Token));

        Assert.True(activated.IsActivated);
        Assert.False(rejected.IsActivated);

        var reloadActivities = activities.Completed
            .Where(
                item => item.OperationName ==
                    RuleGateTelemetry.PolicyReloadActivityName)
            .ToArray();

        Assert.Equal(3, reloadActivities.Length);
        Assert.Contains(
            reloadActivities,
            activity => Equals(
                "activated",
                activity.GetTagItem(
                    "rulegate.policy.reload.result")));
        Assert.Contains(
            reloadActivities,
            activity => Equals(
                "rejected",
                activity.GetTagItem(
                    "rulegate.policy.reload.result")));
        Assert.Contains(
            reloadActivities,
            activity => Equals(
                "cancelled",
                activity.GetTagItem(
                    "rulegate.policy.reload.result")));

        Assert.All(
            activities.Completed,
            activity => AssertTelemetryDoesNotContain(
                activity,
                "secret-source-name",
                "secret-policy-id",
                "SECRET_DIAGNOSTIC",
                "Sensitive source detail."));

        Assert.Contains(
            metrics.Measurements,
            item =>
                item.Name == "rulegate.policy.reloads" &&
                item.Tags[
                    "rulegate.policy.reload.result"] ==
                    "activated");
        Assert.Contains(
            metrics.Measurements,
            item =>
                item.Name == "rulegate.policy.reloads" &&
                item.Tags[
                    "rulegate.policy.reload.result"] ==
                    "rejected");
        Assert.Contains(
            metrics.Measurements,
            item =>
                item.Name == "rulegate.policy.reloads" &&
                item.Tags[
                    "rulegate.policy.reload.result"] ==
                    "cancelled");
        Assert.Contains(
            metrics.Measurements,
            item =>
                item.Name ==
                    "rulegate.policy.snapshot.policy_count" &&
                item.LongValue == 1);
    }

    private static PolicyAuthorizationEngine CreateEngine(
        RequirementDefinition requirement)
    {
        return new PolicyAuthorizationEngine(
            new InMemoryPolicyProvider(
            [
                new PolicyDefinition(
                    "sample-policy",
                    "secret-resource-type",
                    "read",
                    requirement)
            ]),
            new RequirementEvaluationDispatcher(
            [
                new PermissionRequirementEvaluator()
            ]));
    }

    private static AuthorizationRequest CreateRequest(
        string resourceType = "secret-resource-type",
        string subjectId = "subject",
        string resourceId = "resource")
    {
        return new AuthorizationRequest(
            new AuthorizationSubject(subjectId),
            new AuthorizationResource(
                resourceType,
                resourceId),
            "read",
            new AuthorizationContext(
                DateTimeOffset.UtcNow));
    }

    private static void AssertTelemetryDoesNotContain(
        Activity activity,
        params string[] sensitiveValues)
    {
        var telemetry = string.Join(
            "|",
            activity.TagObjects.Select(
                static tag =>
                    $"{tag.Key}={tag.Value}"));

        foreach (var value in sensitiveValues)
        {
            Assert.DoesNotContain(
                value,
                telemetry,
                StringComparison.Ordinal);
        }
    }

    private static void AssertTelemetryDoesNotContain(
        MetricMeasurement measurement,
        params string[] sensitiveValues)
    {
        var telemetry = string.Join(
            "|",
            measurement.Tags.Select(
                static tag =>
                    $"{tag.Key}={tag.Value}"));

        foreach (var value in sensitiveValues)
        {
            Assert.DoesNotContain(
                value,
                telemetry,
                StringComparison.Ordinal);
        }
    }

    private sealed class ActivityCollector : IDisposable
    {
        private readonly ActivityListener _listener;

        public ActivityCollector()
        {
            _listener = new ActivityListener
            {
                ShouldListenTo =
                    static source => source.Name ==
                        RuleGateTelemetry.ActivitySourceName,
                Sample = static (ref ActivityCreationOptions<
                        ActivityContext> options) =>
                    ActivitySamplingResult.AllData,
                ActivityStopped = activity =>
                    Completed.Enqueue(activity),
            };

            ActivitySource.AddActivityListener(
                _listener);
        }

        public ConcurrentQueue<Activity> Completed { get; } = [];

        public void Dispose()
        {
            _listener.Dispose();
        }
    }

    private sealed class MetricCollector : IDisposable
    {
        private readonly MeterListener _listener = new();

        public MetricCollector()
        {
            _listener.InstrumentPublished =
                (instrument, listener) =>
                {
                    if (instrument.Meter.Name ==
                        RuleGateTelemetry.MeterName)
                    {
                        listener.EnableMeasurementEvents(
                            instrument);
                    }
                };

            _listener.SetMeasurementEventCallback<long>(
                (instrument,
                    measurement,
                    tags,
                    state) =>
                {
                    _ = state;
                    Measurements.Enqueue(
                        MetricMeasurement.FromLong(
                            instrument.Name,
                            measurement,
                            tags));
                });

            _listener.SetMeasurementEventCallback<double>(
                (instrument,
                    measurement,
                    tags,
                    state) =>
                {
                    _ = state;
                    Measurements.Enqueue(
                        MetricMeasurement.FromDouble(
                            instrument.Name,
                            measurement,
                            tags));
                });

            _listener.Start();
        }

        public ConcurrentQueue<MetricMeasurement> Measurements { get; } = [];

        public void Dispose()
        {
            _listener.Dispose();
        }
    }

    private sealed record MetricMeasurement(
        string Name,
        long? LongValue,
        double? DoubleValue,
        IReadOnlyDictionary<string, string> Tags)
    {
        public static MetricMeasurement FromLong(
            string name,
            long value,
            ReadOnlySpan<KeyValuePair<string, object?>> tags)
        {
            return new MetricMeasurement(
                name,
                value,
                null,
                CopyTags(tags));
        }

        public static MetricMeasurement FromDouble(
            string name,
            double value,
            ReadOnlySpan<KeyValuePair<string, object?>> tags)
        {
            return new MetricMeasurement(
                name,
                null,
                value,
                CopyTags(tags));
        }

        private static IReadOnlyDictionary<string, string>
            CopyTags(
                ReadOnlySpan<KeyValuePair<string, object?>> tags)
        {
            return tags.ToArray().ToDictionary(
                static item => item.Key,
                static item => item.Value?.ToString() ??
                    string.Empty,
                StringComparer.Ordinal);
        }
    }

    private sealed class TestPolicySource : IPolicySource
    {
        public TestPolicySource(
            string name,
            PolicySourceLoadResult result)
        {
            Name = name;
            Result = result;
        }

        public string Name { get; }

        public PolicySourceLoadResult Result { get; set; }

        public ValueTask<PolicySourceLoadResult> LoadAsync(
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(Result);
        }
    }

    private sealed record UnsupportedRequirementDefinition
        : RequirementDefinition;

    private sealed class ThrowingPolicyProvider : IPolicyProvider
    {
        public ValueTask<PolicyDefinition?> FindAsync(
            string resourceType,
            string action,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException(
                "Sensitive provider detail.");
        }
    }
}

[CollectionDefinition(
    nameof(RuleGateTelemetryCollection),
    DisableParallelization = true)]
public sealed class RuleGateTelemetryCollection;
