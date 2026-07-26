using Fotbiler.RuleGate.Abstractions.Authorization;
using Fotbiler.RuleGate.Abstractions.Evaluation;
using Fotbiler.RuleGate.Abstractions.Policies;
using Fotbiler.RuleGate.AspNetCore.DependencyInjection;
using Fotbiler.RuleGate.Core.Engine;
using Fotbiler.RuleGate.Core.Evaluation;
using Fotbiler.RuleGate.Core.Evaluation.Evaluators;
using Fotbiler.RuleGate.Core.Policies;
using Microsoft.Extensions.DependencyInjection;

namespace Fotbiler.RuleGate.AspNetCore.Tests;

public sealed class RuleGateDependencyInjectionTests
{
    [Fact]
    public void AddRuleGate_RequiresServiceCollection()
    {
        IServiceCollection services = null!;

        Assert.Throws<ArgumentNullException>(
            () => services.AddRuleGate());
    }

    [Fact]
    public void AddRuleGate_ReturnsBuilderForOriginalCollection()
    {
        var services = new ServiceCollection();

        var builder = services.AddRuleGate();

        Assert.Same(services, builder.Services);
    }

    [Fact]
    public void AddRuleGate_RegistersDefaultSingletonServices()
    {
        var services = new ServiceCollection();

        services.AddRuleGate();

        AssertSingleton<
            IAuthorizationEngine,
            PolicyAuthorizationEngine>(services);

        AssertSingleton<
            IPolicyProvider,
            InMemoryPolicyProvider>(services);

        AssertSingleton<
            IRequirementEvaluationDispatcher,
            RequirementEvaluationDispatcher>(services);

        var evaluatorDescriptors = services
            .Where(
                descriptor =>
                    descriptor.ServiceType ==
                    typeof(IRequirementEvaluator))
            .ToArray();

        Assert.Equal(5, evaluatorDescriptors.Length);

        Assert.All(
            evaluatorDescriptors,
            descriptor => Assert.Equal(
                ServiceLifetime.Singleton,
                descriptor.Lifetime));

        var evaluatorTypes = evaluatorDescriptors
            .Select(
                descriptor =>
                    descriptor.ImplementationType)
            .ToHashSet();

        Assert.Contains(
            typeof(PermissionRequirementEvaluator),
            evaluatorTypes);

        Assert.Contains(
            typeof(RoleRequirementEvaluator),
            evaluatorTypes);

        Assert.Contains(
            typeof(AllRequirementEvaluator),
            evaluatorTypes);

        Assert.Contains(
            typeof(AnyRequirementEvaluator),
            evaluatorTypes);

        Assert.Contains(
            typeof(NotRequirementEvaluator),
            evaluatorTypes);
    }

    [Fact]
    public void AddRuleGate_IsIdempotentForDefaultServices()
    {
        var services = new ServiceCollection();

        services.AddRuleGate();
        services.AddRuleGate();

        Assert.Single(services, descriptor =>
                    descriptor.ServiceType ==
                    typeof(IAuthorizationEngine));

        Assert.Single(services, descriptor =>
                    descriptor.ServiceType ==
                    typeof(IPolicyProvider));

        Assert.Single(services, descriptor =>
                    descriptor.ServiceType ==
                    typeof(
                        IRequirementEvaluationDispatcher));

        Assert.Equal(
            5,
            services.Count(
                descriptor =>
                    descriptor.ServiceType ==
                    typeof(IRequirementEvaluator)));
    }

    [Fact]
    public void AddRuleGate_PreservesPreRegisteredPolicyProvider()
    {
        var services = new ServiceCollection();
        var expectedProvider = new StubPolicyProvider();

        services.AddSingleton<IPolicyProvider>(
            expectedProvider);

        services.AddRuleGate();

        using var serviceProvider =
            services.BuildServiceProvider(
                new ServiceProviderOptions
                {
                    ValidateOnBuild = true,
                    ValidateScopes = true,
                });

        var actualProvider =
            serviceProvider.GetRequiredService<
                IPolicyProvider>();

        Assert.Same(
            expectedProvider,
            actualProvider);
    }

    [Fact]
    public void AddRuleGate_ResolvesCompleteDefaultGraph()
    {
        var services = new ServiceCollection();

        services.AddRuleGate();

        using var serviceProvider =
            services.BuildServiceProvider(
                new ServiceProviderOptions
                {
                    ValidateOnBuild = true,
                    ValidateScopes = true,
                });

        var firstEngine =
            serviceProvider.GetRequiredService<
                IAuthorizationEngine>();

        var secondEngine =
            serviceProvider.GetRequiredService<
                IAuthorizationEngine>();

        var policyProvider =
            serviceProvider.GetRequiredService<
                IPolicyProvider>();

        var dispatcher =
            serviceProvider.GetRequiredService<
                IRequirementEvaluationDispatcher>();

        var evaluators =
            serviceProvider
                .GetServices<IRequirementEvaluator>()
                .ToArray();

        Assert.IsType<PolicyAuthorizationEngine>(
            firstEngine);

        Assert.Same(firstEngine, secondEngine);

        Assert.IsType<InMemoryPolicyProvider>(
            policyProvider);

        Assert.IsType<RequirementEvaluationDispatcher>(
            dispatcher);

        Assert.Equal(5, evaluators.Length);
    }

    [Fact]
    public async Task DefaultPolicyProvider_StartsEmpty()
    {
        var services = new ServiceCollection();

        services.AddRuleGate();

        using var serviceProvider =
            services.BuildServiceProvider();

        var provider =
            serviceProvider.GetRequiredService<
                IPolicyProvider>();

        var policy = await provider.FindAsync(
            "sample-resource",
            "read");

        Assert.Null(policy);
    }

    [Fact]
    public void AddPolicy_RequiresPolicy()
    {
        var services = new ServiceCollection();
        var builder = services.AddRuleGate();

        Assert.Throws<ArgumentNullException>(
            () => builder.AddPolicy(null!));
    }

    [Fact]
    public void AddPolicies_RequiresPolicyCollection()
    {
        var services = new ServiceCollection();
        var builder = services.AddRuleGate();

        Assert.Throws<ArgumentNullException>(
            () => builder.AddPolicies(null!));
    }

    [Fact]
    public void AddPolicies_AcceptsEmptyCollection()
    {
        var services = new ServiceCollection();
        var builder = services.AddRuleGate();

        var result = builder.AddPolicies(
            Array.Empty<PolicyDefinition>());

        Assert.Same(builder, result);

        Assert.DoesNotContain(
            services,
            descriptor =>
                descriptor.ServiceType ==
                typeof(PolicyDefinition));
    }

    [Fact]
    public void AddRequirementEvaluator_IsIdempotentByImplementation()
    {
        var services = new ServiceCollection();
        var builder = services.AddRuleGate();

        builder
            .AddRequirementEvaluator<
                CustomRequirementEvaluator>()
            .AddRequirementEvaluator<
                CustomRequirementEvaluator>();

        var descriptors = services
            .Where(
                descriptor =>
                    descriptor.ServiceType ==
                    typeof(IRequirementEvaluator) &&
                    descriptor.ImplementationType ==
                    typeof(CustomRequirementEvaluator))
            .ToArray();

        Assert.Single(descriptors);

        Assert.Equal(
            ServiceLifetime.Singleton,
            descriptors[0].Lifetime);
    }

    private static void AssertSingleton<
        TService,
        TImplementation>(
        IServiceCollection services)
    {
        var descriptor = Assert.Single(services, candidate =>
                    candidate.ServiceType ==
                    typeof(TService));

        Assert.Equal(
            ServiceLifetime.Singleton,
            descriptor.Lifetime);

        Assert.Equal(
            typeof(TImplementation),
            descriptor.ImplementationType);
    }

    private sealed class StubPolicyProvider
        : IPolicyProvider
    {
        public ValueTask<PolicyDefinition?> FindAsync(
            string resourceType,
            string action,
            CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult<
                PolicyDefinition?>(null);
        }
    }

    private sealed class CustomRequirementEvaluator
        : IRequirementEvaluator
    {
        public Type RequirementType =>
            typeof(PermissionRequirementDefinition);

        public ValueTask<RequirementEvaluationResult>
            EvaluateAsync(
                RequirementDefinition requirement,
                RequirementEvaluationContext context,
                IRequirementEvaluationDispatcher dispatcher,
                CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(
                RequirementEvaluationResult.Satisfied());
        }
    }
}
