using System.Security.Claims;
using Fotbiler.RuleGate.Abstractions.Authorization;
using Fotbiler.RuleGate.AspNetCore.DependencyInjection;
using Fotbiler.RuleGate.AspNetCore.Enrichment;
using Microsoft.Extensions.DependencyInjection;

namespace Fotbiler.RuleGate.AspNetCore.Tests;

public sealed class
    RuleGateEnrichmentDependencyInjectionTests
{
    [Fact]
    public void Provider_registration_requires_builder()
    {
        RuleGateBuilder builder = null!;

        Assert.Throws<ArgumentNullException>(
            () => builder
                .AddSubjectAttributeProvider<
                    SubjectProvider>());

        Assert.Throws<ArgumentNullException>(
            () => builder
                .AddResourceAttributeProvider<
                    ResourceProvider>());

        Assert.Throws<ArgumentNullException>(
            () => builder
                .AddContextAttributeProvider<
                    ContextProvider>());
    }

    [Fact]
    public void Provider_registration_is_scoped_by_default()
    {
        var services = new ServiceCollection();

        services
            .AddRuleGate()
            .AddSubjectAttributeProvider<
                SubjectProvider>()
            .AddResourceAttributeProvider<
                ResourceProvider>()
            .AddContextAttributeProvider<
                ContextProvider>();

        AssertDescriptor<
            IRuleGateSubjectAttributeProvider,
            SubjectProvider>(
                services,
                ServiceLifetime.Scoped);

        AssertDescriptor<
            IRuleGateResourceAttributeProvider,
            ResourceProvider>(
                services,
                ServiceLifetime.Scoped);

        AssertDescriptor<
            IRuleGateContextAttributeProvider,
            ContextProvider>(
                services,
                ServiceLifetime.Scoped);
    }

    [Fact]
    public void Provider_registration_supports_explicit_lifetime()
    {
        var services = new ServiceCollection();

        services
            .AddRuleGate()
            .AddSubjectAttributeProvider<
                SubjectProvider>(
                    ServiceLifetime.Singleton)
            .AddResourceAttributeProvider<
                ResourceProvider>(
                    ServiceLifetime.Transient);

        AssertDescriptor<
            IRuleGateSubjectAttributeProvider,
            SubjectProvider>(
                services,
                ServiceLifetime.Singleton);

        AssertDescriptor<
            IRuleGateResourceAttributeProvider,
            ResourceProvider>(
                services,
                ServiceLifetime.Transient);
    }

    [Fact]
    public void Provider_registration_rejects_invalid_lifetime()
    {
        var builder =
            new ServiceCollection().AddRuleGate();

        Assert.Throws<ArgumentOutOfRangeException>(
            () => builder
                .AddSubjectAttributeProvider<
                    SubjectProvider>(
                        (ServiceLifetime)999));
    }

    [Fact]
    public void Provider_registration_is_idempotent()
    {
        var services = new ServiceCollection();
        var builder = services.AddRuleGate();

        builder
            .AddSubjectAttributeProvider<
                SubjectProvider>()
            .AddSubjectAttributeProvider<
                SubjectProvider>();

        Assert.Single(
            services,
            descriptor =>
                descriptor.ServiceType ==
                    typeof(
                        IRuleGateSubjectAttributeProvider) &&
                descriptor.ImplementationType ==
                    typeof(SubjectProvider));
    }

    [Fact]
    public void Scoped_provider_can_use_scoped_dependencies()
    {
        var services = new ServiceCollection();

        services.AddScoped<ScopedDependency>();

        services
            .AddRuleGate()
            .AddSubjectAttributeProvider<
                SubjectProvider>();

        using var serviceProvider =
            services.BuildServiceProvider(
                new ServiceProviderOptions
                {
                    ValidateOnBuild = true,
                    ValidateScopes = true,
                });

        using var firstScope =
            serviceProvider.CreateScope();

        using var secondScope =
            serviceProvider.CreateScope();

        var first = Assert.IsType<SubjectProvider>(
            firstScope.ServiceProvider
                .GetRequiredService<
                    IRuleGateSubjectAttributeProvider>());

        var firstAgain = Assert.IsType<SubjectProvider>(
            firstScope.ServiceProvider
                .GetRequiredService<
                    IRuleGateSubjectAttributeProvider>());

        var second = Assert.IsType<SubjectProvider>(
            secondScope.ServiceProvider
                .GetRequiredService<
                    IRuleGateSubjectAttributeProvider>());

        Assert.Same(first, firstAgain);
        Assert.NotSame(first, second);
        Assert.NotSame(
            first.Dependency,
            second.Dependency);
    }

    [Fact]
    public void AddRuleGate_preserves_custom_enricher()
    {
        var services = new ServiceCollection();
        var expected = new StubRequestEnricher();

        services.AddSingleton<
            IRuleGateAuthorizationRequestEnricher>(
                expected);

        services.AddRuleGate();

        using var serviceProvider =
            services.BuildServiceProvider();

        Assert.Same(
            expected,
            serviceProvider.GetRequiredService<
                IRuleGateAuthorizationRequestEnricher>());
    }

    [Fact]
    public void Startup_style_configuration_resolves_pipeline()
    {
        var services = new ServiceCollection();

        new StartupStyleConfiguration()
            .ConfigureServices(services);

        using var serviceProvider =
            services.BuildServiceProvider(
                new ServiceProviderOptions
                {
                    ValidateOnBuild = true,
                    ValidateScopes = true,
                });

        using var scope =
            serviceProvider.CreateScope();

        Assert.IsType<
            RuleGateAuthorizationRequestEnricher>(
                scope.ServiceProvider
                    .GetRequiredService<
                        IRuleGateAuthorizationRequestEnricher>());

        Assert.IsType<SubjectProvider>(
            scope.ServiceProvider
                .GetRequiredService<
                    IRuleGateSubjectAttributeProvider>());
    }

    private static void
        AssertDescriptor<TService, TImplementation>(
            IServiceCollection services,
            ServiceLifetime lifetime)
    {
        var descriptor = Assert.Single(
            services,
            candidate =>
                candidate.ServiceType ==
                    typeof(TService) &&
                candidate.ImplementationType ==
                    typeof(TImplementation));

        Assert.Equal(
            lifetime,
            descriptor.Lifetime);
    }

    private sealed class StartupStyleConfiguration
    {
        public void ConfigureServices(
            IServiceCollection services)
        {
            services.AddScoped<ScopedDependency>();

            services
                .AddRuleGate()
                .AddSubjectAttributeProvider<
                    SubjectProvider>();
        }
    }

    private sealed class ScopedDependency;

    private sealed class SubjectProvider
        : IRuleGateSubjectAttributeProvider
    {
        public SubjectProvider(
            ScopedDependency dependency)
        {
            Dependency = dependency;
        }

        public ScopedDependency Dependency { get; }

        public ValueTask<RuleGateAttributeProviderResult>
            ProvideAttributesAsync(
                RuleGateAttributeProviderContext context,
                CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(
                RuleGateAttributeProviderResult.Success());
        }
    }

    private sealed class ResourceProvider
        : IRuleGateResourceAttributeProvider
    {
        public ValueTask<RuleGateAttributeProviderResult>
            ProvideAttributesAsync(
                RuleGateAttributeProviderContext context,
                CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(
                RuleGateAttributeProviderResult.Success());
        }
    }

    private sealed class ContextProvider
        : IRuleGateContextAttributeProvider
    {
        public ValueTask<RuleGateAttributeProviderResult>
            ProvideAttributesAsync(
                RuleGateAttributeProviderContext context,
                CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(
                RuleGateAttributeProviderResult.Success());
        }
    }

    private sealed class StubRequestEnricher
        : IRuleGateAuthorizationRequestEnricher
    {
        public ValueTask<
            RuleGateAuthorizationRequestEnrichmentResult>
            EnrichAsync(
                AuthorizationRequest request,
                ClaimsPrincipal principal,
                object? frameworkResource,
                CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(
                RuleGateAuthorizationRequestEnrichmentResult
                    .Success(request));
        }
    }
}
