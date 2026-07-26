using System.Security.Claims;
using Fotbiler.RuleGate.Abstractions.Authorization;
using Fotbiler.RuleGate.AspNetCore.DependencyInjection;
using Fotbiler.RuleGate.AspNetCore.Subjects;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Fotbiler.RuleGate.AspNetCore.Tests;

public sealed class
    RuleGateSubjectDependencyInjectionTests
{
    [Fact]
    public void AddRuleGate_RegistersSingletonSubjectFactory()
    {
        var services = new ServiceCollection();

        services.AddRuleGate();

        var descriptor = Assert.Single(
            services,
            candidate =>
                candidate.ServiceType ==
                typeof(IRuleGateSubjectFactory));

        Assert.Equal(
            ServiceLifetime.Singleton,
            descriptor.Lifetime);

        Assert.Equal(
            typeof(
                ClaimsPrincipalRuleGateSubjectFactory),
            descriptor.ImplementationType);

        using var serviceProvider =
            services.BuildServiceProvider(
                new ServiceProviderOptions
                {
                    ValidateOnBuild = true,
                    ValidateScopes = true,
                });

        var first =
            serviceProvider.GetRequiredService<
                IRuleGateSubjectFactory>();

        var second =
            serviceProvider.GetRequiredService<
                IRuleGateSubjectFactory>();

        Assert.IsType<
            ClaimsPrincipalRuleGateSubjectFactory>(
                first);

        Assert.Same(first, second);
    }

    [Fact]
    public void AddRuleGate_IsIdempotentForSubjectFactory()
    {
        var services = new ServiceCollection();

        services.AddRuleGate();
        services.AddRuleGate();

        Assert.Single(
            services,
            descriptor =>
                descriptor.ServiceType ==
                typeof(IRuleGateSubjectFactory));
    }

    [Fact]
    public void AddRuleGate_PreservesPreRegisteredSubjectFactory()
    {
        var services = new ServiceCollection();

        var expectedFactory =
            new StubSubjectFactory();

        services.AddSingleton<
            IRuleGateSubjectFactory>(
                expectedFactory);

        services.AddRuleGate();

        using var serviceProvider =
            services.BuildServiceProvider();

        var actualFactory =
            serviceProvider.GetRequiredService<
                IRuleGateSubjectFactory>();

        Assert.Same(
            expectedFactory,
            actualFactory);
    }

    [Fact]
    public void ConfigureSubjectMapping_ConfiguresOptions()
    {
        var services = new ServiceCollection();

        services
            .AddRuleGate()
            .ConfigureSubjectMapping(
                options =>
                {
                    options.SubjectIdClaimType =
                        "custom-subject";

                    options.RoleClaimTypes.Clear();
                    options.RoleClaimTypes.Add(
                        "custom-role");

                    options.PermissionClaimTypes.Clear();
                    options.PermissionClaimTypes.Add(
                        "custom-permission");
                });

        using var serviceProvider =
            services.BuildServiceProvider();

        var options =
            serviceProvider.GetRequiredService<
                IOptions<
                    RuleGateSubjectOptions>>()
                .Value;

        Assert.Equal(
            "custom-subject",
            options.SubjectIdClaimType);

        Assert.Equal(
            new[]
            {
                "custom-role",
            },
            options.RoleClaimTypes);

        Assert.Equal(
            new[]
            {
                "custom-permission",
            },
            options.PermissionClaimTypes);
    }

    private sealed class StubSubjectFactory
        : IRuleGateSubjectFactory
    {
        public AuthorizationSubject Create(
            ClaimsPrincipal principal)
        {
            return new AuthorizationSubject(
                "stub-subject");
        }
    }
}
