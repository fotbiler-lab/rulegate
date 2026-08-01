using Fotbiler.RuleGate.Abstractions.Authorization;
using Fotbiler.RuleGate.AspNetCore.Authorization;
using Fotbiler.RuleGate.AspNetCore.DependencyInjection;
using Fotbiler.RuleGate.AspNetCore.Enrichment;
using Fotbiler.RuleGate.AspNetCore.Time;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Fotbiler.RuleGate.AspNetCore.Tests;

public sealed class
    RuleGateAuthorizationDependencyInjectionTests
{
    [Fact]
    public void AddRuleGate_RegistersAuthorizationServices()
    {
        var services =
            new ServiceCollection();

        services.AddRuleGate();

        var resourceFactoryDescriptor =
            Assert.Single(
                services,
                descriptor =>
                    descriptor.ServiceType ==
                    typeof(
                        IRuleGateAuthorizationResourceFactory));

        Assert.Equal(
            ServiceLifetime.Singleton,
            resourceFactoryDescriptor.Lifetime);

        Assert.Equal(
            typeof(
                RuleGateAuthorizationResourceFactory),
            resourceFactoryDescriptor
                .ImplementationType);

        var handlerDescriptor =
            Assert.Single(
                services,
                descriptor =>
                    descriptor.ServiceType ==
                    typeof(IAuthorizationHandler) &&
                    descriptor.ImplementationType ==
                    typeof(
                        RuleGateAuthorizationHandler));

        Assert.Equal(
            ServiceLifetime.Scoped,
            handlerDescriptor.Lifetime);

        var enricherDescriptor =
            Assert.Single(
                services,
                descriptor =>
                    descriptor.ServiceType ==
                    typeof(
                        IRuleGateAuthorizationRequestEnricher));

        Assert.Equal(
            ServiceLifetime.Scoped,
            enricherDescriptor.Lifetime);

        Assert.Equal(
            typeof(
                RuleGateAuthorizationRequestEnricher),
            enricherDescriptor.ImplementationType);

        var clockDescriptor =
            Assert.Single(
                services,
                descriptor =>
                    descriptor.ServiceType ==
                    typeof(IRuleGateClock));

        Assert.Equal(
            ServiceLifetime.Singleton,
            clockDescriptor.Lifetime);

        var defaultClock =
            Assert.IsAssignableFrom<
                IRuleGateClock>(
                    clockDescriptor
                        .ImplementationInstance);

        using var serviceProvider =
            services.BuildServiceProvider(
                new ServiceProviderOptions
                {
                    ValidateOnBuild = true,
                    ValidateScopes = true,
                });

        Assert.IsType<
            RuleGateAuthorizationResourceFactory>(
                serviceProvider
                    .GetRequiredService<
                        IRuleGateAuthorizationResourceFactory>());

        using var scope =
            serviceProvider.CreateScope();

        Assert.Contains(
            scope.ServiceProvider
                .GetServices<IAuthorizationHandler>(),
            handler =>
                handler is
                    RuleGateAuthorizationHandler);

        Assert.IsType<
            RuleGateAuthorizationRequestEnricher>(
                scope.ServiceProvider
                    .GetRequiredService<
                        IRuleGateAuthorizationRequestEnricher>());

        Assert.Same(
            defaultClock,
            serviceProvider
                .GetRequiredService<
                    IRuleGateClock>());
    }

    [Fact]
    public void AddRuleGate_IsIdempotentForAuthorizationServices()
    {
        var services =
            new ServiceCollection();

        services.AddRuleGate();
        services.AddRuleGate();

        Assert.Single(
            services,
            descriptor =>
                descriptor.ServiceType ==
                typeof(
                    IRuleGateAuthorizationResourceFactory));

        Assert.Single(
            services,
            descriptor =>
                descriptor.ServiceType ==
                typeof(IRuleGateClock));

        Assert.Single(
            services,
            descriptor =>
                descriptor.ServiceType ==
                typeof(IAuthorizationHandler) &&
                descriptor.ImplementationType ==
                typeof(
                    RuleGateAuthorizationHandler));

        Assert.Single(
            services,
            descriptor =>
                descriptor.ServiceType ==
                typeof(
                    IRuleGateAuthorizationRequestEnricher));
    }

    [Fact]
    public void AddRuleGate_PreservesPreRegisteredResourceFactory()
    {
        var services =
            new ServiceCollection();

        var expected =
            new StubResourceFactory();

        services.AddSingleton<
            IRuleGateAuthorizationResourceFactory>(
                expected);

        services.AddRuleGate();

        using var serviceProvider =
            services.BuildServiceProvider();

        var actual =
            serviceProvider.GetRequiredService<
                IRuleGateAuthorizationResourceFactory>();

        Assert.Same(expected, actual);
    }

    [Fact]
    public void AddRuleGate_PreservesPreRegisteredClock()
    {
        var services =
            new ServiceCollection();

        var expected =
            new StubRuleGateClock();

        services.AddSingleton<
            IRuleGateClock>(
                expected);

        services.AddRuleGate();

        using var serviceProvider =
            services.BuildServiceProvider();

        var actual =
            serviceProvider.GetRequiredService<
                IRuleGateClock>();

        Assert.Same(expected, actual);
    }

    private sealed class StubResourceFactory
        : IRuleGateAuthorizationResourceFactory
    {
        public AuthorizationResource Create(
            object? resource)
        {
            return new AuthorizationResource(
                "stub-resource");
        }
    }

    private sealed class StubRuleGateClock
        : IRuleGateClock
    {
        public DateTimeOffset GetUtcNow()
        {
            return DateTimeOffset.UnixEpoch;
        }
    }
}
