using Fotbiler.RuleGate.Abstractions.Diagnostics;
using Fotbiler.RuleGate.Abstractions.Evaluation;
using Fotbiler.RuleGate.Abstractions.Policies;
using Fotbiler.RuleGate.AspNetCore.Authorization;
using Fotbiler.RuleGate.AspNetCore.Diagnostics;
using Fotbiler.RuleGate.AspNetCore.Enrichment;
using Fotbiler.RuleGate.AspNetCore.Subjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Fotbiler.RuleGate.AspNetCore.DependencyInjection;

public static class RuleGateBuilderExtensions
{
    public static RuleGateBuilder AddPolicy(
        this RuleGateBuilder builder,
        PolicyDefinition policy)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(policy);

        builder.Services.AddSingleton(policy);

        return builder;
    }

    public static RuleGateBuilder AddPolicies(
        this RuleGateBuilder builder,
        IEnumerable<PolicyDefinition> policies)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(policies);

        var policyArray = policies.ToArray();

        foreach (var policy in policyArray)
        {
            ArgumentNullException.ThrowIfNull(
                policy,
                nameof(policies));
        }

        foreach (var policy in policyArray)
        {
            builder.Services.AddSingleton(policy);
        }

        return builder;
    }

    public static RuleGateBuilder ConfigureSubjectMapping(
        this RuleGateBuilder builder,
        Action<RuleGateSubjectOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        builder.Services.Configure<
            RuleGateSubjectOptions>(configure);

        return builder;
    }

    public static RuleGateBuilder AddRequirementEvaluator<
        TRequirementEvaluator>(
        this RuleGateBuilder builder)
        where TRequirementEvaluator :
            class,
            IRequirementEvaluator
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.TryAddEnumerable(
            ServiceDescriptor.Singleton<
                IRequirementEvaluator,
                TRequirementEvaluator>());

        return builder;
    }

    public static RuleGateBuilder AddLoggingDiagnostics(
        this RuleGateBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddLogging();

        builder.Services.TryAddSingleton<
            IAuthorizationDiagnosticsSink,
            LoggingAuthorizationDiagnosticsSink>();

        builder.Services.TryAddSingleton<
            IRuleGateEnrichmentDiagnosticsSink,
            LoggingRuleGateEnrichmentDiagnosticsSink>();

        return builder;
    }

    public static RuleGateBuilder
        AddSubjectAttributeProvider<TProvider>(
            this RuleGateBuilder builder,
            ServiceLifetime lifetime =
                ServiceLifetime.Scoped)
        where TProvider :
            class,
            IRuleGateSubjectAttributeProvider
    {
        return AddAttributeProvider<
            IRuleGateSubjectAttributeProvider,
            TProvider>(
                builder,
                lifetime);
    }

    public static RuleGateBuilder
        AddResourceAttributeProvider<TProvider>(
            this RuleGateBuilder builder,
            ServiceLifetime lifetime =
                ServiceLifetime.Scoped)
        where TProvider :
            class,
            IRuleGateResourceAttributeProvider
    {
        return AddAttributeProvider<
            IRuleGateResourceAttributeProvider,
            TProvider>(
                builder,
                lifetime);
    }

    public static RuleGateBuilder
        AddContextAttributeProvider<TProvider>(
            this RuleGateBuilder builder,
            ServiceLifetime lifetime =
                ServiceLifetime.Scoped)
        where TProvider :
            class,
            IRuleGateContextAttributeProvider
    {
        return AddAttributeProvider<
            IRuleGateContextAttributeProvider,
            TProvider>(
                builder,
                lifetime);
    }

    public static RuleGateBuilder
        AddHttpAuthorizationResultMapping(
            this RuleGateBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var handlerDescriptors =
            builder.Services
                .Where(
                    descriptor =>
                        descriptor.ServiceType ==
                        typeof(
                            IAuthorizationMiddlewareResultHandler))
                .ToArray();

        var hasCustomHandler =
            handlerDescriptors.Any(
                static descriptor =>
                    descriptor.ImplementationType !=
                        typeof(
                            AuthorizationMiddlewareResultHandler) &&
                    descriptor.ImplementationType !=
                        typeof(
                            RuleGateAuthorizationMiddlewareResultHandler));

        if (hasCustomHandler)
        {
            return builder;
        }

        builder.Services.RemoveAll<
            IAuthorizationMiddlewareResultHandler>();

        builder.Services.AddSingleton<
            IAuthorizationMiddlewareResultHandler,
            RuleGateAuthorizationMiddlewareResultHandler>();

        return builder;
    }

    private static RuleGateBuilder
        AddAttributeProvider<TProviderService, TProvider>(
            RuleGateBuilder builder,
            ServiceLifetime lifetime)
        where TProviderService : class
        where TProvider :
            class,
            TProviderService
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (!Enum.IsDefined(lifetime))
        {
            throw new ArgumentOutOfRangeException(
                nameof(lifetime));
        }

        builder.Services.TryAddEnumerable(
            ServiceDescriptor.Describe(
                typeof(TProviderService),
                typeof(TProvider),
                lifetime));

        return builder;
    }
}
