using Fotbiler.RuleGate.Abstractions.Evaluation;
using Fotbiler.RuleGate.Abstractions.Policies;
using Fotbiler.RuleGate.AspNetCore.Subjects;
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
}
