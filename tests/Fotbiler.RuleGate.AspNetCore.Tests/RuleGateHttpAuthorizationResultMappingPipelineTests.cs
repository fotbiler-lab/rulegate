using System.Net;
using System.Text.Json;
using Fotbiler.RuleGate.Abstractions.Authorization;
using Fotbiler.RuleGate.AspNetCore.Authorization;
using Fotbiler.RuleGate.AspNetCore.DependencyInjection;
using Fotbiler.RuleGate.AspNetCore.Endpoints;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using RuleGateAuthorizationFailure =
    Fotbiler.RuleGate.Abstractions.Authorization.AuthorizationFailure;

namespace Fotbiler.RuleGate.AspNetCore.Tests;

public sealed class
    RuleGateHttpAuthorizationResultMappingPipelineTests
{
    [Fact]
    public async Task
        Unauthenticated_RuleGate_request_returns_problem_details()
    {
        await using var application =
            await CreateApplicationAsync(
                _ => AuthorizationDecision.Allow());

        using var response =
            await application.SendAsync(
                "/rulegate/document-1",
                authenticated: false);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);

        Assert.Equal(
            $"{RuleGatePipelineAuthenticationHandler.SchemeName} realm=\"rulegate-tests\"",
            response.Headers
                .WwwAuthenticate
                .ToString());

        await AssertProblemAsync(
            response,
            expectedStatus: 401,
            expectedType:
                RuleGateHttpAuthorizationProblemTypes
                    .AuthenticationRequired,
            expectedCode:
                RuleGateHttpAuthorizationProblemCodes
                    .AuthenticationRequired);

        Assert.Empty(application.Engine.Requests);
        Assert.False(application.Recorder.WasInvoked);
    }

    [Fact]
    public async Task
        Denied_RuleGate_request_returns_generic_problem_details()
    {
        await using var application =
            await CreateApplicationAsync(
                _ =>
                    AuthorizationDecision.Deny(
                        new RuleGateAuthorizationFailure(
                            "SENSITIVE_POLICY_FAILURE",
                            "sensitive-requirement")));

        using var response =
            await application.SendAsync(
                "/rulegate/document-1",
                authenticated: true);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);

        Assert.Equal(
            "true",
            response.Headers
                .GetValues(
                    "X-RuleGate-Test-Forbid")
                .Single());

        var body =
            await AssertProblemAsync(
                response,
                expectedStatus: 403,
                expectedType:
                    RuleGateHttpAuthorizationProblemTypes
                        .AccessForbidden,
                expectedCode:
                    RuleGateHttpAuthorizationProblemCodes
                        .AccessForbidden);

        Assert.DoesNotContain(
            "SENSITIVE_POLICY_FAILURE",
            body,
            StringComparison.Ordinal);

        Assert.DoesNotContain(
            "sensitive-requirement",
            body,
            StringComparison.Ordinal);

        Assert.DoesNotContain(
            "document-1",
            body,
            StringComparison.Ordinal);

        Assert.DoesNotContain(
            "user-1",
            body,
            StringComparison.Ordinal);

        Assert.Single(application.Engine.Requests);
        Assert.False(application.Recorder.WasInvoked);
    }

    [Fact]
    public async Task
        Allowed_RuleGate_request_executes_endpoint()
    {
        await using var application =
            await CreateApplicationAsync(
                _ => AuthorizationDecision.Allow());

        using var response =
            await application.SendAsync(
                "/rulegate/document-1",
                authenticated: true);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        Assert.Equal(
            "allowed",
            await response.Content.ReadAsStringAsync());

        Assert.True(application.Recorder.WasInvoked);
        Assert.Single(application.Engine.Requests);
    }

    [Fact]
    public async Task
        Controller_RuleGate_denied_request_returns_problem_details()
    {
        await using var application =
            await CreateApplicationAsync(
                _ =>
                    AuthorizationDecision.Deny(
                        new RuleGateAuthorizationFailure(
                            "CONTROLLER_SENSITIVE_FAILURE",
                            "controller-requirement")));

        using var response =
            await application.SendAsync(
                "/rulegate-pipeline/documents/document-2",
                authenticated: true);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);

        Assert.Equal(
            "true",
            response.Headers
                .GetValues(
                    "X-RuleGate-Test-Forbid")
                .Single());

        var body =
            await AssertProblemAsync(
                response,
                expectedStatus: 403,
                expectedType:
                    RuleGateHttpAuthorizationProblemTypes
                        .AccessForbidden,
                expectedCode:
                    RuleGateHttpAuthorizationProblemCodes
                        .AccessForbidden);

        Assert.DoesNotContain(
            "CONTROLLER_SENSITIVE_FAILURE",
            body,
            StringComparison.Ordinal);

        Assert.DoesNotContain(
            "controller-requirement",
            body,
            StringComparison.Ordinal);

        Assert.Single(application.Engine.Requests);
        Assert.False(application.Recorder.WasInvoked);
    }

    [Fact]
    public async Task
        Missing_RuleGate_route_value_returns_forbidden_problem()
    {
        await using var application =
            await CreateApplicationAsync(
                _ => AuthorizationDecision.Allow());

        using var response =
            await application.SendAsync(
                "/rulegate",
                authenticated: true);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);

        await AssertProblemAsync(
            response,
            expectedStatus: 403,
            expectedType:
                RuleGateHttpAuthorizationProblemTypes
                    .AccessForbidden,
            expectedCode:
                RuleGateHttpAuthorizationProblemCodes
                    .AccessForbidden);

        Assert.Empty(application.Engine.Requests);
        Assert.False(application.Recorder.WasInvoked);
    }

    [Fact]
    public async Task
        Standard_policy_uses_default_result_handling()
    {
        await using var application =
            await CreateApplicationAsync(
                _ => AuthorizationDecision.Allow());

        using var response =
            await application.SendAsync(
                "/standard",
                authenticated: false);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);

        Assert.Null(
            response.Content.Headers.ContentType);

        Assert.Equal(
            string.Empty,
            await response.Content.ReadAsStringAsync());

        Assert.Empty(application.Engine.Requests);
        Assert.False(application.Recorder.WasInvoked);
    }

    [Fact]
    public async Task
        Standard_forbidden_policy_uses_default_result_handling()
    {
        await using var application =
            await CreateApplicationAsync(
                _ => AuthorizationDecision.Allow());

        using var response =
            await application.SendAsync(
                "/standard-denied",
                authenticated: true);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);

        Assert.Equal(
            "true",
            response.Headers
                .GetValues(
                    "X-RuleGate-Test-Forbid")
                .Single());

        Assert.Null(
            response.Content.Headers.ContentType);

        Assert.Equal(
            string.Empty,
            await response.Content.ReadAsStringAsync());

        Assert.Empty(application.Engine.Requests);
        Assert.False(application.Recorder.WasInvoked);
    }

    private static async Task<string>
        AssertProblemAsync(
            HttpResponseMessage response,
            int expectedStatus,
            string expectedType,
            string expectedCode)
    {
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType
                ?.MediaType);

        var body =
            await response.Content.ReadAsStringAsync();

        using var document =
            JsonDocument.Parse(body);

        var root = document.RootElement;

        Assert.Equal(
            expectedType,
            root.GetProperty("type").GetString());

        Assert.Equal(
            expectedStatus,
            root.GetProperty("status").GetInt32());

        Assert.Equal(
            expectedCode,
            root.GetProperty("code").GetString());

        Assert.False(
            string.IsNullOrWhiteSpace(
                root.GetProperty("traceId")
                    .GetString()));

        Assert.False(
            root.TryGetProperty(
                "instance",
                out _));

        return body;
    }

    private static async Task<
        RuleGatePipelineApplication>
        CreateApplicationAsync(
            Func<
                AuthorizationRequest,
                AuthorizationDecision> evaluate)
    {
        var builder =
            WebApplication.CreateBuilder(
                new WebApplicationOptions
                {
                    ApplicationName =
                        typeof(
                            RuleGateHttpAuthorizationResultMappingPipelineTests)
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

        builder.Services.AddAuthorization(
            options =>
            {
                options.AddPolicy(
                    "standard-authenticated",
                    policy =>
                        policy
                            .RequireAuthenticatedUser());

                options.AddPolicy(
                    "standard-denied",
                    policy =>
                        policy.RequireClaim(
                            "standard-access",
                            "granted"));
            });

        builder.Services
            .AddControllers()
            .AddApplicationPart(
                typeof(
                    RuleGatePipelineController)
                    .Assembly);

        var engine =
            new RecordingRuleGateAuthorizationEngine(
                evaluate);

        builder.Services.AddSingleton<
            IAuthorizationEngine>(engine);

        builder.Services.AddSingleton<
            RuleGateEndpointInvocationRecorder>();

        builder.Services
            .AddRuleGate()
            .AddHttpAuthorizationResultMapping();

        var application = builder.Build();

        application.UseRouting();
        application.UseAuthentication();
        application.UseAuthorization();

        var recorder =
            application.Services.GetRequiredService<
                RuleGateEndpointInvocationRecorder>();

        application
            .MapGet(
                "/rulegate/{id?}",
                async context =>
                {
                    recorder.WasInvoked = true;

                    await context.Response.WriteAsync(
                        "allowed");
                })
            .RequireRuleGate(
                resourceType: "document",
                action: "read",
                resourceIdRouteValue: "id");

        application
            .MapGet(
                "/standard",
                async context =>
                {
                    recorder.WasInvoked = true;

                    await context.Response.WriteAsync(
                        "standard");
                })
            .RequireAuthorization(
                "standard-authenticated");

        application
            .MapGet(
                "/standard-denied",
                async context =>
                {
                    recorder.WasInvoked = true;

                    await context.Response.WriteAsync(
                        "standard-denied");
                })
            .RequireAuthorization(
                "standard-denied");

        application.MapControllers();

        await application.StartAsync();

        var server =
            application.Services.GetRequiredService<
                IServer>();

        var addressesFeature =
            server.Features.Get<
                IServerAddressesFeature>()
            ?? throw new InvalidOperationException(
                "The test server does not expose its bound addresses.");

        var client =
            new HttpClient
            {
                BaseAddress =
                    new Uri(
                        addressesFeature
                            .Addresses
                            .Single(),
                        UriKind.Absolute),
            };

        return new RuleGatePipelineApplication(
            application,
            client,
            engine,
            recorder);
    }
}
