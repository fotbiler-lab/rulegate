using Fotbiler.RuleGate.Abstractions.Attributes;
using Fotbiler.RuleGate.Abstractions.Authorization;
using Fotbiler.RuleGate.Core.Engine;
using Fotbiler.RuleGate.Core.Evaluation;
using Fotbiler.RuleGate.Core.Evaluation.Evaluators;
using Fotbiler.RuleGate.Core.Policies;
using Fotbiler.RuleGate.Manifest.Compilation;

namespace Fotbiler.RuleGate.Integration.Tests;

public sealed class ManifestTimeContextAuthorizationIntegrationTests
{
    private const string Manifest = """
        schemaVersion: 1
        application:
          id: secure-portal
          name: Secure Portal
        policies:
          - id: portal-access
            resourceType: portal
            action: access
            requirement:
              all:
                - timeWindow:
                    days: [wednesday]
                    start: "08:00"
                    end: "18:00"
                    timeZone: Europe/Istanbul
                - contextAge:
                    timestamp: mfa
                    maximumAge: "00:15:00"
                - context:
                    property: authenticationMethod
                    operator: equal
                    valueType: string
                    value: mfa
                - context:
                    property: networkZone
                    operator: in
                    valueType: stringCollection
                    value: [internal, vpn]
                - context:
                    property: trustedDevice
                    operator: equal
                    valueType: boolean
                    value: true
        """;

    [Fact]
    public async Task Matching_time_and_trusted_context_allow_access()
    {
        var now = new DateTimeOffset(
            2026, 7, 29, 9, 0, 0,
            TimeSpan.Zero);

        var decision = await CreateEngine().EvaluateAsync(
            CreateRequest(
                now,
                new AuthorizationAttributes(
                [
                    Pair(
                        AuthorizationContextAttributeNames
                            .MultiFactorAuthenticationTime,
                        now.AddMinutes(-10)),
                    Pair(
                        AuthorizationContextAttributeNames
                            .AuthenticationMethod,
                        "mfa"),
                    Pair(
                        AuthorizationContextAttributeNames.NetworkZone,
                        "vpn"),
                    Pair(
                        AuthorizationContextAttributeNames.TrustedDevice,
                        true)
                ])));

        Assert.True(decision.IsAllowed);
    }

    [Fact]
    public async Task Request_derived_context_is_not_inferred_or_trusted()
    {
        var now = new DateTimeOffset(
            2026, 7, 29, 9, 0, 0,
            TimeSpan.Zero);

        var decision = await CreateEngine().EvaluateAsync(
            CreateRequest(now, AuthorizationAttributes.Empty));

        Assert.False(decision.IsAllowed);
        Assert.NotEmpty(decision.Failures);
    }

    private static PolicyAuthorizationEngine CreateEngine()
    {
        var compilation = new RuleGateManifestCompiler()
            .CompileFromText(Manifest);

        Assert.True(compilation.IsSuccess);

        return new PolicyAuthorizationEngine(
            new InMemoryPolicyProvider(compilation.Policies),
            new RequirementEvaluationDispatcher(
            [
                new TimeWindowRequirementEvaluator(),
                new ContextAgeRequirementEvaluator(),
                new ContextRequirementEvaluator(),
                new AllRequirementEvaluator()
            ]));
    }

    private static AuthorizationRequest CreateRequest(
        DateTimeOffset evaluationTime,
        AuthorizationAttributes attributes)
    {
        return new AuthorizationRequest(
            new AuthorizationSubject("user-1"),
            new AuthorizationResource("portal", "portal-1"),
            "access",
            new AuthorizationContext(evaluationTime, attributes));
    }

    private static KeyValuePair<string, object?> Pair(
        string name,
        object? value)
    {
        return new KeyValuePair<string, object?>(name, value);
    }
}
