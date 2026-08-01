using Fotbiler.RuleGate.Abstractions.Authorization;
using Fotbiler.RuleGate.Abstractions.Constants;
using Fotbiler.RuleGate.Abstractions.Diagnostics;
using Fotbiler.RuleGate.Abstractions.Evaluation;
using Fotbiler.RuleGate.Abstractions.Policies;
using Fotbiler.RuleGate.Core.Engine;
using Fotbiler.RuleGate.Core.Evaluation;
using Fotbiler.RuleGate.Core.Evaluation.Evaluators;
using Fotbiler.RuleGate.Core.Policies;

namespace Fotbiler.RuleGate.Core.Tests;

public sealed class RequirementEvaluationDepthLimitTests
{
    [Fact]
    public async Task
        EvaluateAsync_AcceptsMaximumRequirementDepth()
    {
        var evaluator =
            new CountingRequirementEvaluator();

        var dispatcher =
            CreateDispatcher(evaluator);

        var result =
            await dispatcher.EvaluateAsync(
                CreateNotChain(
                    nodeCount: 64,
                    new CountingRequirementDefinition(
                        "depth-64")),
                CreateContext());

        Assert.False(result.IsIndeterminate);
        Assert.Equal(1, evaluator.EvaluationCount);

        Assert.DoesNotContain(
            result.Failures,
            static failure =>
                failure.Code ==
                AuthorizationFailureCodes
                    .RequirementDepthExceeded);
    }

    [Fact]
    public async Task
        EvaluateAsync_ReturnsIndeterminateAboveMaximumDepth()
    {
        var evaluator =
            new CountingRequirementEvaluator();

        var dispatcher =
            CreateDispatcher(evaluator);

        var result =
            await dispatcher.EvaluateAsync(
                CreateNotChain(
                    nodeCount: 65,
                    new CountingRequirementDefinition(
                        "depth-65")),
                CreateContext());

        Assert.True(result.IsIndeterminate);
        Assert.Equal(0, evaluator.EvaluationCount);

        var failure =
            Assert.Single(result.Failures);

        Assert.Equal(
            AuthorizationFailureCodes
                .RequirementDepthExceeded,
            failure.Code);

        Assert.Equal(
            "depth-65",
            failure.RequirementId);
    }

    [Fact]
    public async Task
        EvaluateAsync_IsolatesDepthAcrossConcurrentEvaluations()
    {
        var evaluator =
            new CountingRequirementEvaluator();

        var dispatcher =
            CreateDispatcher(evaluator);

        var accepted =
            CreateNotChain(
                nodeCount: 64,
                new CountingRequirementDefinition(
                    "accepted"));

        var rejected =
            CreateNotChain(
                nodeCount: 65,
                new CountingRequirementDefinition(
                    "rejected"));

        var context = CreateContext();

        var results =
            await Task.WhenAll(
                Enumerable.Range(
                        0,
                        64)
                    .Select(
                        async index =>
                            await dispatcher.EvaluateAsync(
                                index % 2 == 0
                                    ? accepted
                                    : rejected,
                                context)));

        for (var index = 0;
             index < results.Length;
             index++)
        {
            Assert.Equal(
                index % 2 != 0,
                results[index].IsIndeterminate);
        }

        Assert.Equal(
            32,
            evaluator.EvaluationCount);
    }

    [Fact]
    public async Task
        EvaluateAsync_PreservesCancellationBeforeDepthFailure()
    {
        var evaluator =
            new CountingRequirementEvaluator();

        var dispatcher =
            CreateDispatcher(evaluator);

        using var cancellation =
            new CancellationTokenSource();

        cancellation.Cancel();

        await Assert.ThrowsAsync<
            OperationCanceledException>(
            async () =>
                await dispatcher.EvaluateAsync(
                    CreateNotChain(
                        nodeCount: 65,
                        new CountingRequirementDefinition(
                            "cancelled")),
                    CreateContext(),
                    cancellation.Token));

        Assert.Equal(0, evaluator.EvaluationCount);
    }

    [Fact]
    public async Task
        EvaluateAsync_PreservesCustomEvaluatorExceptions()
    {
        var dispatcher =
            CreateDispatcher(
                new ThrowingRequirementEvaluator());

        await Assert.ThrowsAsync<
            InvalidOperationException>(
            async () =>
                await dispatcher.EvaluateAsync(
                    CreateNotChain(
                        nodeCount: 64,
                        new ThrowingRequirementDefinition(
                            "throwing-depth-64")),
                    CreateContext()));
    }

    [Fact]
    public async Task
        EvaluateAsync_RecordsDiagnosticsForDepthFailure()
    {
        var evaluator =
            new CountingRequirementEvaluator();

        var dispatcher =
            CreateDispatcher(evaluator);

        var sink =
            new RecordingDiagnosticsSink();

        var requirement =
            CreateNotChain(
                nodeCount: 65,
                new CountingRequirementDefinition(
                    "depth-65"));

        var engine =
            CreateEngine(
                requirement,
                dispatcher,
                sink);

        var decision =
            await engine.EvaluateAsync(
                CreateRequest());

        Assert.False(decision.IsAllowed);
        Assert.Equal(0, evaluator.EvaluationCount);

        Assert.Contains(
            decision.Failures,
            static failure =>
                failure.Code ==
                AuthorizationFailureCodes
                    .RequirementDepthExceeded);

        var diagnostic =
            Assert.Single(sink.Diagnostics);

        Assert.False(diagnostic.IsAllowed);

        Assert.Contains(
            AuthorizationFailureCodes
                .RequirementDepthExceeded,
            diagnostic.FailureCodes);

        var evaluations =
            diagnostic.RequirementEvaluations;

        Assert.Equal(
            65,
            evaluations.Count);

        Assert.Null(
            evaluations[0].ParentEvaluationId);

        for (var index = 1;
             index < evaluations.Count;
             index++)
        {
            Assert.Equal(
                evaluations[index - 1]
                    .EvaluationId,
                evaluations[index]
                    .ParentEvaluationId);
        }

        var rejected =
            evaluations[64];

        Assert.Equal(
            "depth-65",
            rejected.RequirementId);

        Assert.Equal(
            AuthorizationRequirementKind.Custom,
            rejected.RequirementKind);

        Assert.Equal(
            RequirementEvaluationOutcome
                .Indeterminate,
            rejected.Outcome);

        Assert.Equal(
            [
                AuthorizationFailureCodes
                    .RequirementDepthExceeded
            ],
            rejected.FailureCodes);
    }

    private static RequirementEvaluationDispatcher
        CreateDispatcher(
            params IRequirementEvaluator[]
                additionalEvaluators)
    {
        var evaluators =
            new List<IRequirementEvaluator>
            {
                new PermissionRequirementEvaluator(),
                new NotRequirementEvaluator()
            };

        evaluators.AddRange(
            additionalEvaluators);

        return new RequirementEvaluationDispatcher(
            evaluators);
    }

    private static PolicyAuthorizationEngine
        CreateEngine(
            RequirementDefinition requirement,
            RequirementEvaluationDispatcher dispatcher,
            IAuthorizationDiagnosticsSink sink)
    {
        var policy =
            new PolicyDefinition(
                "depth-policy",
                "sample-resource",
                "read",
                requirement);

        return new PolicyAuthorizationEngine(
            new InMemoryPolicyProvider(
            [
                policy
            ]),
            dispatcher,
            sink);
    }

    private static RequirementDefinition
        CreateNotChain(
            int nodeCount,
            RequirementDefinition leaf)
    {
        if (nodeCount < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(nodeCount));
        }

        ArgumentNullException.ThrowIfNull(leaf);

        var current = leaf;

        for (var index = 1;
             index < nodeCount;
             index++)
        {
            current =
                new NotRequirementDefinition(
                    current,
                    $"not-{index}");
        }

        return current;
    }

    private static RequirementEvaluationContext
        CreateContext()
    {
        return new RequirementEvaluationContext(
            CreateRequest());
    }

    private static AuthorizationRequest CreateRequest()
    {
        return new AuthorizationRequest(
            subject:
                new AuthorizationSubject(
                    "user-1"),
            resource:
                new AuthorizationResource(
                    "sample-resource",
                    "resource-1"),
            action:
                "read",
            context:
                new AuthorizationContext(
                    DateTimeOffset.UnixEpoch));
    }

    private sealed record
        CountingRequirementDefinition
        : RequirementDefinition
    {
        internal CountingRequirementDefinition(
            string? id = null)
            : base(id)
        {
        }
    }

    private sealed class CountingRequirementEvaluator
        : RequirementEvaluator<
            CountingRequirementDefinition>
    {
        private int _evaluationCount;

        internal int EvaluationCount =>
            Volatile.Read(
                ref _evaluationCount);

        protected override
            ValueTask<RequirementEvaluationResult>
            EvaluateAsync(
                CountingRequirementDefinition requirement,
                RequirementEvaluationContext context,
                IRequirementEvaluationDispatcher dispatcher,
                CancellationToken cancellationToken)
        {
            Interlocked.Increment(
                ref _evaluationCount);

            return new ValueTask<
                RequirementEvaluationResult>(
                RequirementEvaluationResult
                    .Satisfied());
        }
    }

    private sealed record
        ThrowingRequirementDefinition
        : RequirementDefinition
    {
        internal ThrowingRequirementDefinition(
            string? id = null)
            : base(id)
        {
        }
    }

    private sealed class ThrowingRequirementEvaluator
        : RequirementEvaluator<
            ThrowingRequirementDefinition>
    {
        protected override
            ValueTask<RequirementEvaluationResult>
            EvaluateAsync(
                ThrowingRequirementDefinition requirement,
                RequirementEvaluationContext context,
                IRequirementEvaluationDispatcher dispatcher,
                CancellationToken cancellationToken)
        {
            throw new InvalidOperationException(
                "Intentional evaluator failure.");
        }
    }

    private sealed class RecordingDiagnosticsSink
        : IAuthorizationDiagnosticsSink
    {
        private readonly List<
            AuthorizationEvaluationDiagnostic>
            _diagnostics = [];

        public bool IsEnabled => true;

        internal IReadOnlyList<
            AuthorizationEvaluationDiagnostic>
            Diagnostics => _diagnostics;

        public ValueTask WriteAsync(
            AuthorizationEvaluationDiagnostic diagnostic,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(
                diagnostic);

            cancellationToken
                .ThrowIfCancellationRequested();

            _diagnostics.Add(
                diagnostic);

            return default;
        }
    }
}
