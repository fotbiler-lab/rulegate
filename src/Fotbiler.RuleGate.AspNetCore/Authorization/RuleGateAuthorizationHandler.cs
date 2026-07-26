using Fotbiler.RuleGate.Abstractions.Authorization;
using Fotbiler.RuleGate.AspNetCore.Subjects;
using Microsoft.AspNetCore.Authorization;
using RuleGateAuthorizationContext =
    Fotbiler.RuleGate.Abstractions.Authorization.AuthorizationContext;

namespace Fotbiler.RuleGate.AspNetCore.Authorization;

public sealed class RuleGateAuthorizationHandler
    : AuthorizationHandler<
        RuleGateAuthorizationRequirement>
{
    private readonly IAuthorizationEngine
        _authorizationEngine;

    private readonly IRuleGateSubjectFactory
        _subjectFactory;

    private readonly
        IRuleGateAuthorizationResourceFactory
        _resourceFactory;

    private readonly TimeProvider _timeProvider;

    public RuleGateAuthorizationHandler(
        IAuthorizationEngine authorizationEngine,
        IRuleGateSubjectFactory subjectFactory,
        IRuleGateAuthorizationResourceFactory
            resourceFactory,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(
            authorizationEngine);

        ArgumentNullException.ThrowIfNull(
            subjectFactory);

        ArgumentNullException.ThrowIfNull(
            resourceFactory);

        ArgumentNullException.ThrowIfNull(
            timeProvider);

        _authorizationEngine =
            authorizationEngine;

        _subjectFactory =
            subjectFactory;

        _resourceFactory =
            resourceFactory;

        _timeProvider =
            timeProvider;
    }

    protected override async Task
        HandleRequirementAsync(
            AuthorizationHandlerContext context,
            RuleGateAuthorizationRequirement
                requirement)
    {
        AuthorizationSubject subject;
        AuthorizationResource resource;

        try
        {
            subject =
                _subjectFactory.Create(
                    context.User);

            resource =
                _resourceFactory.Create(
                    context.Resource);
        }
        catch (ArgumentException)
        {
            context.Fail();
            return;
        }
        catch (InvalidOperationException)
        {
            context.Fail();
            return;
        }

        var request =
            new AuthorizationRequest(
                subject: subject,
                resource: resource,
                action: requirement.Action,
                context:
                    new RuleGateAuthorizationContext(
                        _timeProvider.GetUtcNow()));

        var decision =
            await _authorizationEngine
                .EvaluateAsync(request);

        if (decision.IsAllowed)
        {
            context.Succeed(requirement);
            return;
        }

        context.Fail();
    }
}
