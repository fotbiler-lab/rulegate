using Fotbiler.RuleGate.Abstractions.Attributes;
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

public sealed class
    AuthorizationDiagnosticsInstrumentationTests
{
    [Fact]
    public async Task
        EvaluateAsync_records_requirement_tree()
    {
        var sink = new RecordingDiagnosticsSink();

        var engine =
            CreateEngine(
                new AllRequirementDefinition(
                [
                    new PermissionRequirementDefinition(
                        "document.read",
                        "permission-node"),

                    new AttributeRequirementDefinition(
                        AuthorizationAttributeSource.Resource,
                        "classification",
                        AuthorizationAttributeOperator
                            .LessThanOrEqual,
                        3,
                        "attribute-node")
                ],
                "all-root"),
                sink);

        var decision =
            await engine.EvaluateAsync(
                CreateRequest(
                    permissions:
                    [
                        "document.read"
                    ],
                    resourceAttributes:
                        CreateAttributes(
                            "classification",
                            2)));

        Assert.True(decision.IsAllowed);

        var diagnostic =
            Assert.Single(sink.Diagnostics);

        Assert.True(diagnostic.IsAllowed);
        Assert.Equal(
            "sample-read",
            diagnostic.PolicyId);
        Assert.Empty(diagnostic.FailureCodes);
        Assert.True(
            diagnostic.Duration >= TimeSpan.Zero);

        Assert.Collection(
            diagnostic.RequirementEvaluations,

            root =>
            {
                Assert.Equal(
                    "all-root",
                    root.RequirementId);

                Assert.Equal(
                    AuthorizationRequirementKind.All,
                    root.RequirementKind);

                Assert.Null(
                    root.ParentEvaluationId);

                Assert.Equal(
                    RequirementEvaluationOutcome
                        .Satisfied,
                    root.Outcome);
            },

            permission =>
            {
                Assert.Equal(
                    "permission-node",
                    permission.RequirementId);

                Assert.Equal(
                    AuthorizationRequirementKind
                        .Permission,
                    permission.RequirementKind);

                Assert.Equal(
                    diagnostic.RequirementEvaluations[0]
                        .EvaluationId,
                    permission.ParentEvaluationId);

                Assert.Equal(
                    RequirementEvaluationOutcome
                        .Satisfied,
                    permission.Outcome);
            },

            attribute =>
            {
                Assert.Equal(
                    "attribute-node",
                    attribute.RequirementId);

                Assert.Equal(
                    AuthorizationRequirementKind
                        .Attribute,
                    attribute.RequirementKind);

                Assert.Equal(
                    diagnostic.RequirementEvaluations[0]
                        .EvaluationId,
                    attribute.ParentEvaluationId);

                Assert.Equal(
                    AuthorizationAttributeSource.Resource,
                    attribute.AttributeSource);

                Assert.Equal(
                    "classification",
                    attribute.AttributeName);

                Assert.Equal(
                    RequirementEvaluationOutcome
                        .Satisfied,
                    attribute.Outcome);
            });
    }

    [Fact]
    public async Task
        EvaluateAsync_records_denial_failure_codes()
    {
        var sink = new RecordingDiagnosticsSink();

        var engine =
            CreateEngine(
                new PermissionRequirementDefinition(
                    "document.read",
                    "permission-node"),
                sink);

        var decision =
            await engine.EvaluateAsync(
                CreateRequest());

        Assert.False(decision.IsAllowed);

        var diagnostic =
            Assert.Single(sink.Diagnostics);

        Assert.False(diagnostic.IsAllowed);

        Assert.Equal(
            [AuthorizationFailureCodes
                .MissingPermission],
            diagnostic.FailureCodes);

        var requirement =
            Assert.Single(
                diagnostic.RequirementEvaluations);

        Assert.Equal(
            RequirementEvaluationOutcome
                .NotSatisfied,
            requirement.Outcome);

        Assert.Equal(
            [AuthorizationFailureCodes
                .MissingPermission],
            requirement.FailureCodes);
    }

    [Fact]
    public async Task
        EvaluateAsync_records_no_matching_policy()
    {
        var sink = new RecordingDiagnosticsSink();

        var engine =
            new PolicyAuthorizationEngine(
                new InMemoryPolicyProvider([]),
                CreateDispatcher(),
                sink);

        var decision =
            await engine.EvaluateAsync(
                CreateRequest());

        Assert.False(decision.IsAllowed);

        var diagnostic =
            Assert.Single(sink.Diagnostics);

        Assert.Null(diagnostic.PolicyId);

        Assert.Equal(
            [AuthorizationFailureCodes
                .NoMatchingPolicy],
            diagnostic.FailureCodes);

        Assert.Empty(
            diagnostic.RequirementEvaluations);
    }

    [Fact]
    public async Task
        EvaluateAsync_ignores_sink_failures()
    {
        var engine =
            CreateEngine(
                new PermissionRequirementDefinition(
                    "document.read"),
                new ThrowingDiagnosticsSink());

        var decision =
            await engine.EvaluateAsync(
                CreateRequest(
                    permissions:
                    [
                        "document.read"
                    ]));

        Assert.True(decision.IsAllowed);
    }

    [Fact]
    public async Task
        EvaluateAsync_does_not_write_when_sink_is_disabled()
    {
        var sink =
            new DisabledDiagnosticsSink();

        var engine =
            CreateEngine(
                new PermissionRequirementDefinition(
                    "document.read"),
                sink);

        var decision =
            await engine.EvaluateAsync(
                CreateRequest(
                    permissions:
                    [
                        "document.read"
                    ]));

        Assert.True(decision.IsAllowed);
        Assert.Equal(0, sink.WriteCount);
    }

    private static PolicyAuthorizationEngine
        CreateEngine(
            RequirementDefinition requirement,
            IAuthorizationDiagnosticsSink sink)
    {
        var policy =
            new PolicyDefinition(
                "sample-read",
                "sample-resource",
                "read",
                requirement);

        return new PolicyAuthorizationEngine(
            new InMemoryPolicyProvider(
            [
                policy
            ]),
            CreateDispatcher(),
            sink);
    }

    private static RequirementEvaluationDispatcher
        CreateDispatcher()
    {
        return new RequirementEvaluationDispatcher(
        [
            new PermissionRequirementEvaluator(),
            new RoleRequirementEvaluator(),
            new AttributeRequirementEvaluator(),
            new AllRequirementEvaluator(),
            new AnyRequirementEvaluator(),
            new NotRequirementEvaluator()
        ]);
    }

    private static AuthorizationRequest CreateRequest(
        IEnumerable<string>? permissions = null,
        AuthorizationAttributes? resourceAttributes = null)
    {
        return new AuthorizationRequest(
            new AuthorizationSubject(
                "user-1",
                permissions: permissions),

            new AuthorizationResource(
                "sample-resource",
                "resource-1",
                resourceAttributes),

            "read",

            new AuthorizationContext(
                DateTimeOffset.UnixEpoch));
    }

    private static AuthorizationAttributes
        CreateAttributes(
            string name,
            object? value)
    {
        return new AuthorizationAttributes(
        [
            new KeyValuePair<string, object?>(
                name,
                value)
        ]);
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
            cancellationToken
                .ThrowIfCancellationRequested();

            _diagnostics.Add(diagnostic);

            return ValueTask.CompletedTask;
        }
    }

    private sealed class ThrowingDiagnosticsSink
        : IAuthorizationDiagnosticsSink
    {
        public bool IsEnabled => true;

        public ValueTask WriteAsync(
            AuthorizationEvaluationDiagnostic diagnostic,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException(
                "Diagnostics failure.");
        }
    }

    private sealed class DisabledDiagnosticsSink
        : IAuthorizationDiagnosticsSink
    {
        public bool IsEnabled => false;

        internal int WriteCount { get; private set; }

        public ValueTask WriteAsync(
            AuthorizationEvaluationDiagnostic diagnostic,
            CancellationToken cancellationToken = default)
        {
            WriteCount++;

            return ValueTask.CompletedTask;
        }
    }
}
