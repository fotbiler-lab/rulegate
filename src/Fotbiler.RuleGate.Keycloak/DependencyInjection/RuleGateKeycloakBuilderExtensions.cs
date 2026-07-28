using Fotbiler.RuleGate.AspNetCore.DependencyInjection;
using Fotbiler.RuleGate.AspNetCore.Subjects;
using Fotbiler.RuleGate.Keycloak.Subjects;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Fotbiler.RuleGate.Keycloak.DependencyInjection;

public static class RuleGateKeycloakBuilderExtensions
{
    public static RuleGateBuilder
        UseKeycloakSubjectMapping(
            this RuleGateBuilder builder,
            Action<RuleGateKeycloakSubjectOptions>?
                configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddOptions<
            RuleGateKeycloakSubjectOptions>();

        if (configure is not null)
        {
            builder.Services.Configure(configure);
        }

        builder.Services.Replace(
            ServiceDescriptor.Singleton<
                IRuleGateSubjectFactory,
                KeycloakRuleGateSubjectFactory>());

        return builder;
    }
}
