using Fotbiler.RuleGate.Abstractions.Authorization;
using Fotbiler.RuleGate.Abstractions.Evaluation;
using Fotbiler.RuleGate.Abstractions.Policies;
using Fotbiler.RuleGate.AspNetCore.Authorization;
using Fotbiler.RuleGate.AspNetCore.Subjects;
using Fotbiler.RuleGate.Core.Engine;
using Fotbiler.RuleGate.Core.Evaluation;
using Fotbiler.RuleGate.Core.Evaluation.Evaluators;
using Fotbiler.RuleGate.Core.Policies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Fotbiler.RuleGate.AspNetCore.DependencyInjection;

public static class RuleGateServiceCollectionExtensions
{
    public static RuleGateBuilder AddRuleGate(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<
            RuleGateSubjectOptions>();

        services.TryAddSingleton<
            IRuleGateSubjectFactory,
            ClaimsPrincipalRuleGateSubjectFactory>();

        services.TryAddSingleton<
            TimeProvider>(
                TimeProvider.System);

        services.Replace(
            ServiceDescriptor.Singleton<
                IAuthorizationPolicyProvider,
                RuleGateAuthorizationPolicyProvider>());

        services.TryAddSingleton<
            IRuleGateAuthorizationResourceFactory,
            RuleGateAuthorizationResourceFactory>();

        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<
                IAuthorizationHandler,
                RuleGateAuthorizationHandler>());

        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<
                IRequirementEvaluator,
                PermissionRequirementEvaluator>());

        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<
                IRequirementEvaluator,
                RoleRequirementEvaluator>());
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<
                IRequirementEvaluator,
                AttributeRequirementEvaluator>());

        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<
                IRequirementEvaluator,
                AllRequirementEvaluator>());

        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<
                IRequirementEvaluator,
                AnyRequirementEvaluator>());

        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<
                IRequirementEvaluator,
                NotRequirementEvaluator>());

        services.TryAddSingleton<
            IRequirementEvaluationDispatcher,
            RequirementEvaluationDispatcher>();

        services.TryAddSingleton<
            IPolicyProvider,
            InMemoryPolicyProvider>();

        services.TryAddSingleton<
            IAuthorizationEngine,
            PolicyAuthorizationEngine>();

        return new RuleGateBuilder(services);
    }
}
