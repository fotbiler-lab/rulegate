using System.Diagnostics;
using System.Security.Claims;
using Fotbiler.RuleGate.Abstractions.Attributes;
using Fotbiler.RuleGate.Abstractions.Authorization;
using Fotbiler.RuleGate.Abstractions.Policies;
using RuleGateAuthorizationContext =
    Fotbiler.RuleGate.Abstractions.Authorization.AuthorizationContext;

namespace Fotbiler.RuleGate.AspNetCore.Enrichment;

public sealed class RuleGateAuthorizationRequestEnricher
    : IRuleGateAuthorizationRequestEnricher
{
    private readonly IReadOnlyList<ProviderRegistration>
        _subjectProviders;

    private readonly IReadOnlyList<ProviderRegistration>
        _resourceProviders;

    private readonly IReadOnlyList<ProviderRegistration>
        _contextProviders;

    private readonly IReadOnlyList<
        IRuleGateEnrichmentDiagnosticsSink>
        _diagnosticsSinks;

    public RuleGateAuthorizationRequestEnricher(
        IEnumerable<IRuleGateSubjectAttributeProvider>
            subjectProviders,
        IEnumerable<IRuleGateResourceAttributeProvider>
            resourceProviders,
        IEnumerable<IRuleGateContextAttributeProvider>
            contextProviders,
        IEnumerable<IRuleGateEnrichmentDiagnosticsSink>
            diagnosticsSinks)
    {
        ArgumentNullException.ThrowIfNull(
            subjectProviders);
        ArgumentNullException.ThrowIfNull(
            resourceProviders);
        ArgumentNullException.ThrowIfNull(
            contextProviders);
        ArgumentNullException.ThrowIfNull(
            diagnosticsSinks);

        _subjectProviders = CreateRegistrations(
            subjectProviders,
            AuthorizationAttributeSource.Subject);

        _resourceProviders = CreateRegistrations(
            resourceProviders,
            AuthorizationAttributeSource.Resource);

        _contextProviders = CreateRegistrations(
            contextProviders,
            AuthorizationAttributeSource.Context);

        _diagnosticsSinks = diagnosticsSinks.ToArray();
    }

    public async ValueTask<
        RuleGateAuthorizationRequestEnrichmentResult>
        EnrichAsync(
            AuthorizationRequest request,
            ClaimsPrincipal principal,
            object? frameworkResource,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(principal);

        if (cancellationToken.IsCancellationRequested)
        {
            return RuleGateAuthorizationRequestEnrichmentResult
                .Fail();
        }

        if (_subjectProviders.Count == 0 &&
            _resourceProviders.Count == 0 &&
            _contextProviders.Count == 0)
        {
            return RuleGateAuthorizationRequestEnrichmentResult
                .Success(request);
        }

        var subject = request.Subject;
        var resource = request.Resource;
        var context = request.Context;

        var subjectResult = await EnrichAttributesAsync(
            _subjectProviders,
            subject.Attributes,
            attributes =>
                CreateProviderContext(
                    principal,
                    frameworkResource,
                    new AuthorizationSubject(
                        subject.Id,
                        subject.Roles,
                        subject.Permissions,
                        attributes),
                    resource,
                    request.Action,
                    context),
            cancellationToken);

        if (!subjectResult.IsSuccessful)
        {
            return RuleGateAuthorizationRequestEnrichmentResult
                .Fail();
        }

        subject = new AuthorizationSubject(
            subject.Id,
            subject.Roles,
            subject.Permissions,
            subjectResult.Attributes!);

        var resourceResult = await EnrichAttributesAsync(
            _resourceProviders,
            resource.Attributes,
            attributes =>
                CreateProviderContext(
                    principal,
                    frameworkResource,
                    subject,
                    new AuthorizationResource(
                        resource.Type,
                        resource.Id,
                        attributes),
                    request.Action,
                    context),
            cancellationToken);

        if (!resourceResult.IsSuccessful)
        {
            return RuleGateAuthorizationRequestEnrichmentResult
                .Fail();
        }

        resource = new AuthorizationResource(
            resource.Type,
            resource.Id,
            resourceResult.Attributes!);

        var contextResult = await EnrichAttributesAsync(
            _contextProviders,
            context.Attributes,
            attributes =>
                CreateProviderContext(
                    principal,
                    frameworkResource,
                    subject,
                    resource,
                    request.Action,
                    new RuleGateAuthorizationContext(
                        context.EvaluationTime,
                        attributes)),
            cancellationToken);

        if (!contextResult.IsSuccessful)
        {
            return RuleGateAuthorizationRequestEnrichmentResult
                .Fail();
        }

        context = new RuleGateAuthorizationContext(
            context.EvaluationTime,
            contextResult.Attributes!);

        return RuleGateAuthorizationRequestEnrichmentResult
            .Success(
                new AuthorizationRequest(
                    subject,
                    resource,
                    request.Action,
                    context));
    }

    private async ValueTask<AttributeEnrichmentResult>
        EnrichAttributesAsync(
            IReadOnlyList<ProviderRegistration> providers,
            AuthorizationAttributes initialAttributes,
            Func<
                AuthorizationAttributes,
                RuleGateAttributeProviderContext>
                createContext,
            CancellationToken cancellationToken)
    {
        var values = initialAttributes.ToDictionary(
            static pair => pair.Key,
            static pair => pair.Value,
            StringComparer.Ordinal);

        foreach (var provider in providers)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                await WriteDiagnosticAsync(
                    provider,
                    RuleGateEnrichmentOutcome.Cancelled,
                    attributeCount: 0,
                    TimeSpan.Zero,
                    cancellationToken);

                return AttributeEnrichmentResult.Fail();
            }

            var startedAt = Stopwatch.GetTimestamp();
            RuleGateAttributeProviderResult? result;

            try
            {
                result = await provider.ProvideAsync(
                    createContext(
                        new AuthorizationAttributes(values)),
                    cancellationToken);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                await WriteDiagnosticAsync(
                    provider,
                    RuleGateEnrichmentOutcome.Cancelled,
                    attributeCount: 0,
                    StopwatchCompat.GetElapsedTime(startedAt),
                    cancellationToken);

                return AttributeEnrichmentResult.Fail();
            }
            catch (Exception)
            {
                await WriteDiagnosticAsync(
                    provider,
                    RuleGateEnrichmentOutcome
                        .ProviderException,
                    attributeCount: 0,
                    StopwatchCompat.GetElapsedTime(startedAt),
                    cancellationToken);

                return AttributeEnrichmentResult.Fail();
            }

            if (result is null)
            {
                await WriteDiagnosticAsync(
                    provider,
                    RuleGateEnrichmentOutcome.ProviderFailed,
                    attributeCount: 0,
                    StopwatchCompat.GetElapsedTime(startedAt),
                    cancellationToken);

                return AttributeEnrichmentResult.Fail();
            }

            if (!result.IsSuccessful)
            {
                var outcome = result.Status ==
                    RuleGateAttributeProviderResultStatus
                        .MissingRequiredData
                    ? RuleGateEnrichmentOutcome
                        .MissingRequiredData
                    : RuleGateEnrichmentOutcome
                        .ProviderFailed;

                await WriteDiagnosticAsync(
                    provider,
                    outcome,
                    attributeCount: 0,
                    StopwatchCompat.GetElapsedTime(startedAt),
                    cancellationToken);

                return AttributeEnrichmentResult.Fail();
            }

            var mergeOutcome = MergeAttributes(
                values,
                result.Attributes,
                provider.CollisionBehavior);

            await WriteDiagnosticAsync(
                provider,
                mergeOutcome,
                result.Attributes.Count,
                StopwatchCompat.GetElapsedTime(startedAt),
                cancellationToken);

            if (mergeOutcome !=
                RuleGateEnrichmentOutcome.Succeeded)
            {
                return AttributeEnrichmentResult.Fail();
            }
        }

        return AttributeEnrichmentResult.Success(
            new AuthorizationAttributes(values));
    }

    private static RuleGateEnrichmentOutcome
        MergeAttributes(
            IDictionary<string, object?> current,
            AuthorizationAttributes additions,
            RuleGateAttributeCollisionBehavior
                collisionBehavior)
    {
        if (!Enum.IsDefined(
                typeof(RuleGateAttributeCollisionBehavior),
                collisionBehavior))
        {
            return RuleGateEnrichmentOutcome
                .ProviderFailed;
        }

        foreach (var pair in additions)
        {
            if (string.IsNullOrWhiteSpace(pair.Key))
            {
                return RuleGateEnrichmentOutcome
                    .InvalidAttribute;
            }

            try
            {
                _ = AuthorizationAttributeValue.Create(
                    pair.Value);
            }
            catch (ArgumentException)
            {
                return RuleGateEnrichmentOutcome
                    .InvalidAttribute;
            }

            if (!current.ContainsKey(pair.Key))
            {
                current.Add(pair.Key, pair.Value);
                continue;
            }

            switch (collisionBehavior)
            {
                case RuleGateAttributeCollisionBehavior.Fail:
                    return RuleGateEnrichmentOutcome
                        .AttributeCollision;

                case RuleGateAttributeCollisionBehavior
                    .KeepExisting:
                    continue;

                case RuleGateAttributeCollisionBehavior
                    .ReplaceExisting:
                    current[pair.Key] = pair.Value;
                    continue;
            }
        }

        return RuleGateEnrichmentOutcome.Succeeded;
    }

    private async ValueTask WriteDiagnosticAsync(
        ProviderRegistration provider,
        RuleGateEnrichmentOutcome outcome,
        int attributeCount,
        TimeSpan duration,
        CancellationToken cancellationToken)
    {
        if (_diagnosticsSinks.Count == 0)
        {
            return;
        }

        var diagnostic = new RuleGateEnrichmentDiagnostic(
            provider.AttributeSource,
            provider.ProviderName,
            provider.Order,
            provider.CollisionBehavior,
            outcome,
            attributeCount,
            duration);

        foreach (var sink in _diagnosticsSinks)
        {
            try
            {
                await sink.WriteAsync(
                    diagnostic,
                    cancellationToken);
            }
            catch (Exception)
            {
                // Diagnostics must not change authorization behavior.
            }
        }
    }

    private static RuleGateAttributeProviderContext
        CreateProviderContext(
            ClaimsPrincipal principal,
            object? frameworkResource,
            AuthorizationSubject subject,
            AuthorizationResource resource,
            string action,
            RuleGateAuthorizationContext context)
    {
        return new RuleGateAttributeProviderContext(
            principal,
            frameworkResource,
            subject,
            resource,
            action,
            context);
    }

    private static IReadOnlyList<ProviderRegistration>
        CreateRegistrations(
            IEnumerable<
                IRuleGateSubjectAttributeProvider>
                providers,
            AuthorizationAttributeSource source)
    {
        return providers
            .Select(
                (provider, index) =>
                    new ProviderRegistration(
                        source,
                        provider.GetType(),
                        provider.Order,
                        provider.CollisionBehavior,
                        provider.ProvideAttributesAsync,
                        index))
            .OrderBy(
                static registration =>
                    registration.Order)
            .ThenBy(
                static registration =>
                    registration.RegistrationIndex)
            .ToArray();
    }

    private static IReadOnlyList<ProviderRegistration>
        CreateRegistrations(
            IEnumerable<
                IRuleGateResourceAttributeProvider>
                providers,
            AuthorizationAttributeSource source)
    {
        return providers
            .Select(
                (provider, index) =>
                    new ProviderRegistration(
                        source,
                        provider.GetType(),
                        provider.Order,
                        provider.CollisionBehavior,
                        provider.ProvideAttributesAsync,
                        index))
            .OrderBy(
                static registration =>
                    registration.Order)
            .ThenBy(
                static registration =>
                    registration.RegistrationIndex)
            .ToArray();
    }

    private static IReadOnlyList<ProviderRegistration>
        CreateRegistrations(
            IEnumerable<
                IRuleGateContextAttributeProvider>
                providers,
            AuthorizationAttributeSource source)
    {
        return providers
            .Select(
                (provider, index) =>
                    new ProviderRegistration(
                        source,
                        provider.GetType(),
                        provider.Order,
                        provider.CollisionBehavior,
                        provider.ProvideAttributesAsync,
                        index))
            .OrderBy(
                static registration =>
                    registration.Order)
            .ThenBy(
                static registration =>
                    registration.RegistrationIndex)
            .ToArray();
    }

    private sealed class ProviderRegistration
    {
        public ProviderRegistration(
            AuthorizationAttributeSource attributeSource,
            Type providerType,
            int order,
            RuleGateAttributeCollisionBehavior
                collisionBehavior,
            Func<
                RuleGateAttributeProviderContext,
                CancellationToken,
                ValueTask<
                    RuleGateAttributeProviderResult>>
                provideAsync,
            int registrationIndex)
        {
            AttributeSource = attributeSource;
            ProviderName =
                providerType.FullName ?? providerType.Name;
            Order = order;
            CollisionBehavior = collisionBehavior;
            ProvideAsync = provideAsync;
            RegistrationIndex = registrationIndex;
        }

        public AuthorizationAttributeSource
            AttributeSource
        { get; }

        public string ProviderName { get; }

        public int Order { get; }

        public RuleGateAttributeCollisionBehavior
            CollisionBehavior
        { get; }

        public Func<
            RuleGateAttributeProviderContext,
            CancellationToken,
            ValueTask<
                RuleGateAttributeProviderResult>>
            ProvideAsync
        { get; }

        public int RegistrationIndex { get; }
    }

    private sealed class AttributeEnrichmentResult
    {
        private AttributeEnrichmentResult(
            AuthorizationAttributes? attributes)
        {
            Attributes = attributes;
        }

        public bool IsSuccessful =>
            Attributes is not null;

        public AuthorizationAttributes? Attributes { get; }

        public static AttributeEnrichmentResult Success(
            AuthorizationAttributes attributes)
        {
            ArgumentNullException.ThrowIfNull(attributes);

            return new AttributeEnrichmentResult(
                attributes);
        }

        public static AttributeEnrichmentResult Fail()
        {
            return new AttributeEnrichmentResult(
                attributes: null);
        }
    }
}
