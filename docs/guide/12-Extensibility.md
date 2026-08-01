# 12. Extensibility

RuleGate extension points adapt trusted application data and domain-specific
requirements without coupling the core engine to an identity provider,
database, or remote service.

## Extension map

| Need                              | Contract                                          |
| --------------------------------- | ------------------------------------------------- |
| Build a subject differently       | `IRuleGateSubjectFactory`                         |
| Map HTTP/domain resources         | `IRuleGateAuthorizationResourceFactory`           |
| Add subject/resource/context data | attribute provider interfaces                     |
| Transform the complete request    | `IRuleGateAuthorizationRequestEnricher`           |
| Add a programmatic requirement    | `RequirementDefinition` + `IRequirementEvaluator` |
| Supply policies                   | `IPolicySource`                                   |
| Control reload                    | `IPolicyReloadService`                            |
| Observe decisions                 | `IAuthorizationDiagnosticsSink`                   |
| Observe enrichment                | `IRuleGateEnrichmentDiagnosticsSink`              |
| Control ASP.NET evaluation time   | `IRuleGateClock`                                  |

Prefer a built-in requirement and a trusted provider when it expresses the
business rule. A custom evaluator adds code, review, test, and compatibility
responsibilities.

## Custom subject factory

Use a custom factory when claims cannot be represented by configured claim
types. It must reject ambiguous/missing identity and explicitly normalize
roles, permissions, and attributes. Register it before `AddRuleGate()` because
the default uses `TryAdd` semantics:

```csharp
builder.Services.AddSingleton<
    IRuleGateSubjectFactory,
    ApplicationSubjectFactory>();

builder.Services.AddRuleGate();
```

Keycloak users normally choose the tested `UseKeycloakSubjectMapping` helper
instead.

## Custom resource factory

Implement `IRuleGateAuthorizationResourceFactory` when imperative
authorization receives domain objects. Preserve support for `HttpContext` if
the same application uses endpoint metadata. Map only trusted server-side
state; never copy an input DTO as authoritative resource data.

## Custom request enricher

Most applications should compose focused attribute providers. A custom
`IRuleGateAuthorizationRequestEnricher` owns the complete enrichment pipeline
and is appropriate only when the standard stage model cannot express a proven
requirement. It must preserve cancellation, type validation, fail-closed
behavior, and safe diagnostics.

## Custom requirement and evaluator

Custom requirements are programmatic policy definitions. The schema-v1 YAML
manifest supports the documented built-in kinds; it does not automatically
deserialize arbitrary application requirement types.

The examples in this section use these namespaces:

```csharp
using Fotbiler.RuleGate.Abstractions.Attributes;
using Fotbiler.RuleGate.Abstractions.Authorization;
using Fotbiler.RuleGate.Abstractions.Evaluation;
using Fotbiler.RuleGate.Abstractions.Policies;
using Fotbiler.RuleGate.Core.Evaluation;
```

Define a requirement:

```csharp
public sealed record MaximumRiskRequirement : RequirementDefinition
{
    public MaximumRiskRequirement(decimal maximumRisk, string? id = null)
        : base(id)
    {
        MaximumRisk = maximumRisk;
    }

    public decimal MaximumRisk { get; }
}
```

Evaluate it deterministically:

```csharp
public sealed class MaximumRiskRequirementEvaluator
    : RequirementEvaluator<MaximumRiskRequirement>
{
    protected override ValueTask<RequirementEvaluationResult> EvaluateAsync(
        MaximumRiskRequirement requirement,
        RequirementEvaluationContext context,
        IRequirementEvaluationDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!context.AuthorizationContext.Attributes.TryGetValue(
                "riskScore",
                out var rawRisk))
        {
            return ValueTask.FromResult(
                RequirementEvaluationResult.Indeterminate(
                    new AuthorizationFailure(
                        "APPLICATION_RISK_SCORE_MISSING",
                        requirement.Id)));
        }

        AuthorizationAttributeValue normalizedRisk;

        try
        {
            normalizedRisk = AuthorizationAttributeValue.Create(rawRisk);
        }
        catch (ArgumentException)
        {
            return ValueTask.FromResult(
                RequirementEvaluationResult.Indeterminate(
                    new AuthorizationFailure(
                        "APPLICATION_RISK_SCORE_INVALID",
                        requirement.Id)));
        }

        if (normalizedRisk.Kind != AuthorizationAttributeValueKind.Number)
        {
            return ValueTask.FromResult(
                RequirementEvaluationResult.Indeterminate(
                    new AuthorizationFailure(
                        "APPLICATION_RISK_SCORE_INVALID",
                        requirement.Id)));
        }

        var risk = (decimal)normalizedRisk.Value!;

        var result = risk <= requirement.MaximumRisk
            ? RequirementEvaluationResult.Satisfied()
            : RequirementEvaluationResult.NotSatisfied(
                new AuthorizationFailure(
                    "APPLICATION_RISK_SCORE_EXCEEDED",
                    requirement.Id));

        return ValueTask.FromResult(result);
    }
}
```

Register and supply a programmatic policy:

```csharp
builder.Services
    .AddRuleGate()
    .AddRequirementEvaluator<MaximumRiskRequirementEvaluator>()
    .AddPolicy(
        new PolicyDefinition(
            id: "payment-release",
            resourceType: "payment",
            action: "release",
            requirement: new MaximumRiskRequirement(
                maximumRisk: 25m,
                id: "acceptable-risk")));
```

Custom evaluators must be deterministic, cancellation-aware, fail closed on
missing/invalid data, return stable non-sensitive codes, and avoid remote I/O.
Load remote/application data in providers before evaluation.

## Custom trusted clock

ASP.NET Core constructs context time through `IRuleGateClock`. A test clock:

```csharp
public sealed class FixedRuleGateClock : IRuleGateClock
{
    public DateTimeOffset UtcNow { get; set; }

    public DateTimeOffset GetUtcNow() => UtcNow;
}
```

Register it before `AddRuleGate()`:

```csharp
builder.Services.AddSingleton<IRuleGateClock>(
    new FixedRuleGateClock
    {
        UtcNow = new DateTimeOffset(2026, 8, 3, 6, 0, 0, TimeSpan.Zero),
    });

builder.Services.AddRuleGate();
```

Use this for deterministic host integration tests. Production clocks should
be trusted, monotonic enough for the operation, and UTC-based.

## Custom policy source

Implement `IPolicySource` to transform application-owned local configuration
into complete `PolicyDefinition` values. Keep loading bounded and return
structured diagnostics. Do not perform authorization by calling a remote
service from an evaluator.

## Custom diagnostics

Custom sinks receive already-structured diagnostics. Preserve the built-in
privacy boundary, isolate failures, avoid unbounded queues, and never attach
subject/resource IDs or attribute values to metric labels.

## Extension review checklist

- Is a built-in feature sufficient?
- Who owns and validates the input?
- What happens when data is missing, malformed, stale, cancelled, or throws?
- Are identifiers exact and stable?
- Can the extension allow on uncertainty?
- Is it safe under the registered DI lifetime and concurrency?
- Does it leak data through logs, exceptions, metrics, or HTTP responses?
- Are allow, deny, and indeterminate paths tested?
- Does an upgrade preserve the public contract used by the extension?

## Further reference

- [ASP.NET Core custom evaluators](../aspnetcore.md#custom-requirement-evaluators)
- [Enrichment reference](../enrichment.md)
- [Diagnostics reference](../diagnostics.md)
- [Policy source reference](../policy-sources.md)

---

Previous: [Policy sources and reload](11-Policy-Sources-and-Reload.md) · Next:
[Real-world recipes](13-Real-World-Recipes.md)
