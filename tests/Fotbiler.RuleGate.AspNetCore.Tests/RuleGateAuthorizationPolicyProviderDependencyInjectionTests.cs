using Fotbiler.RuleGate.AspNetCore.Authorization;
using Fotbiler.RuleGate.AspNetCore.DependencyInjection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Fotbiler.RuleGate.AspNetCore.Tests;

public sealed class
    RuleGateAuthorizationPolicyProviderDependencyInjectionTests
{
    [Fact]
    public void AddRuleGate_replaces_default_policy_provider()
    {
        var services =
            new ServiceCollection();

        services.AddLogging();

        services.AddAuthorizationCore();
        services.AddRuleGate();

        using var serviceProvider =
            services.BuildServiceProvider(
                new ServiceProviderOptions
                {
                    ValidateOnBuild = true,
                    ValidateScopes = true,
                });

        var provider =
            serviceProvider.GetRequiredService<
                IAuthorizationPolicyProvider>();

        Assert.IsType<
            RuleGateAuthorizationPolicyProvider>(
                provider);
    }

    [Fact]
    public void Registration_order_does_not_replace_rulegate_provider()
    {
        var services =
            new ServiceCollection();

        services.AddLogging();

        services.AddRuleGate();
        services.AddAuthorizationCore();

        using var serviceProvider =
            services.BuildServiceProvider(
                new ServiceProviderOptions
                {
                    ValidateOnBuild = true,
                    ValidateScopes = true,
                });

        var provider =
            serviceProvider.GetRequiredService<
                IAuthorizationPolicyProvider>();

        Assert.IsType<
            RuleGateAuthorizationPolicyProvider>(
                provider);
    }

    [Fact]
    public void AddRuleGate_registers_one_policy_provider()
    {
        var services =
            new ServiceCollection();

        services.AddLogging();

        services.AddAuthorizationCore();
        services.AddRuleGate();
        services.AddRuleGate();

        var descriptors =
            services
                .Where(
                    descriptor =>
                        descriptor.ServiceType
                        == typeof(
                            IAuthorizationPolicyProvider))
                .ToArray();

        Assert.Single(descriptors);

        Assert.Equal(
            typeof(
                RuleGateAuthorizationPolicyProvider),
            descriptors[0].ImplementationType);

        Assert.Equal(
            ServiceLifetime.Singleton,
            descriptors[0].Lifetime);
    }
}
