using System.Reflection;
using Fotbiler.RuleGate.Abstractions.Diagnostics;
using Fotbiler.RuleGate.Abstractions.Evaluation;
using Fotbiler.RuleGate.Abstractions.Policies;
using Fotbiler.RuleGate.AspNetCore.Authorization;
using Fotbiler.RuleGate.AspNetCore.Diagnostics;
using Fotbiler.RuleGate.AspNetCore.Enrichment;
using Fotbiler.RuleGate.AspNetCore.PolicySources;
using Fotbiler.RuleGate.AspNetCore.Subjects;
using Fotbiler.RuleGate.Core.Policies;
using Fotbiler.RuleGate.Manifest.PolicySources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

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

    public static RuleGateBuilder AddPolicySource<TPolicySource>(
        this RuleGateBuilder builder)
        where TPolicySource : class, IPolicySource
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.TryAddEnumerable(
            ServiceDescriptor.Singleton<
                IPolicySource,
                TPolicySource>());

        return UsePolicySources(builder);
    }

    public static RuleGateBuilder AddPolicySource(
        this RuleGateBuilder builder,
        IPolicySource source)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(source);

        builder.Services.AddSingleton(source);

        return UsePolicySources(builder);
    }

    public static RuleGateBuilder AddYamlPolicyFile(
        this RuleGateBuilder builder,
        string path,
        Action<YamlPolicyFileOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var options = new YamlPolicyFileOptions();
        configure?.Invoke(options);

        var source = new YamlFilePolicySource(
            path,
            options);

        builder.Services.AddSingleton(source);
        builder.Services.AddSingleton<IPolicySource>(
            source);

        return UsePolicySources(builder);
    }

    public static RuleGateBuilder AddEmbeddedPolicyResource(
        this RuleGateBuilder builder,
        Assembly assembly,
        string resourceName)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(assembly);
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceName);

        builder.Services.AddSingleton<IPolicySource>(
            new EmbeddedResourcePolicySource(
                assembly,
                resourceName));

        return UsePolicySources(builder);
    }

    public static RuleGateBuilder AddConfigurationPolicySource(
        this RuleGateBuilder builder,
        IConfiguration configuration,
        string sectionPath,
        Action<ConfigurationPolicySourceOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionPath);

        var options =
            new ConfigurationPolicySourceOptions();

        configure?.Invoke(options);

        var source = new ConfigurationPolicySource(
            configuration,
            sectionPath,
            options);

        builder.Services.AddSingleton(source);
        builder.Services.AddSingleton<IPolicySource>(
            source);

        return UsePolicySources(builder);
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

#if !NETCOREAPP3_1
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
#endif

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

        if (!Enum.IsDefined(
                typeof(ServiceLifetime),
                lifetime))
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

    private static RuleGateBuilder UsePolicySources(
        RuleGateBuilder builder)
    {
        builder.Services.AddLogging();

        builder.Services.RemoveAll<IPolicyProvider>();

        builder.Services.TryAddSingleton<
            AtomicPolicyProvider>();

        builder.Services.AddSingleton<IPolicyProvider>(
            static services =>
                services.GetRequiredService<
                    AtomicPolicyProvider>());

        builder.Services.TryAddSingleton<
            IPolicyReloadService>(
                static services =>
                    services.GetRequiredService<
                        AtomicPolicyProvider>());

        builder.Services.TryAddEnumerable(
            ServiceDescriptor.Singleton<
                IHostedService,
                PolicySourceReloadHostedService>());

        return builder;
    }
}
