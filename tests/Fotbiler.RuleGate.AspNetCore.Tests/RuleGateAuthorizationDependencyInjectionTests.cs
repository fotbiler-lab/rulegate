using Fotbiler.RuleGate.Abstractions.Authorization;
using Fotbiler.RuleGate.AspNetCore.Authorization;
using Fotbiler.RuleGate.AspNetCore.DependencyInjection;
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
            ServiceLifetime.Singleton,
            handlerDescriptor.Lifetime);

        var timeProviderDescriptor =
            Assert.Single(
                services,
                descriptor =>
                    descriptor.ServiceType ==
                    typeof(TimeProvider));

        Assert.Equal(
            ServiceLifetime.Singleton,
            timeProviderDescriptor.Lifetime);

        Assert.Same(
            TimeProvider.System,
            timeProviderDescriptor
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

        Assert.Contains(
            serviceProvider
                .GetServices<
                    IAuthorizationHandler>(),
            handler =>
                handler is
                    RuleGateAuthorizationHandler);

        Assert.Same(
            TimeProvider.System,
            serviceProvider
                .GetRequiredService<
                    TimeProvider>());
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
                typeof(TimeProvider));

        Assert.Single(
            services,
            descriptor =>
                descriptor.ServiceType ==
                typeof(IAuthorizationHandler) &&
                descriptor.ImplementationType ==
                typeof(
                    RuleGateAuthorizationHandler));
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
    public void AddRuleGate_PreservesPreRegisteredTimeProvider()
    {
        var services =
            new ServiceCollection();

        var expected =
            new StubTimeProvider();

        services.AddSingleton<TimeProvider>(
            expected);

        services.AddRuleGate();

        using var serviceProvider =
            services.BuildServiceProvider();

        var actual =
            serviceProvider.GetRequiredService<
                TimeProvider>();

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

    private sealed class StubTimeProvider
        : TimeProvider;
}
