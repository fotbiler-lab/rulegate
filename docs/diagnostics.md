# RuleGate Diagnostics

RuleGate diagnostics provide structured information about ASP.NET Core
attribute enrichment, completed authorization evaluations, and their
requirement trees. Policy-source hosting also emits safe snapshot activation
and reload-rejection events.

Diagnostics are intended for:

- Trusted application logs
- Operational troubleshooting
- Authorization test assertions
- Custom observability integrations
- Performance investigation

Diagnostics do not change whether access is allowed or denied.

For ASP.NET Core registration and endpoint integration, read the
[ASP.NET Core integration guide](aspnetcore.md).

For the wider runtime trust model, read the
[security model](security.md).

## Diagnostics are disabled by default

Calling `AddRuleGate()` alone does not enable diagnostics:

```csharp
builder.Services
    .AddRuleGate()
    .AddPolicies(policies);
```

When no diagnostics sink is registered, RuleGate uses the non-diagnostic
evaluation path.

This avoids constructing evaluation identifiers, requirement traces, and
diagnostic snapshots when diagnostics are not needed.

## Enable built-in logging diagnostics

Enable the ASP.NET Core structured logging sink explicitly:

```csharp
using Fotbiler.RuleGate.AspNetCore.DependencyInjection;

builder.Services
    .AddRuleGate()
    .AddLoggingDiagnostics()
    .AddPolicies(policies);
```

`AddLoggingDiagnostics()`:

- Adds Microsoft.Extensions.Logging services
- Registers one singleton `IAuthorizationDiagnosticsSink`
- Registers one singleton `IRuleGateEnrichmentDiagnosticsSink`
- Is idempotent
- Preserves custom sinks registered earlier

Diagnostics are produced only when the sink's `IsEnabled` property returns
`true`.

For the built-in logging sink, `IsEnabled` is true when either Information or
Debug logging is enabled for its category.

## Logging category

The current built-in logger category is:

```text
Fotbiler.RuleGate.AspNetCore.Diagnostics.LoggingAuthorizationDiagnosticsSink
```

ASP.NET Core enrichment uses:

```text
Fotbiler.RuleGate.AspNetCore.Diagnostics.LoggingRuleGateEnrichmentDiagnosticsSink
```

Policy source activation and reload hosting uses:

```text
Fotbiler.RuleGate.AspNetCore.PolicySources.PolicySourceReloadHostedService
```

Example `appsettings.json` configuration:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Fotbiler.RuleGate.AspNetCore.Diagnostics.LoggingAuthorizationDiagnosticsSink": "Information",
      "Fotbiler.RuleGate.AspNetCore.Diagnostics.LoggingRuleGateEnrichmentDiagnosticsSink": "Warning",
      "Fotbiler.RuleGate.AspNetCore.PolicySources.PolicySourceReloadHostedService": "Information"
    }
  }
}
```

Use `Debug` when requirement-level traces are needed:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Fotbiler.RuleGate.AspNetCore.Diagnostics.LoggingAuthorizationDiagnosticsSink": "Debug"
    }
  }
}
```

Requirement-level Debug logging can be significantly more verbose than the
authorization-level Information event.

Enable it deliberately and retain the output only inside trusted operational
boundaries.

## Built-in logging events

The built-in sinks and policy-source host emit seven structured event types.

| Event ID | Level       | Purpose                                  |
| -------: | ----------- | ---------------------------------------- |
|   `2000` | Information | Completed authorization evaluation       |
|   `2001` | Debug       | Completed requirement evaluation         |
|   `2010` | Debug       | Successful attribute-enrichment provider |
|   `2011` | Warning     | Fail-closed enrichment provider outcome  |
|   `2020` | Information | Immutable policy snapshot activated      |
|   `2021` | Warning     | Candidate policy reload rejected         |
|   `2022` | Warning     | Policy-source file watch unavailable     |

Policy reload events include snapshot version, policy count, or stable
diagnostic codes. They do not include policy values, manifest contents,
exception messages, subject IDs, resource IDs, roles, permissions, or
attribute values. The source guide documents the structured
`PolicyReloadResult` returned to trusted application code.

## Enrichment events

Events `2010` and `2011` describe one ASP.NET Core enrichment provider call.

They contain:

| Field               | Description                                   |
| ------------------- | --------------------------------------------- |
| `ProviderName`      | Provider implementation type                  |
| `AttributeSource`   | Subject, Resource, or Context                 |
| `Order`             | Provider execution order                      |
| `CollisionBehavior` | Fail, KeepExisting, or ReplaceExisting        |
| `Outcome`           | Safe structured provider outcome              |
| `AttributeCount`    | Number of attributes returned by the provider |
| `DurationMs`        | Provider execution and merge duration         |

The possible failure outcomes are:

- `MissingRequiredData`
- `ProviderFailed`
- `ProviderException`
- `AttributeCollision`
- `InvalidAttribute`
- `Cancelled`

Enrichment logs never include attribute names, attribute values, exception
messages, or provider result payloads.

Custom integrations can implement:

```csharp
using Fotbiler.RuleGate.AspNetCore.Enrichment;

public interface IRuleGateEnrichmentDiagnosticsSink
{
    ValueTask WriteAsync(
        RuleGateEnrichmentDiagnostic diagnostic,
        CancellationToken cancellationToken = default);
}
```

`RuleGateEnrichmentDiagnostic` intentionally exposes only provider type,
source, order, collision behavior, outcome, attribute count, and duration.
Diagnostic-sink exceptions are ignored so observability cannot change an
authorization result.

For provider contracts and pipeline behavior, read the
[ASP.NET Core enrichment guide](enrichment.md).

## Authorization event

Event `2000` represents the completed authorization decision.

It contains:

| Field              | Description                                               |
| ------------------ | --------------------------------------------------------- |
| `EvaluationId`     | Unique authorization evaluation identifier                |
| `PolicyId`         | Matched policy identifier, or null when no policy matched |
| `IsAllowed`        | Final allow or deny result                                |
| `DurationMs`       | Total measured evaluation duration                        |
| `FailureCodes`     | Comma-separated authorization failure codes               |
| `RequirementCount` | Number of recorded requirement evaluations                |

Conceptual log entry:

```text
RuleGate authorization evaluation <evaluation-id> completed.
PolicyId: document-read;
IsAllowed: False;
DurationMs: 1.42;
FailureCodes: RULEGATE_MISSING_PERMISSION;
RequirementCount: 1.
```

The exact formatting is controlled by Microsoft.Extensions.Logging and the
configured logging provider.

## Requirement event

Event `2001` represents one evaluated requirement.

It contains:

| Field                       | Description                                     |
| --------------------------- | ----------------------------------------------- |
| `AuthorizationEvaluationId` | Parent authorization evaluation                 |
| `RequirementEvaluationId`   | Unique requirement evaluation identifier        |
| `ParentEvaluationId`        | Parent requirement identifier in the trace tree |
| `RequirementId`             | Optional policy-defined requirement identifier  |
| `RequirementKind`           | Built-in or custom requirement category         |
| `Outcome`                   | Satisfied, NotSatisfied, or Indeterminate       |
| `DurationMs`                | Requirement evaluation duration                 |
| `FailureCodes`              | Comma-separated requirement failure codes       |
| `AttributeSource`           | Subject, Resource, or Context when applicable   |
| `ComparedAttributeSource`   | Second operand source when applicable           |

The built-in logging sink intentionally does not log the attribute name.

## Diagnostic contract

Custom integrations use:

```csharp
using Fotbiler.RuleGate.Abstractions.Diagnostics;

public interface IAuthorizationDiagnosticsSink
{
    bool IsEnabled { get; }

    ValueTask WriteAsync(
        AuthorizationEvaluationDiagnostic diagnostic,
        CancellationToken cancellationToken = default);
}
```

The engine checks `IsEnabled` before selecting the diagnostic evaluation path.

When it is false:

- No diagnostic trace session is created.
- `WriteAsync` is not called.
- Authorization behavior remains unchanged.

## AuthorizationEvaluationDiagnostic

One `AuthorizationEvaluationDiagnostic` represents one completed authorization
evaluation.

It exposes:

| Property                 | Type                                             | Description                     |
| ------------------------ | ------------------------------------------------ | ------------------------------- |
| `EvaluationId`           | `Guid`                                           | Non-empty evaluation identifier |
| `PolicyId`               | `string?`                                        | Matched policy identifier       |
| `IsAllowed`              | `bool`                                           | Final decision                  |
| `Duration`               | `TimeSpan`                                       | Total measured duration         |
| `FailureCodes`           | `IReadOnlyList<string>`                          | Decision failure codes          |
| `RequirementEvaluations` | `IReadOnlyList<RequirementEvaluationDiagnostic>` | Requirement trace               |

The model:

- Rejects an empty evaluation ID
- Rejects a negative duration
- Rejects blank failure codes
- Copies supplied collections
- Exposes read-only collection views

When no policy matches:

```text
PolicyId: null
IsAllowed: false
FailureCodes:
  - RULEGATE_NO_MATCHING_POLICY
RequirementEvaluations: empty
```

## RequirementEvaluationDiagnostic

One `RequirementEvaluationDiagnostic` represents one requirement evaluation.

It exposes:

| Property                  | Type                            | Description                                    |
| ------------------------- | ------------------------------- | ---------------------------------------------- |
| `EvaluationId`            | `Guid`                          | Unique requirement evaluation ID               |
| `ParentEvaluationId`      | `Guid?`                         | Parent requirement evaluation ID               |
| `RequirementId`           | `string?`                       | Optional policy-defined ID                     |
| `RequirementKind`         | `AuthorizationRequirementKind`  | Requirement category                           |
| `Outcome`                 | `RequirementEvaluationOutcome`  | Evaluation outcome                             |
| `Duration`                | `TimeSpan`                      | Measured duration                              |
| `FailureCodes`            | `IReadOnlyList<string>`         | Requirement failure codes                      |
| `AttributeSource`         | `AuthorizationAttributeSource?` | Attribute model                                |
| `AttributeName`           | `string?`                       | Attribute name for custom sinks                |
| `ComparedAttributeSource` | `AuthorizationAttributeSource?` | Second operand attribute model                 |
| `ComparedAttributeName`   | `string?`                       | Second operand attribute name for custom sinks |

The diagnostic model can contain attribute names for either operand.

It never contains:

- Actual attribute value
- Expected attribute value
- Raw authorization attribute value

Applications implementing custom sinks must decide whether attribute names are
safe for their environment.

The built-in logging sink does not emit `AttributeName`.

## Requirement kinds

`AuthorizationRequirementKind` contains:

| Value                 | Meaning                                         |
| --------------------- | ----------------------------------------------- |
| `Permission`          | Permission requirement                          |
| `Role`                | Role requirement                                |
| `Attribute`           | Attribute requirement                           |
| `AttributeComparison` | Attribute comparison requirement                |
| `TimeWindow`          | Recurring local time-window requirement         |
| `DateTimeWindow`      | Before, after, or bounded date-time requirement |
| `ContextAge`          | Authentication- or MFA-age requirement          |
| `Context`             | Canonical context-property requirement          |
| `All`                 | Logical all requirement                         |
| `Any`                 | Logical any requirement                         |
| `Not`                 | Logical negation requirement                    |
| `Custom`              | Application-specific requirement                |

Custom requirement definitions are reported as `Custom` unless RuleGate knows
their built-in category.

## Requirement outcomes

`RequirementEvaluationOutcome` contains:

| Outcome         | Meaning                                                 |
| --------------- | ------------------------------------------------------- |
| `Satisfied`     | The requirement succeeded                               |
| `NotSatisfied`  | The requirement evaluated normally but did not pass     |
| `Indeterminate` | The requirement could not produce a reliable comparison |

Examples of `NotSatisfied`:

- Missing permission
- Missing role
- Missing attribute
- Literal comparison returned false
- Time or date-time window was not satisfied
- Context timestamp exceeded its maximum age
- A negated child was satisfied

Examples of `Indeterminate`:

- Requirement evaluator not found
- Unsupported runtime attribute type
- Attribute type mismatch
- Context timestamp is in the future
- Unsupported operator and type combination

Both `NotSatisfied` and `Indeterminate` lead to a denied authorization decision
through the fail-closed engine.

## Requirement tree relationships

Every requirement evaluation receives a unique `EvaluationId`.

Root requirements have:

```text
ParentEvaluationId: null
```

Nested requirements point to the evaluation ID of their parent.

Example:

```text
all-root
├── permission-node
└── attribute-node
```

Conceptual identifiers:

```text
all-root
EvaluationId: A
ParentEvaluationId: null

permission-node
EvaluationId: B
ParentEvaluationId: A

attribute-node
EvaluationId: C
ParentEvaluationId: A
```

This allows custom observability tools to reconstruct nested `all`, `any`, and
`not` evaluation trees.

The top-level authorization evaluation has a separate
`AuthorizationEvaluationDiagnostic.EvaluationId`. Requirement events also
carry that authorization evaluation ID when emitted by the built-in logger.

## Failure codes

Diagnostics expose stable authorization failure codes rather than
user-facing explanations.

Examples include:

```text
RULEGATE_NO_MATCHING_POLICY
RULEGATE_MISSING_PERMISSION
RULEGATE_MISSING_ROLE
RULEGATE_REQUIREMENT_EVALUATOR_NOT_FOUND
RULEGATE_ATTRIBUTE_NOT_FOUND
RULEGATE_ATTRIBUTE_TYPE_NOT_SUPPORTED
RULEGATE_ATTRIBUTE_TYPE_MISMATCH
RULEGATE_ATTRIBUTE_OPERATOR_NOT_SUPPORTED
RULEGATE_ATTRIBUTE_COMPARISON_NOT_SATISFIED
RULEGATE_TIME_WINDOW_NOT_SATISFIED
RULEGATE_DATE_TIME_WINDOW_NOT_SATISFIED
RULEGATE_CONTEXT_AGE_NOT_SATISFIED
RULEGATE_CONTEXT_TIMESTAMP_IN_FUTURE
```

A requirement diagnostic can contain its local failure codes.

The authorization diagnostic contains the failure codes propagated into the
final denied decision.

Failure codes are useful for:

- Tests
- Alert grouping
- Operational dashboards
- Troubleshooting
- Decision analysis

They should not automatically be returned to untrusted API clients.

## Data included by the built-in logging sink

The Information authorization event includes:

- Evaluation ID
- Policy ID
- Allow or deny result
- Duration
- Failure codes
- Requirement count

The Debug requirement event includes:

- Authorization evaluation ID
- Requirement evaluation ID
- Parent requirement evaluation ID
- Requirement ID
- Requirement kind
- Outcome
- Duration
- Failure codes
- Attribute source
- Compared attribute source

## Data omitted by the built-in logging sink

The built-in sink does not log:

- Attribute names
- Attribute values
- Subject identifiers
- Resource identifiers
- Raw claims
- Role values
- Permission values
- Raw authorization requests
- Resource attributes
- Subject attributes
- Context attributes

These omissions reduce accidental disclosure, but the remaining fields may
still be sensitive.

Policy IDs, requirement IDs, failure codes, timing data, and decision outcomes
can reveal application behavior.

Protect diagnostics as security-relevant operational data.

## Custom diagnostics sink

Register a custom sink before calling `AddRuleGate()`:

```csharp
using Fotbiler.RuleGate.Abstractions.Diagnostics;

builder.Services.AddSingleton<
    IAuthorizationDiagnosticsSink,
    ApplicationAuthorizationDiagnosticsSink>();

builder.Services
    .AddRuleGate()
    .AddPolicies(policies);
```

Example implementation:

```csharp
using Fotbiler.RuleGate.Abstractions.Diagnostics;
using Microsoft.Extensions.Logging;

public sealed class
    ApplicationAuthorizationDiagnosticsSink
    : IAuthorizationDiagnosticsSink
{
    private readonly ILogger<
        ApplicationAuthorizationDiagnosticsSink>
        _logger;

    public ApplicationAuthorizationDiagnosticsSink(
        ILogger<
            ApplicationAuthorizationDiagnosticsSink>
            logger)
    {
        _logger = logger;
    }

    public bool IsEnabled =>
        _logger.IsEnabled(
            LogLevel.Information);

    public ValueTask WriteAsync(
        AuthorizationEvaluationDiagnostic diagnostic,
        CancellationToken cancellationToken = default)
    {
        cancellationToken
            .ThrowIfCancellationRequested();

        _logger.LogInformation(
            "Authorization {EvaluationId} completed. " +
            "Allowed: {IsAllowed}; DurationMs: {DurationMs}.",
            diagnostic.EvaluationId,
            diagnostic.IsAllowed,
            diagnostic.Duration.TotalMilliseconds);

        return ValueTask.CompletedTask;
    }
}
```

This example intentionally avoids logging policy IDs, requirement IDs,
failure codes, and attribute names.

Select fields according to the application's threat model and operational
requirements.

## Custom sink registration behavior

`AddLoggingDiagnostics()` uses `TryAddSingleton`.

Therefore:

```csharp
builder.Services.AddSingleton<
    IAuthorizationDiagnosticsSink,
    ApplicationAuthorizationDiagnosticsSink>();

builder.Services
    .AddRuleGate()
    .AddLoggingDiagnostics();
```

preserves `ApplicationAuthorizationDiagnosticsSink`.

It does not register both sinks.

RuleGate currently resolves one `IAuthorizationDiagnosticsSink`.

When output must be sent to multiple destinations, register an
application-owned composite sink that delegates to those destinations.

## Custom sink requirements

A custom sink is registered as a singleton and should be:

- Thread-safe
- Non-blocking where practical
- Bounded in memory use
- Resilient to temporary destination failures
- Careful with sensitive fields
- Safe under concurrent authorization traffic

Avoid performing slow network calls directly on the authorization path.

Prefer:

- Buffered channels
- Bounded queues
- Existing telemetry SDKs
- Efficient structured logging providers

A custom sink should monitor its own failures because exceptions escaping
`WriteAsync` are isolated by the authorization engine.

## Sink failure isolation

Diagnostics are best-effort.

After an authorization decision is produced, RuleGate attempts to write one
authorization diagnostic.

Exceptions thrown by the sink are caught and ignored by the engine.

A sink failure:

- Does not change an allowed decision into a denial
- Does not change a denied decision into an allowance
- Does not make authorization unavailable
- Is not rethrown to the application

This isolation applies to all exceptions escaping `WriteAsync`.

The custom sink should record or monitor its own delivery failures when such
visibility is required.

## Cancellation behavior

Request cancellation is honored during:

- Policy lookup
- Requirement dispatch
- Requirement evaluation

When cancellation occurs during evaluation, it is propagated normally.

Diagnostic publication happens only after RuleGate has produced a decision.

The engine calls the sink with:

```text
CancellationToken.None
```

This means request cancellation does not cancel the final best-effort
diagnostic write after the decision has been created.

The `IAuthorizationDiagnosticsSink.WriteAsync` contract still accepts a
cancellation token because sinks may be called directly or used by other
integrations.

The built-in logging sink checks the token supplied to it.

## Incomplete evaluations

Diagnostics describe completed authorization decisions that reach diagnostic
publication.

An unexpected exception during:

- Policy lookup
- Requirement evaluation
- Custom evaluator execution
- Core engine execution

may prevent a completed authorization diagnostic from being created.

Diagnostics must not be treated as a guaranteed audit record of every
attempted request.

Security audit trails should use a separate, application-owned mechanism with
appropriate durability guarantees.

## Diagnostics and audit logs

Diagnostics and audit logs serve different purposes.

Diagnostics answer questions such as:

- Which policy was selected?
- Which requirement failed?
- How long did evaluation take?
- Was the outcome indeterminate?
- What did the requirement tree look like?

Audit logs answer questions such as:

- Who attempted the protected action?
- Which business resource was affected?
- When did the business operation occur?
- Was the operation completed?
- What compliance record must be retained?

RuleGate diagnostics intentionally omit much of the subject and resource data
normally needed by an audit record.

Do not use diagnostics as the only compliance audit trail.

## Production logging guidance

A practical production baseline is:

- Information events enabled
- Debug requirement events disabled
- Restricted access to authorization logs
- Bounded retention
- Structured field indexing
- Alerting on unusual denial or indeterminate rates
- No raw claims or attribute values
- No public exposure of diagnostic payloads

Enable Debug traces temporarily when requirement-tree investigation is needed.

Return the category to Information after troubleshooting.

## Useful operational signals

Event `2000` can support metrics such as:

- Authorization evaluation count
- Allow rate
- Deny rate
- No-matching-policy rate
- Evaluation duration
- Failure-code frequency

Event `2001` can support investigations such as:

- Slow requirement categories
- Frequently failing requirement IDs
- Indeterminate requirement rate
- Deep or unexpectedly complex policy trees

RuleGate does not currently publish built-in metrics.

Metrics must be derived by the logging or custom diagnostics integration.

## Testing diagnostics

Test custom diagnostics behavior without asserting entire formatted log
messages.

Prefer asserting:

- Event ID
- Log level
- Structured field values
- Evaluation ID relationships
- Requirement outcomes
- Failure codes
- Omission of sensitive fields
- Sink failure isolation
- Disabled-sink behavior

Also test:

- Allowed decisions
- Denied decisions
- Missing policies
- Nested logical requirements
- Attribute requirements
- Indeterminate outcomes
- Concurrent evaluations

## OpenTelemetry signals

RuleGate also emits exporter-neutral activities and metrics through the public
`RuleGateTelemetry.ActivitySourceName` and `RuleGateTelemetry.MeterName`
constants. These signals cover authorization decisions, bounded failure
categories, latency, policy lookup, source loading, and atomic reload.

OpenTelemetry is independent from `IAuthorizationDiagnosticsSink`. A host may
enable structured logs, a custom diagnostic sink, telemetry, or any
combination. Built-in telemetry never records policy, requirement, source,
subject, resource, role, permission, claim, or attribute values.

See the
[telemetry, performance, and concurrency guide](telemetry-performance-concurrency.md)
for registration, instrument names, bounded dimensions, benchmarks, stress
tests, and thread-safety contracts.

## Current boundaries

The current diagnostics surface includes:

- Opt-in diagnostics
- Authorization-level diagnostics
- Requirement-level diagnostics
- Parent-child requirement identifiers
- Evaluation durations
- Failure codes
- Built-in ASP.NET Core structured logging
- Policy snapshot activation and reload-rejection logging
- Structured deterministic reload results and source diagnostics
- Custom singleton diagnostics sink
- Sink failure isolation
- Exporter-neutral OpenTelemetry authorization and reload activities
- Built-in low-cardinality counters and duration histograms
- Cancellation and unexpected-error telemetry

The current preview does not include:

- Persistent diagnostic storage
- Built-in multi-sink fan-out
- Decision visualization
- Durable compliance audit logging
- Guaranteed diagnostics for failed or canceled evaluations

See the [roadmap](roadmap.md) for the remaining release-hardening work.

## Next steps

Continue with:

- [ASP.NET Core integration](aspnetcore.md) for registration and HTTP usage.
- [Authorization model](authorization-model.md) for requirement concepts.
- [Manifest guide](manifests.md) for policy configuration.
- [Policy sources](policy-sources.md) for reload results and source failures.
- [Telemetry, performance, and concurrency](telemetry-performance-concurrency.md)
  for OpenTelemetry and thread-safety guidance.
- [Documentation index](README.md) for all available guides.
