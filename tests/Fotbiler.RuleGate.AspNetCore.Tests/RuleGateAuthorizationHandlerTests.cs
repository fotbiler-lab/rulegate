using System.Security.Claims;
using Fotbiler.RuleGate.Abstractions.Authorization;
using Fotbiler.RuleGate.AspNetCore.Authorization;
using Fotbiler.RuleGate.AspNetCore.Enrichment;
using Fotbiler.RuleGate.AspNetCore.Subjects;
using Fotbiler.RuleGate.AspNetCore.Time;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using RuleGateAuthorizationFailure =
    Fotbiler.RuleGate.Abstractions.Authorization.AuthorizationFailure;

namespace Fotbiler.RuleGate.AspNetCore.Tests;

public sealed class RuleGateAuthorizationHandlerTests
{
    [Fact]
    public async Task
        HandleAsync_SucceedsAllowedRequirement()
    {
        var subject =
            new AuthorizationSubject("user-1");

        var resource =
            new AuthorizationResource(
                "document",
                "document-1");

        var requirement =
            new RuleGateAuthorizationRequirement(
                "read");

        var handler =
            CreateHandler(
                evaluate:
                    _ => AuthorizationDecision.Allow(),
                createSubject: _ => subject,
                createResource: _ => resource);

        var context =
            CreateContext(
                requirement,
                resource);

        await handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
        Assert.False(context.HasFailed);
        Assert.Empty(
            context.PendingRequirements);
    }

    [Fact]
    public async Task
        HandleAsync_FailsDeniedRequirement()
    {
        var subject =
            new AuthorizationSubject("user-1");

        var resource =
            new AuthorizationResource(
                "document",
                "document-1");

        var requirement =
            new RuleGateAuthorizationRequirement(
                "read");

        var handler =
            CreateHandler(
                evaluate:
                    _ => AuthorizationDecision.Deny(
                        new RuleGateAuthorizationFailure(
                            "denied")),
                createSubject: _ => subject,
                createResource: _ => resource);

        var context =
            CreateContext(
                requirement,
                resource);

        await handler.HandleAsync(context);

        Assert.True(context.HasFailed);
        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task
        HandleAsync_FailsWhenSubjectMappingFails()
    {
        var engineCalled = false;

        var requirement =
            new RuleGateAuthorizationRequirement(
                "read");

        var resource =
            new AuthorizationResource(
                "document");

        var handler =
            CreateHandler(
                evaluate:
                    _ =>
                    {
                        engineCalled = true;

                        return AuthorizationDecision
                            .Allow();
                    },
                createSubject:
                    _ => throw
                        new InvalidOperationException(),
                createResource: _ => resource);

        var context =
            CreateContext(
                requirement,
                resource);

        await handler.HandleAsync(context);

        Assert.True(context.HasFailed);
        Assert.False(engineCalled);
    }

    [Fact]
    public async Task
        HandleAsync_FailsWhenResourceMappingFails()
    {
        var engineCalled = false;

        var subject =
            new AuthorizationSubject("user-1");

        var requirement =
            new RuleGateAuthorizationRequirement(
                "read");

        var handler =
            CreateHandler(
                evaluate:
                    _ =>
                    {
                        engineCalled = true;

                        return AuthorizationDecision
                            .Allow();
                    },
                createSubject: _ => subject,
                createResource:
                    _ => throw
                        new InvalidOperationException());

        var context =
            CreateContext(
                requirement,
                resource: new object());

        await handler.HandleAsync(context);

        Assert.True(context.HasFailed);
        Assert.False(engineCalled);
    }

    [Fact]
    public async Task
        HandleAsync_FailsWhenResourceTypeDoesNotMatch()
    {
        var engineCalled = false;

        var subject =
            new AuthorizationSubject(
                "user-1");

        var resource =
            new AuthorizationResource(
                "invoice",
                "invoice-1");

        var requirement =
            new RuleGateAuthorizationRequirement(
                resourceType: "document",
                action: "read");

        var handler =
            CreateHandler(
                evaluate:
                    _ =>
                    {
                        engineCalled = true;

                        return AuthorizationDecision
                            .Allow();
                    },
                createSubject: _ => subject,
                createResource: _ => resource);

        var context =
            CreateContext(
                requirement,
                resource);

        await handler.HandleAsync(context);

        Assert.True(context.HasFailed);
        Assert.False(context.HasSucceeded);
        Assert.False(engineCalled);
    }

    [Fact]
    public async Task
        HandleAsync_fails_cancelled_http_request_before_engine()
    {
        var engineCalled = false;

        var subject =
            new AuthorizationSubject("user-1");

        var resource =
            new AuthorizationResource(
                "document",
                "document-1");

        var requirement =
            new RuleGateAuthorizationRequirement(
                resourceType: "document",
                action: "read");

        using var cancellationSource =
            new CancellationTokenSource();

        cancellationSource.Cancel();

        var httpContext =
            new DefaultHttpContext
            {
                RequestAborted =
                    cancellationSource.Token,
            };

        var handler = CreateHandler(
            evaluate: _ =>
            {
                engineCalled = true;

                return AuthorizationDecision.Allow();
            },
            createSubject: _ => subject,
            createResource: _ => resource);

        var context = CreateContext(
            requirement,
            httpContext);

        await handler.HandleAsync(context);

        Assert.True(context.HasFailed);
        Assert.False(engineCalled);
    }

    [Fact]
    public async Task
        HandleAsync_CreatesExpectedRequest()
    {
        var evaluationTime =
            new DateTimeOffset(
                2026,
                7,
                26,
                12,
                0,
                0,
                TimeSpan.Zero);

        var subject =
            new AuthorizationSubject(
                "user-1");

        var resource =
            new AuthorizationResource(
                "document",
                "document-1");

        AuthorizationRequest? observedRequest =
            null;

        var requirement =
            new RuleGateAuthorizationRequirement(
                "document.read");

        var handler =
            CreateHandler(
                evaluate:
                    request =>
                    {
                        observedRequest = request;

                        return AuthorizationDecision
                            .Allow();
                    },
                createSubject: _ => subject,
                createResource: _ => resource,
                clock:
                    new TestRuleGateClock(
                        evaluationTime));

        var context =
            CreateContext(
                requirement,
                resource);

        await handler.HandleAsync(context);

        var request =
            Assert.IsType<AuthorizationRequest>(
                observedRequest);

        Assert.Same(
            subject,
            request.Subject);

        Assert.Same(
            resource,
            request.Resource);

        Assert.Equal(
            "document.read",
            request.Action);

        Assert.Equal(
            evaluationTime,
            request.Context.EvaluationTime);
    }

    [Fact]
    public async Task
        HandleAsync_forwards_http_request_cancellation()
    {
        var subject =
            new AuthorizationSubject("user-1");

        var resource =
            new AuthorizationResource("document");

        var requirement =
            new RuleGateAuthorizationRequirement(
                resourceType: "document",
                action: "read");

        using var cancellationSource =
            new CancellationTokenSource();

        var httpContext =
            new DefaultHttpContext
            {
                RequestAborted =
                    cancellationSource.Token,
            };

        var observedCancellationToken =
            CancellationToken.None;

        var handler = CreateHandler(
            evaluate: _ =>
                AuthorizationDecision.Allow(),
            createSubject: _ => subject,
            createResource: _ => resource,
            observeEngineCancellation:
                cancellationToken =>
                    observedCancellationToken =
                        cancellationToken);

        await handler.HandleAsync(
            CreateContext(
                requirement,
                httpContext));

        Assert.Equal(
            cancellationSource.Token,
            observedCancellationToken);
    }

    [Fact]
    public async Task
        HandleAsync_PropagatesEngineFailure()
    {
        var subject =
            new AuthorizationSubject("user-1");

        var resource =
            new AuthorizationResource(
                "document");

        var requirement =
            new RuleGateAuthorizationRequirement(
                "read");

        var handler =
            CreateHandler(
                evaluate:
                    _ => throw new TestException(),
                createSubject: _ => subject,
                createResource: _ => resource);

        var context =
            CreateContext(
                requirement,
                resource);

        await Assert.ThrowsAsync<TestException>(
            () => handler.HandleAsync(context));

        Assert.False(context.HasSucceeded);
    }

    private static RuleGateAuthorizationHandler
        CreateHandler(
            Func<
                AuthorizationRequest,
                AuthorizationDecision> evaluate,
            Func<
                ClaimsPrincipal,
                AuthorizationSubject> createSubject,
            Func<
                object?,
                AuthorizationResource> createResource,
            IRuleGateClock? clock = null,
            Action<CancellationToken>?
                observeEngineCancellation = null)
    {
        return new RuleGateAuthorizationHandler(
            authorizationEngine:
                new StubAuthorizationEngine(
                    evaluate,
                    observeEngineCancellation),
            subjectFactory:
                new StubSubjectFactory(
                    createSubject),
            resourceFactory:
                new StubResourceFactory(
                    createResource),
            clock:
                clock
                ?? new TestRuleGateClock(
                    DateTimeOffset.UtcNow),
            requestEnricher:
                new RuleGateAuthorizationRequestEnricher(
                    subjectProviders: [],
                    resourceProviders: [],
                    contextProviders: [],
                    diagnosticsSinks: []));
    }

    private static AuthorizationHandlerContext
        CreateContext(
            RuleGateAuthorizationRequirement
                requirement,
            object? resource)
    {
        var principal =
            new ClaimsPrincipal(
                new ClaimsIdentity(
                    authenticationType: "Test"));

        return new AuthorizationHandlerContext(
            requirements:
            [
                requirement,
            ],
            user: principal,
            resource: resource);
    }

    private sealed class StubAuthorizationEngine
        : IAuthorizationEngine
    {
        private readonly Func<
            AuthorizationRequest,
            AuthorizationDecision> _evaluate;

        private readonly Action<CancellationToken>?
            _observeCancellation;

        public StubAuthorizationEngine(
            Func<
                AuthorizationRequest,
                AuthorizationDecision> evaluate,
            Action<CancellationToken>?
                observeCancellation = null)
        {
            _evaluate = evaluate;
            _observeCancellation =
                observeCancellation;
        }

        public ValueTask<AuthorizationDecision>
            EvaluateAsync(
                AuthorizationRequest request,
                CancellationToken
                    cancellationToken = default)
        {
            _observeCancellation?.Invoke(
                cancellationToken);

            return ValueTask.FromResult(
                _evaluate(request));
        }
    }

    private sealed class StubSubjectFactory
        : IRuleGateSubjectFactory
    {
        private readonly Func<
            ClaimsPrincipal,
            AuthorizationSubject> _create;

        public StubSubjectFactory(
            Func<
                ClaimsPrincipal,
                AuthorizationSubject> create)
        {
            _create = create;
        }

        public AuthorizationSubject Create(
            ClaimsPrincipal principal)
        {
            return _create(principal);
        }
    }

    private sealed class StubResourceFactory
        : IRuleGateAuthorizationResourceFactory
    {
        private readonly Func<
            object?,
            AuthorizationResource> _create;

        public StubResourceFactory(
            Func<
                object?,
                AuthorizationResource> create)
        {
            _create = create;
        }

        public AuthorizationResource Create(
            object? resource)
        {
            return _create(resource);
        }
    }

    private sealed class TestRuleGateClock
        : IRuleGateClock
    {
        private readonly DateTimeOffset _utcNow;

        public TestRuleGateClock(
            DateTimeOffset utcNow)
        {
            _utcNow = utcNow;
        }

        public DateTimeOffset GetUtcNow()
        {
            return _utcNow;
        }
    }

    private sealed class TestException
        : Exception;
}
