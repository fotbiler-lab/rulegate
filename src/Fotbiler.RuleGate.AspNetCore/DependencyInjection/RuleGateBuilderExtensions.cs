using Fotbiler.RuleGate.Abstractions.Diagnostics;
using Fotbiler.RuleGate.Abstractions.Evaluation;
using Fotbiler.RuleGate.Abstractions.Policies;
using Fotbiler.RuleGate.AspNetCore.Authorization;
using Fotbiler.RuleGate.AspNetCore.Diagnostics;
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

        return builder;
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
}
