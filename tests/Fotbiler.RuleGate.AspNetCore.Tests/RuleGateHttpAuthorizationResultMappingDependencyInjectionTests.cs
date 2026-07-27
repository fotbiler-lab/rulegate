using Fotbiler.RuleGate.AspNetCore.DependencyInjection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Fotbiler.RuleGate.AspNetCore.Tests;

public sealed class
    RuleGateHttpAuthorizationResultMappingDependencyInjectionTests
{
    [Fact]
    public void
        AddRuleGate_does_not_enable_result_mapping_by_default()
    {
        var services = new ServiceCollection();

        services.AddRuleGate();

        Assert.DoesNotContain(
            services,
            descriptor =>
                descriptor.ServiceType ==
                typeof(
                    IAuthorizationMiddlewareResultHandler));
    }

    [Fact]
    public void
        AddHttpAuthorizationResultMapping_requires_builder()
    {
        RuleGateBuilder builder = null!;

        Assert.Throws<ArgumentNullException>(
            () =>
                builder
                    .AddHttpAuthorizationResultMapping());
    }

    [Fact]
    public void
        AddHttpAuthorizationResultMapping_is_idempotent()
    {
        var services = new ServiceCollection();

        services.AddAuthorization();

        var builder = services.AddRuleGate();

        var first =
            builder
                .AddHttpAuthorizationResultMapping();

        var second =
            builder
                .AddHttpAuthorizationResultMapping();

        Assert.Same(builder, first);
        Assert.Same(builder, second);

        var descriptor =
            Assert.Single(
                services,
                candidate =>
                    candidate.ServiceType ==
                    typeof(
                        IAuthorizationMiddlewareResultHandler));

        Assert.Equal(
            ServiceLifetime.Singleton,
            descriptor.Lifetime);

        Assert.Equal(
            "RuleGateAuthorizationMiddlewareResultHandler",
            descriptor.ImplementationType?.Name);

        using var serviceProvider =
            services.BuildServiceProvider();

        var firstHandler =
            serviceProvider.GetRequiredService<
                IAuthorizationMiddlewareResultHandler>();

        var secondHandler =
            serviceProvider.GetRequiredService<
                IAuthorizationMiddlewareResultHandler>();

        Assert.Same(firstHandler, secondHandler);

        Assert.Equal(
            "RuleGateAuthorizationMiddlewareResultHandler",
            firstHandler.GetType().Name);
    }

    [Fact]
    public void
        AddHttpAuthorizationResultMapping_preserves_custom_handler()
    {
        var services = new ServiceCollection();

        services.AddAuthorization();

        var expected =
            new StubAuthorizationMiddlewareResultHandler();

        services.AddSingleton<
            IAuthorizationMiddlewareResultHandler>(
                expected);

        services
            .AddRuleGate()
            .AddHttpAuthorizationResultMapping();

        using var serviceProvider =
            services.BuildServiceProvider();

        var actual =
            serviceProvider.GetRequiredService<
                IAuthorizationMiddlewareResultHandler>();

        Assert.Same(expected, actual);
    }

    private sealed class
        StubAuthorizationMiddlewareResultHandler
        : IAuthorizationMiddlewareResultHandler
    {
        public Task HandleAsync(
            RequestDelegate next,
            HttpContext context,
            AuthorizationPolicy policy,
            PolicyAuthorizationResult
                authorizeResult)
        {
            return Task.CompletedTask;
        }
    }
}
