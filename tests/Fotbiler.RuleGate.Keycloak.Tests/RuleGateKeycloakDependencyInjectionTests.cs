using Fotbiler.RuleGate.AspNetCore.DependencyInjection;
using Fotbiler.RuleGate.AspNetCore.Subjects;
using Fotbiler.RuleGate.Keycloak.DependencyInjection;
using Fotbiler.RuleGate.Keycloak.Subjects;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Fotbiler.RuleGate.Keycloak.Tests;

public sealed class
    RuleGateKeycloakDependencyInjectionTests
{
    [Fact]
    public void UseKeycloakSubjectMapping_ReplacesOnlyTheSubjectMapper()
    {
        var services = new ServiceCollection();

        services
            .AddRuleGate()
            .UseKeycloakSubjectMapping(
                options =>
                    options.ClientIds.Add("web"));

        using var provider =
            services.BuildServiceProvider();

        Assert.IsType<
            KeycloakRuleGateSubjectFactory>(
                provider.GetRequiredService<
                    IRuleGateSubjectFactory>());

        Assert.Contains(
            "web",
            provider.GetRequiredService<
                    IOptions<
                        RuleGateKeycloakSubjectOptions>>()
                .Value
                .ClientIds);
    }
}
