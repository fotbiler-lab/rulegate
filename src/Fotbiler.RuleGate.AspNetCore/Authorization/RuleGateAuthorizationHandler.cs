using Fotbiler.RuleGate.Abstractions.Authorization;
using Fotbiler.RuleGate.AspNetCore.Enrichment;
using Fotbiler.RuleGate.AspNetCore.Subjects;
using Fotbiler.RuleGate.AspNetCore.Time;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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

    private readonly IRuleGateClock _clock;

    private readonly IRuleGateAuthorizationRequestEnricher
        _requestEnricher;

    public RuleGateAuthorizationHandler(
        IAuthorizationEngine authorizationEngine,
        IRuleGateSubjectFactory subjectFactory,
        IRuleGateAuthorizationResourceFactory
            resourceFactory,
        IRuleGateClock clock,
        IRuleGateAuthorizationRequestEnricher
            requestEnricher)
    {
        ArgumentNullException.ThrowIfNull(
            authorizationEngine);

        ArgumentNullException.ThrowIfNull(
            subjectFactory);

        ArgumentNullException.ThrowIfNull(
            resourceFactory);

        ArgumentNullException.ThrowIfNull(
            clock);

        ArgumentNullException.ThrowIfNull(
            requestEnricher);

        _authorizationEngine =
            authorizationEngine;

        _subjectFactory =
            subjectFactory;

        _resourceFactory =
            resourceFactory;

        _clock =
            clock;

        _requestEnricher =
            requestEnricher;
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
                    context.Resource,
                    requirement);
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

        if (requirement.ResourceType is not null
            && !string.Equals(
                requirement.ResourceType,
                resource.Type,
                StringComparison.Ordinal))
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
                        _clock.GetUtcNow()));

        var cancellationToken =
            context.Resource is HttpContext httpContext
                ? httpContext.RequestAborted
                : CancellationToken.None;

        RuleGateAuthorizationRequestEnrichmentResult
            enrichmentResult;

        try
        {
            enrichmentResult = await _requestEnricher
                .EnrichAsync(
                    request,
                    context.User,
                    context.Resource,
                    cancellationToken);
        }
        catch (Exception)
        {
            context.Fail();
            return;
        }

        if (!enrichmentResult.IsSuccessful)
        {
            context.Fail();
            return;
        }

        request = enrichmentResult.Request;

        var decision =
            await _authorizationEngine
                .EvaluateAsync(
                    request,
                    cancellationToken);

        if (decision.IsAllowed)
        {
            context.Succeed(requirement);
            return;
        }

        context.Fail();
    }
}
