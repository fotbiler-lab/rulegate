using System.Security.Claims;
using Fotbiler.RuleGate.Abstractions.Attributes;
using Fotbiler.RuleGate.Abstractions.Authorization;
using Fotbiler.RuleGate.AspNetCore.Enrichment;
using RuleGateAuthorizationContext =
    Fotbiler.RuleGate.Abstractions.Authorization.AuthorizationContext;

namespace Fotbiler.RuleGate.AspNetCore.Tests;

public sealed class
    RuleGateAuthorizationRequestEnricherTests
{
    [Fact]
    public async Task
        EnrichAsync_returns_original_request_without_providers()
    {
        var request = CreateRequest();

        var result = await CreateEnricher()
            .EnrichAsync(
                request,
                CreatePrincipal(),
                frameworkResource: null);

        Assert.True(result.IsSuccessful);
        Assert.Same(request, result.Request);
    }

    [Fact]
    public async Task
        EnrichAsync_runs_ordered_stages_sequentially()
    {
        var calls = new List<string>();

        var lateSubjectProvider =
            new DelegateSubjectProvider(
                order: 20,
                provide: async (context, _) =>
                {
                    await Task.Yield();

                    Assert.Equal(
                        "tenant-1",
                        context.Subject.Attributes[
                            "tenant"]);

                    calls.Add("subject-20");

                    return Success(
                        "clearance",
                        "confidential");
                });

        var earlySubjectProvider =
            new DelegateSubjectProvider(
                order: 10,
                provide: (context, _) =>
                {
                    Assert.Equal(
                        "user-1",
                        context.Subject.Id);

                    calls.Add("subject-10");

                    return ValueTask.FromResult(
                        Success(
                            "tenant",
                            "tenant-1"));
                });

        var resourceProvider =
            new DelegateResourceProvider(
                order: 0,
                provide: (context, _) =>
                {
                    Assert.Equal(
                        "confidential",
                        context.Subject.Attributes[
                            "clearance"]);

                    calls.Add("resource");

                    return ValueTask.FromResult(
                        Success(
                            "ownerId",
                            "user-1"));
                });

        var contextProvider =
            new DelegateContextProvider(
                order: 0,
                provide: (context, _) =>
                {
                    Assert.Equal(
                        "user-1",
                        context.Resource.Attributes[
                            "ownerId"]);

                    calls.Add("context");

                    return ValueTask.FromResult(
                        Success(
                            "requestChannel",
                            "api"));
                });

        var enricher = CreateEnricher(
            subjectProviders:
            [
                lateSubjectProvider,
                earlySubjectProvider,
            ],
            resourceProviders:
            [
                resourceProvider,
            ],
            contextProviders:
            [
                contextProvider,
            ]);

        var result = await enricher.EnrichAsync(
            CreateRequest(),
            CreatePrincipal(),
            frameworkResource: null);

        Assert.True(result.IsSuccessful);
        Assert.Equal(
            [
                "subject-10",
                "subject-20",
                "resource",
                "context",
            ],
            calls);

        Assert.Equal(
            "tenant-1",
            result.Request!.Subject.Attributes[
                "tenant"]);

        Assert.Equal(
            "confidential",
            result.Request.Subject.Attributes[
                "clearance"]);

        Assert.Equal(
            "user-1",
            result.Request.Resource.Attributes[
                "ownerId"]);

        Assert.Equal(
            "api",
            result.Request.Context.Attributes[
                "requestChannel"]);
    }

    [Fact]
    public async Task
        EnrichAsync_preserves_registration_order_for_equal_orders()
    {
        var calls = new List<string>();

        var first = new DelegateSubjectProvider(
            order: 5,
            provide: (_, _) =>
            {
                calls.Add("first");

                return ValueTask.FromResult(
                    RuleGateAttributeProviderResult
                        .Success());
            });

        var second = new DelegateSubjectProvider(
            order: 5,
            provide: (_, _) =>
            {
                calls.Add("second");

                return ValueTask.FromResult(
                    RuleGateAttributeProviderResult
                        .Success());
            });

        var result = await CreateEnricher(
                subjectProviders:
                [
                    first,
                    second,
                ])
            .EnrichAsync(
                CreateRequest(),
                CreatePrincipal(),
                frameworkResource: null);

        Assert.True(result.IsSuccessful);
        Assert.Equal(
            [
                "first",
                "second",
            ],
            calls);
    }

    [Theory]
    [InlineData(
        RuleGateAttributeCollisionBehavior.Fail,
        null)]
    [InlineData(
        RuleGateAttributeCollisionBehavior.KeepExisting,
        "original")]
    [InlineData(
        RuleGateAttributeCollisionBehavior.ReplaceExisting,
        "replacement")]
    public async Task
        EnrichAsync_applies_collision_behavior(
            RuleGateAttributeCollisionBehavior behavior,
            string? expectedValue)
    {
        var provider = new DelegateSubjectProvider(
            order: 0,
            collisionBehavior: behavior,
            provide: (_, _) =>
                ValueTask.FromResult(
                    Success(
                        "tenant",
                        "replacement")));

        var request = CreateRequest(
            subjectAttributes:
                Attributes(
                    "tenant",
                    "original"));

        var result = await CreateEnricher(
                subjectProviders:
                [
                    provider,
                ])
            .EnrichAsync(
                request,
                CreatePrincipal(),
                frameworkResource: null);

        if (behavior ==
            RuleGateAttributeCollisionBehavior.Fail)
        {
            Assert.False(result.IsSuccessful);
            Assert.Null(result.Request);
            return;
        }

        Assert.True(result.IsSuccessful);
        Assert.Equal(
            expectedValue,
            result.Request!.Subject.Attributes[
                "tenant"]);
    }

    [Theory]
    [InlineData(
        RuleGateAttributeProviderResultStatus
            .MissingRequiredData,
        RuleGateEnrichmentOutcome.MissingRequiredData)]
    [InlineData(
        RuleGateAttributeProviderResultStatus.Failed,
        RuleGateEnrichmentOutcome.ProviderFailed)]
    public async Task
        EnrichAsync_fails_closed_for_provider_result(
            RuleGateAttributeProviderResultStatus status,
            RuleGateEnrichmentOutcome expectedOutcome)
    {
        var laterProviderCalled = false;
        var sink = new RecordingDiagnosticsSink();

        var failingProvider =
            new DelegateSubjectProvider(
                order: 0,
                provide: (_, _) =>
                    ValueTask.FromResult(
                        status ==
                        RuleGateAttributeProviderResultStatus
                            .MissingRequiredData
                            ? RuleGateAttributeProviderResult
                                .MissingRequiredData()
                            : RuleGateAttributeProviderResult
                                .Fail()));

        var laterProvider =
            new DelegateSubjectProvider(
                order: 1,
                provide: (_, _) =>
                {
                    laterProviderCalled = true;

                    return ValueTask.FromResult(
                        RuleGateAttributeProviderResult
                            .Success());
                });

        var result = await CreateEnricher(
                subjectProviders:
                [
                    failingProvider,
                    laterProvider,
                ],
                diagnosticsSinks:
                [
                    sink,
                ])
            .EnrichAsync(
                CreateRequest(),
                CreatePrincipal(),
                frameworkResource: null);

        Assert.False(result.IsSuccessful);
        Assert.False(laterProviderCalled);

        var diagnostic = Assert.Single(
            sink.Diagnostics);

        Assert.Equal(
            expectedOutcome,
            diagnostic.Outcome);
        Assert.Equal(0, diagnostic.AttributeCount);
    }

    [Fact]
    public async Task
        EnrichAsync_fails_closed_for_provider_exception()
    {
        const string sensitiveValue =
            "secret-provider-value";

        var sink = new RecordingDiagnosticsSink();

        var provider = new DelegateContextProvider(
            order: 0,
            provide: (_, _) =>
                throw new InvalidOperationException(
                    sensitiveValue));

        var result = await CreateEnricher(
                contextProviders:
                [
                    provider,
                ],
                diagnosticsSinks:
                [
                    sink,
                ])
            .EnrichAsync(
                CreateRequest(),
                CreatePrincipal(),
                frameworkResource: null);

        Assert.False(result.IsSuccessful);

        var diagnostic = Assert.Single(
            sink.Diagnostics);

        Assert.Equal(
            RuleGateEnrichmentOutcome.ProviderException,
            diagnostic.Outcome);

        Assert.DoesNotContain(
            sensitiveValue,
            diagnostic.ProviderName,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task
        EnrichAsync_fails_closed_for_invalid_attribute()
    {
        var sink = new RecordingDiagnosticsSink();

        var provider = new DelegateResourceProvider(
            order: 0,
            provide: (_, _) =>
                ValueTask.FromResult(
                    Success(
                        "unsupported",
                        new object())));

        var result = await CreateEnricher(
                resourceProviders:
                [
                    provider,
                ],
                diagnosticsSinks:
                [
                    sink,
                ])
            .EnrichAsync(
                CreateRequest(),
                CreatePrincipal(),
                frameworkResource: null);

        Assert.False(result.IsSuccessful);

        Assert.Equal(
            RuleGateEnrichmentOutcome.InvalidAttribute,
            Assert.Single(
                    sink.Diagnostics)
                .Outcome);
    }

    [Fact]
    public async Task
        EnrichAsync_stops_when_cancellation_is_requested()
    {
        using var cancellationSource =
            new CancellationTokenSource();

        var laterProviderCalled = false;

        var cancellingProvider =
            new DelegateSubjectProvider(
                order: 0,
                provide: (_, cancellationToken) =>
                {
                    Assert.Equal(
                        cancellationSource.Token,
                        cancellationToken);

                    cancellationSource.Cancel();

                    return ValueTask.FromResult(
                        RuleGateAttributeProviderResult
                            .Success());
                });

        var laterProvider =
            new DelegateSubjectProvider(
                order: 1,
                provide: (_, _) =>
                {
                    laterProviderCalled = true;

                    return ValueTask.FromResult(
                        RuleGateAttributeProviderResult
                            .Success());
                });

        var result = await CreateEnricher(
                subjectProviders:
                [
                    cancellingProvider,
                    laterProvider,
                ])
            .EnrichAsync(
                CreateRequest(),
                CreatePrincipal(),
                frameworkResource: null,
                cancellationSource.Token);

        Assert.False(result.IsSuccessful);
        Assert.False(laterProviderCalled);
    }

    [Fact]
    public async Task
        EnrichAsync_ignores_diagnostics_sink_failure()
    {
        var provider = new DelegateSubjectProvider(
            order: 0,
            provide: (_, _) =>
                ValueTask.FromResult(
                    Success(
                        "tenant",
                        "tenant-1")));

        var result = await CreateEnricher(
                subjectProviders:
                [
                    provider,
                ],
                diagnosticsSinks:
                [
                    new ThrowingDiagnosticsSink(),
                ])
            .EnrichAsync(
                CreateRequest(),
                CreatePrincipal(),
                frameworkResource: null);

        Assert.True(result.IsSuccessful);
        Assert.Equal(
            "tenant-1",
            result.Request!.Subject.Attributes[
                "tenant"]);
    }

    private static RuleGateAuthorizationRequestEnricher
        CreateEnricher(
            IEnumerable<IRuleGateSubjectAttributeProvider>?
                subjectProviders = null,
            IEnumerable<IRuleGateResourceAttributeProvider>?
                resourceProviders = null,
            IEnumerable<IRuleGateContextAttributeProvider>?
                contextProviders = null,
            IEnumerable<IRuleGateEnrichmentDiagnosticsSink>?
                diagnosticsSinks = null)
    {
        return new RuleGateAuthorizationRequestEnricher(
            subjectProviders ?? [],
            resourceProviders ?? [],
            contextProviders ?? [],
            diagnosticsSinks ?? []);
    }

    private static AuthorizationRequest CreateRequest(
        AuthorizationAttributes? subjectAttributes = null)
    {
        return new AuthorizationRequest(
            new AuthorizationSubject(
                "user-1",
                attributes: subjectAttributes),
            new AuthorizationResource(
                "document",
                "document-1"),
            "read",
            new RuleGateAuthorizationContext(
                DateTimeOffset.UnixEpoch));
    }

    private static ClaimsPrincipal CreatePrincipal()
    {
        return new ClaimsPrincipal(
            new ClaimsIdentity(
                authenticationType: "Test"));
    }

    private static RuleGateAttributeProviderResult
        Success(
            string name,
            object? value)
    {
        return RuleGateAttributeProviderResult.Success(
            Attributes(
                name,
                value));
    }

    private static AuthorizationAttributes Attributes(
        string name,
        object? value)
    {
        return new AuthorizationAttributes(
        [
            new KeyValuePair<string, object?>(
                name,
                value),
        ]);
    }

    private sealed class DelegateSubjectProvider
        : IRuleGateSubjectAttributeProvider
    {
        private readonly Func<
            RuleGateAttributeProviderContext,
            CancellationToken,
            ValueTask<RuleGateAttributeProviderResult>>
            _provide;

        public DelegateSubjectProvider(
            int order,
            Func<
                RuleGateAttributeProviderContext,
                CancellationToken,
                ValueTask<RuleGateAttributeProviderResult>>
                provide,
            RuleGateAttributeCollisionBehavior
                collisionBehavior =
                    RuleGateAttributeCollisionBehavior.Fail)
        {
            Order = order;
            CollisionBehavior = collisionBehavior;
            _provide = provide;
        }

        public int Order { get; }

        public RuleGateAttributeCollisionBehavior
            CollisionBehavior
        { get; }

        public ValueTask<RuleGateAttributeProviderResult>
            ProvideAttributesAsync(
                RuleGateAttributeProviderContext context,
                CancellationToken cancellationToken = default)
        {
            return _provide(
                context,
                cancellationToken);
        }
    }

    private sealed class DelegateResourceProvider
        : IRuleGateResourceAttributeProvider
    {
        private readonly Func<
            RuleGateAttributeProviderContext,
            CancellationToken,
            ValueTask<RuleGateAttributeProviderResult>>
            _provide;

        public DelegateResourceProvider(
            int order,
            Func<
                RuleGateAttributeProviderContext,
                CancellationToken,
                ValueTask<RuleGateAttributeProviderResult>>
                provide,
            RuleGateAttributeCollisionBehavior
                collisionBehavior =
                    RuleGateAttributeCollisionBehavior.Fail)
        {
            Order = order;
            CollisionBehavior = collisionBehavior;
            _provide = provide;
        }

        public int Order { get; }

        public RuleGateAttributeCollisionBehavior
            CollisionBehavior
        { get; }

        public ValueTask<RuleGateAttributeProviderResult>
            ProvideAttributesAsync(
                RuleGateAttributeProviderContext context,
                CancellationToken cancellationToken = default)
        {
            return _provide(
                context,
                cancellationToken);
        }
    }

    private sealed class DelegateContextProvider
        : IRuleGateContextAttributeProvider
    {
        private readonly Func<
            RuleGateAttributeProviderContext,
            CancellationToken,
            ValueTask<RuleGateAttributeProviderResult>>
            _provide;

        public DelegateContextProvider(
            int order,
            Func<
                RuleGateAttributeProviderContext,
                CancellationToken,
                ValueTask<RuleGateAttributeProviderResult>>
                provide,
            RuleGateAttributeCollisionBehavior
                collisionBehavior =
                    RuleGateAttributeCollisionBehavior.Fail)
        {
            Order = order;
            CollisionBehavior = collisionBehavior;
            _provide = provide;
        }

        public int Order { get; }

        public RuleGateAttributeCollisionBehavior
            CollisionBehavior
        { get; }

        public ValueTask<RuleGateAttributeProviderResult>
            ProvideAttributesAsync(
                RuleGateAttributeProviderContext context,
                CancellationToken cancellationToken = default)
        {
            return _provide(
                context,
                cancellationToken);
        }
    }

    private sealed class RecordingDiagnosticsSink
        : IRuleGateEnrichmentDiagnosticsSink
    {
        public List<RuleGateEnrichmentDiagnostic>
            Diagnostics
        { get; } = [];

        public ValueTask WriteAsync(
            RuleGateEnrichmentDiagnostic diagnostic,
            CancellationToken cancellationToken = default)
        {
            Diagnostics.Add(diagnostic);

            return ValueTask.CompletedTask;
        }
    }

    private sealed class ThrowingDiagnosticsSink
        : IRuleGateEnrichmentDiagnosticsSink
    {
        public ValueTask WriteAsync(
            RuleGateEnrichmentDiagnostic diagnostic,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException();
        }
    }
}
