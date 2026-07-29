using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Fotbiler.RuleGate.Abstractions.Attributes;
using Fotbiler.RuleGate.Abstractions.Authorization;
using Fotbiler.RuleGate.AspNetCore.Authorization;
using Fotbiler.RuleGate.AspNetCore.DependencyInjection;
using Fotbiler.RuleGate.AspNetCore.Endpoints;
using Fotbiler.RuleGate.AspNetCore.Enrichment;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RuleGateAuthorizationFailure =
    Fotbiler.RuleGate.Abstractions.Authorization.AuthorizationFailure;

namespace Fotbiler.RuleGate.AspNetCore.Tests;

public sealed class RuleGateAuthorizationPipelineTests
{
    [Fact]
    public async Task
        MinimalApi_unauthenticated_request_returns_challenge()
    {
        await using var application =
            await CreateMinimalApiApplicationAsync(
                _ => AuthorizationDecision.Allow());

        using var response =
            await application.SendAsync(
                path: "/documents/document-1",
                authenticated: false);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);

        Assert.False(
            application.Recorder.WasInvoked);

        Assert.Empty(
            application.Engine.Requests);
    }

    [Fact]
    public async Task
        MinimalApi_authenticated_denied_request_returns_forbid()
    {
        await using var application =
            await CreateMinimalApiApplicationAsync(
                _ =>
                    AuthorizationDecision.Deny(
                        new RuleGateAuthorizationFailure(
                            "denied")));

        using var response =
            await application.SendAsync(
                path: "/documents/document-1",
                authenticated: true);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);

        Assert.False(
            application.Recorder.WasInvoked);

        var request =
            Assert.Single(
                application.Engine.Requests);

        Assert.Equal(
            "user-1",
            request.Subject.Id);

        Assert.Equal(
            "document",
            request.Resource.Type);

        Assert.Equal(
            "document-1",
            request.Resource.Id);

        Assert.Equal(
            "approve",
            request.Action);
    }

    [Fact]
    public async Task
        MinimalApi_authenticated_allowed_request_executes_endpoint()
    {
        await using var application =
            await CreateMinimalApiApplicationAsync(
                _ => AuthorizationDecision.Allow());

        using var response =
            await application.SendAsync(
                path: "/documents/document-1",
                authenticated: true);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        Assert.True(
            application.Recorder.WasInvoked);

        var request =
            Assert.Single(
                application.Engine.Requests);

        Assert.Equal(
            "document",
            request.Resource.Type);

        Assert.Equal(
            "document-1",
            request.Resource.Id);

        Assert.Equal(
            "approve",
            request.Action);
    }

    [Fact]
    public async Task
        MinimalApi_missing_route_resource_id_fails_closed()
    {
        await using var application =
            await CreateMinimalApiApplicationAsync(
                _ => AuthorizationDecision.Allow());

        using var response =
            await application.SendAsync(
                path: "/documents",
                authenticated: true);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);

        Assert.False(
            application.Recorder.WasInvoked);

        Assert.Empty(
            application.Engine.Requests);
    }

    [Fact]
    public async Task
        MinimalApi_enrichment_providers_populate_request()
    {
        await using var application =
            await CreateMinimalApiApplicationAsync(
                request =>
                    request.Subject.Attributes.TryGetValue(
                        "tenant",
                        out var tenant) &&
                    Equals(tenant, "tenant-1") &&
                    request.Resource.Attributes.TryGetValue(
                        "ownerId",
                        out var ownerId) &&
                    Equals(ownerId, "user-1") &&
                    request.Context.Attributes.TryGetValue(
                        "requestChannel",
                        out var requestChannel) &&
                    Equals(requestChannel, "api")
                        ? AuthorizationDecision.Allow()
                        : AuthorizationDecision.Deny(
                            new RuleGateAuthorizationFailure(
                                "missing-enrichment")),
                configureRuleGate:
                    ruleGate =>
                        ruleGate
                            .AddSubjectAttributeProvider<
                                PipelineSubjectAttributeProvider>()
                            .AddResourceAttributeProvider<
                                PipelineResourceAttributeProvider>()
                            .AddContextAttributeProvider<
                                PipelineContextAttributeProvider>());

        using var response =
            await application.SendAsync(
                path: "/documents/document-1",
                authenticated: true);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var request = Assert.Single(
            application.Engine.Requests);

        Assert.Equal(
            "tenant-1",
            request.Subject.Attributes["tenant"]);

        Assert.Equal(
            "user-1",
            request.Resource.Attributes["ownerId"]);

        Assert.Equal(
            "api",
            request.Context.Attributes[
                "requestChannel"]);
    }

    [Fact]
    public async Task
        MinimalApi_provider_exception_fails_closed()
    {
        await using var application =
            await CreateMinimalApiApplicationAsync(
                _ => AuthorizationDecision.Allow(),
                configureRuleGate:
                    ruleGate =>
                        ruleGate
                            .AddContextAttributeProvider<
                                ThrowingPipelineContextAttributeProvider>());

        using var response =
            await application.SendAsync(
                path: "/documents/document-1",
                authenticated: true);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);

        Assert.False(
            application.Recorder.WasInvoked);

        Assert.Empty(
            application.Engine.Requests);
    }

    [Fact]
    public async Task
        Controller_attribute_allowed_request_executes_action()
    {
        await using var application =
            await CreateControllerApplicationAsync(
                _ => AuthorizationDecision.Allow());

        using var response =
            await application.SendAsync(
                path:
                    "/rulegate-pipeline/documents/document-2",
                authenticated: true);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        Assert.True(
            application.Recorder.WasInvoked);

        var request =
            Assert.Single(
                application.Engine.Requests);

        Assert.Equal(
            "user-1",
            request.Subject.Id);

        Assert.Equal(
            "document",
            request.Resource.Type);

        Assert.Equal(
            "document-2",
            request.Resource.Id);

        Assert.Equal(
            "read",
            request.Action);
    }

    [Fact]
    public async Task
        Controller_attribute_denied_request_returns_forbid()
    {
        await using var application =
            await CreateControllerApplicationAsync(
                _ =>
                    AuthorizationDecision.Deny(
                        new RuleGateAuthorizationFailure(
                            "denied")));

        using var response =
            await application.SendAsync(
                path:
                    "/rulegate-pipeline/documents/document-2",
                authenticated: true);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);

        Assert.False(
            application.Recorder.WasInvoked);

        Assert.Single(
            application.Engine.Requests);
    }

    [Fact]
    public async Task
        Controller_attribute_unauthenticated_request_returns_challenge()
    {
        await using var application =
            await CreateControllerApplicationAsync(
                _ => AuthorizationDecision.Allow());

        using var response =
            await application.SendAsync(
                path:
                    "/rulegate-pipeline/documents/document-2",
                authenticated: false);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);

        Assert.False(
            application.Recorder.WasInvoked);

        Assert.Empty(
            application.Engine.Requests);
    }

    [Fact]
    public async Task
        Controller_attribute_missing_route_resource_id_fails_closed()
    {
        await using var application =
            await CreateControllerApplicationAsync(
                _ => AuthorizationDecision.Allow());

        using var response =
            await application.SendAsync(
                path:
                    "/rulegate-pipeline/documents",
                authenticated: true);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);

        Assert.False(
            application.Recorder.WasInvoked);

        Assert.Empty(
            application.Engine.Requests);
    }

    private static Task<
        RuleGatePipelineApplication>
        CreateMinimalApiApplicationAsync(
            Func<
                AuthorizationRequest,
                AuthorizationDecision> evaluate,
            Action<RuleGateBuilder>?
                configureRuleGate = null)
    {
        return CreateApplicationAsync(
            evaluate,
            addControllers: false,
            configureRuleGate,
            configureEndpoints:
                application =>
                {
                    var recorder =
                        application.Services
                            .GetRequiredService<
                                RuleGateEndpointInvocationRecorder>();

                    application
                        .MapGet(
                            "/documents/{id?}",
                            async context =>
                            {
                                recorder.WasInvoked =
                                    true;

                                await context.Response
                                    .WriteAsync(
                                        "allowed");
                            })
                        .RequireRuleGate(
                            resourceType:
                                "document",
                            action:
                                "approve",
                            resourceIdRouteValue:
                                "id");
                });
    }

    private static Task<
        RuleGatePipelineApplication>
        CreateControllerApplicationAsync(
            Func<
                AuthorizationRequest,
                AuthorizationDecision> evaluate,
            Action<RuleGateBuilder>?
                configureRuleGate = null)
    {
        return CreateApplicationAsync(
            evaluate,
            addControllers: true,
            configureRuleGate,
            configureEndpoints:
                application =>
                    application.MapControllers());
    }

    private static async Task<
        RuleGatePipelineApplication>
        CreateApplicationAsync(
            Func<
                AuthorizationRequest,
                AuthorizationDecision> evaluate,
            bool addControllers,
            Action<RuleGateBuilder>?
                configureRuleGate,
            Action<WebApplication>
                configureEndpoints)
    {
        ArgumentNullException.ThrowIfNull(
            evaluate);

        ArgumentNullException.ThrowIfNull(
            configureEndpoints);

        var builder =
            WebApplication.CreateBuilder(
                new WebApplicationOptions
                {
                    ApplicationName =
                        typeof(
                            RuleGateAuthorizationPipelineTests)
                            .Assembly
                            .FullName,
                    EnvironmentName =
                        Environments.Development,
                    ContentRootPath =
                        AppContext.BaseDirectory,
                });

        builder.Logging.ClearProviders();

        builder.WebHost.ConfigureKestrel(
            options =>
                options.Listen(
                    IPAddress.Loopback,
                    port: 0));

        builder.Services
            .AddAuthentication(
                RuleGatePipelineAuthenticationHandler
                    .SchemeName)
            .AddScheme<
                AuthenticationSchemeOptions,
                RuleGatePipelineAuthenticationHandler>(
                    RuleGatePipelineAuthenticationHandler
                        .SchemeName,
                    _ =>
                    {
                    });

        builder.Services.AddAuthorization();

        var engine =
            new RecordingRuleGateAuthorizationEngine(
                evaluate);

        builder.Services.AddSingleton<
            IAuthorizationEngine>(
                engine);

        builder.Services.AddSingleton<
            RuleGateEndpointInvocationRecorder>();

        if (addControllers)
        {
            builder.Services
                .AddControllers()
                .AddApplicationPart(
                    typeof(
                        RuleGatePipelineController)
                        .Assembly);
        }

        var ruleGate =
            builder.Services.AddRuleGate();

        configureRuleGate?.Invoke(ruleGate);

        var application =
            builder.Build();

        application.UseRouting();
        application.UseAuthentication();
        application.UseAuthorization();

        configureEndpoints(application);

        await application.StartAsync();

        var server =
            application.Services
                .GetRequiredService<IServer>();

        var addressesFeature =
            server.Features.Get<
                IServerAddressesFeature>()
            ?? throw new InvalidOperationException(
                "The test server does not expose its bound addresses.");

        var address =
            addressesFeature.Addresses.Single();

        var client =
            new HttpClient
            {
                BaseAddress =
                    new Uri(
                        address,
                        UriKind.Absolute),
            };

        return new RuleGatePipelineApplication(
            application,
            client,
            engine,
            application.Services
                .GetRequiredService<
                    RuleGateEndpointInvocationRecorder>());
    }
}

public sealed class RuleGatePipelineApplication
    : IAsyncDisposable
{
    private readonly WebApplication
        _application;

    private readonly HttpClient _client;

    public RuleGatePipelineApplication(
        WebApplication application,
        HttpClient client,
        RecordingRuleGateAuthorizationEngine
            engine,
        RuleGateEndpointInvocationRecorder
            recorder)
    {
        ArgumentNullException.ThrowIfNull(
            application);

        ArgumentNullException.ThrowIfNull(
            client);

        ArgumentNullException.ThrowIfNull(
            engine);

        ArgumentNullException.ThrowIfNull(
            recorder);

        _application = application;
        _client = client;

        Engine = engine;
        Recorder = recorder;
    }

    public RecordingRuleGateAuthorizationEngine
        Engine
    { get; }

    public RuleGateEndpointInvocationRecorder
        Recorder
    { get; }

    public Task<HttpResponseMessage>
        SendAsync(
            string path,
            bool authenticated)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            path);

        var request =
            new HttpRequestMessage(
                HttpMethod.Get,
                path);

        if (authenticated)
        {
            request.Headers.Add(
                RuleGatePipelineAuthenticationHandler
                    .UserHeader,
                "user-1");
        }

        return SendAndDisposeRequestAsync(
            request);
    }

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();

        await _application.StopAsync();
        await _application.DisposeAsync();
    }

    private async Task<HttpResponseMessage>
        SendAndDisposeRequestAsync(
            HttpRequestMessage request)
    {
        using (request)
        {
            return await _client.SendAsync(
                request);
        }
    }
}

public sealed class
    RecordingRuleGateAuthorizationEngine
    : IAuthorizationEngine
{
    private readonly Func<
        AuthorizationRequest,
        AuthorizationDecision> _evaluate;

    public RecordingRuleGateAuthorizationEngine(
        Func<
            AuthorizationRequest,
            AuthorizationDecision> evaluate)
    {
        ArgumentNullException.ThrowIfNull(
            evaluate);

        _evaluate = evaluate;
    }

    public List<AuthorizationRequest>
        Requests
    { get; } = [];

    public ValueTask<AuthorizationDecision>
        EvaluateAsync(
            AuthorizationRequest request,
            CancellationToken
                cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            request);

        Requests.Add(request);

        return ValueTask.FromResult(
            _evaluate(request));
    }
}

public sealed class
    RuleGateEndpointInvocationRecorder
{
    public bool WasInvoked { get; set; }
}

public sealed class PipelineSubjectAttributeProvider
    : IRuleGateSubjectAttributeProvider
{
    public ValueTask<RuleGateAttributeProviderResult>
        ProvideAttributesAsync(
            RuleGateAttributeProviderContext context,
            CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(
            RuleGateAttributeProviderResult.Success(
                new AuthorizationAttributes(
                [
                    new KeyValuePair<string, object?>(
                        "tenant",
                        "tenant-1"),
                ])));
    }
}

public sealed class PipelineResourceAttributeProvider
    : IRuleGateResourceAttributeProvider
{
    public ValueTask<RuleGateAttributeProviderResult>
        ProvideAttributesAsync(
            RuleGateAttributeProviderContext context,
            CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(
            RuleGateAttributeProviderResult.Success(
                new AuthorizationAttributes(
                [
                    new KeyValuePair<string, object?>(
                        "ownerId",
                        "user-1"),
                ])));
    }
}

public sealed class PipelineContextAttributeProvider
    : IRuleGateContextAttributeProvider
{
    public ValueTask<RuleGateAttributeProviderResult>
        ProvideAttributesAsync(
            RuleGateAttributeProviderContext context,
            CancellationToken cancellationToken = default)
    {
        Assert.NotNull(context.HttpContext);

        return ValueTask.FromResult(
            RuleGateAttributeProviderResult.Success(
                new AuthorizationAttributes(
                [
                    new KeyValuePair<string, object?>(
                        "requestChannel",
                        "api"),
                ])));
    }
}

public sealed class
    ThrowingPipelineContextAttributeProvider
    : IRuleGateContextAttributeProvider
{
    public ValueTask<RuleGateAttributeProviderResult>
        ProvideAttributesAsync(
            RuleGateAttributeProviderContext context,
            CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException(
            "sensitive-provider-failure");
    }
}

public sealed class
    RuleGatePipelineAuthenticationHandler
    : AuthenticationHandler<
        AuthenticationSchemeOptions>
{
    public const string SchemeName =
        "RuleGatePipelineTest";

    public const string UserHeader =
        "X-RuleGate-Test-User";

    public RuleGatePipelineAuthenticationHandler(
        IOptionsMonitor<
            AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(
            options,
            logger,
            encoder)
    {
    }

    protected override Task<AuthenticateResult>
        HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(
                UserHeader,
                out var values))
        {
            return Task.FromResult(
                AuthenticateResult.NoResult());
        }

        var userId =
            values.ToString();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Task.FromResult(
                AuthenticateResult.NoResult());
        }

        var identity =
            new ClaimsIdentity(
                claims:
                [
                    new Claim(
                        ClaimTypes.NameIdentifier,
                        userId),
                ],
                authenticationType:
                    Scheme.Name);

        var principal =
            new ClaimsPrincipal(identity);

        var ticket =
            new AuthenticationTicket(
                principal,
                Scheme.Name);

        return Task.FromResult(
            AuthenticateResult.Success(
                ticket));
    }

    protected override Task
        HandleChallengeAsync(
            AuthenticationProperties properties)
    {
        Response.StatusCode =
            StatusCodes.Status401Unauthorized;

        Response.Headers["WWW-Authenticate"] =
            $"{SchemeName} realm=\"rulegate-tests\"";

        return Task.CompletedTask;
    }

    protected override Task
        HandleForbiddenAsync(
            AuthenticationProperties properties)
    {
        Response.StatusCode =
            StatusCodes.Status403Forbidden;

        Response.Headers[
            "X-RuleGate-Test-Forbid"] =
                "true";

        return Task.CompletedTask;
    }
}

[ApiController]
[Route("rulegate-pipeline/documents")]
public sealed class RuleGatePipelineController
    : ControllerBase
{
    private readonly
        RuleGateEndpointInvocationRecorder
        _recorder;

    public RuleGatePipelineController(
        RuleGateEndpointInvocationRecorder
            recorder)
    {
        ArgumentNullException.ThrowIfNull(
            recorder);

        _recorder = recorder;
    }

    [HttpGet("{id?}")]
    [RuleGateAuthorize(
        resourceType: "document",
        action: "read",
        resourceIdRouteValue: "id")]
    public IActionResult Get(
        string? id)
    {
        _recorder.WasInvoked = true;

        return Ok(
            new
            {
                id,
            });
    }
}
