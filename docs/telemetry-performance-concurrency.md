# Telemetry, Performance, and Concurrency

RuleGate `0.9.0-preview.3` adds exporter-neutral OpenTelemetry signals,
repeatable BenchmarkDotNet suites, bounded CI stress, and documented
thread-safety contracts.

RuleGate remains local-first. Telemetry is emitted from the in-process policy
engine and atomic policy provider; it does not send policy data or decisions to
a remote authorization service.

## Design

RuleGate instruments itself with the standard .NET `ActivitySource` and
`Meter` APIs. The library does not select or initialize an OpenTelemetry SDK,
collector, backend, sampler, or exporter.

This separation gives applications full control over:

- which signals are collected;
- sampling and aggregation;
- resource attributes such as service name and version;
- console, OTLP, or vendor-specific exporters;
- retention and access control.

Without a registered listener, activities are not created and metric
instruments behave as no-op producers.

## OpenTelemetry registration

Install the OpenTelemetry hosting package in the application that owns the
telemetry pipeline:

```bash
dotnet add package OpenTelemetry.Extensions.Hosting
```

Register the public RuleGate source and meter names:

```csharp
using Fotbiler.RuleGate.Abstractions.Diagnostics;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

builder.Services
    .AddOpenTelemetry()
    .WithTracing(tracing =>
        tracing.AddSource(
            RuleGateTelemetry.ActivitySourceName))
    .WithMetrics(metrics =>
        metrics.AddMeter(
            RuleGateTelemetry.MeterName));
```

Add exporters in the host application. RuleGate does not require an exporter
package and remains compatible with native `ActivityListener`, `MeterListener`,
and OpenTelemetry auto-instrumentation configuration.

## Activities

| Activity name                     | Scope                                     |
| --------------------------------- | ----------------------------------------- |
| `rulegate.authorization.evaluate` | One authorization-engine evaluation       |
| `rulegate.policy.reload`          | One atomic reload or lazy initialization  |
| `rulegate.policy.source.load`     | One source load within a candidate reload |

Activities use `ActivityKind.Internal` and automatically become children of
the current ASP.NET Core request activity when tracing is enabled.

### Activity attributes

Only bounded outcome attributes are emitted:

| Attribute                                 | Values                                                                               |
| ----------------------------------------- | ------------------------------------------------------------------------------------ |
| `rulegate.authorization.outcome`          | `allow`, `deny`, `cancelled`, `error`                                                |
| `rulegate.authorization.failure_category` | `none`, `no_matching_policy`, `not_satisfied`, `indeterminate`, `cancelled`, `error` |
| `rulegate.policy.matched`                 | `true`, `false`                                                                      |
| `rulegate.policy.reload.result`           | `activated`, `rejected`, `cancelled`, `error`, `coalesced`                           |
| `rulegate.policy.source.load.result`      | `success`, `rejected`, `invalid`, `cancelled`, `error`                               |

Denied authorization is an expected business result and does not mark the
activity as an OpenTelemetry error. Unexpected exceptions, cancellation,
rejected candidate snapshots, and failed source loads use error activity
status without recording exception messages or stack traces.

## Metrics

| Instrument                              | Type      | Unit           | Dimensions                                |
| --------------------------------------- | --------- | -------------- | ----------------------------------------- |
| `rulegate.authorization.evaluations`    | Counter   | `{evaluation}` | outcome, failure category, policy matched |
| `rulegate.authorization.duration`       | Histogram | `s`            | outcome, failure category, policy matched |
| `rulegate.policy.lookups`               | Counter   | `{lookup}`     | policy matched                            |
| `rulegate.policy.lookup.duration`       | Histogram | `s`            | policy matched                            |
| `rulegate.policy.reloads`               | Counter   | `{reload}`     | reload result                             |
| `rulegate.policy.reload.duration`       | Histogram | `s`            | reload result                             |
| `rulegate.policy.source.loads`          | Counter   | `{load}`       | source-load result                        |
| `rulegate.policy.source.load.duration`  | Histogram | `s`            | source-load result                        |
| `rulegate.policy.snapshot.policy_count` | Histogram | `{policy}`     | none                                      |

Durations use seconds, matching OpenTelemetry metric conventions. Policy
count is a measurement value rather than a dimension, so it cannot create a
new time series for every count.

## Privacy and cardinality boundary

RuleGate never adds the following values to built-in activities or metrics:

- subject or resource identifiers;
- resource types or actions;
- policy, requirement, or source identifiers;
- role or permission names;
- claim names or values;
- subject, resource, or context attribute names or values;
- raw failure or source-diagnostic codes;
- exception messages or stack traces.

The built-in tag value sets are closed and tested. This prevents normal policy
growth, tenant growth, and identity traffic from creating unbounded metric
series.

Host applications may enrich activities independently, but doing so changes
this privacy and cardinality guarantee. Treat any custom telemetry enrichment
as security-sensitive code.

## Benchmark suites

Benchmarks live in
`benchmarks/Fotbiler.RuleGate.Benchmarks` and are never included in RuleGate
NuGet packages.

`RequirementBenchmarks` covers:

- typed scalar attribute comparison;
- collection operators;
- attribute-to-attribute comparison;
- logical `all` composition;
- explicit-time-zone time windows;
- canonical trusted context.

`PolicyLookupBenchmarks` covers hit and miss behavior for immutable in-memory
and atomic snapshots at 10, 100, 1,000, and 10,000 policies.

Run the complete benchmark suite from a quiet machine:

```bash
dotnet run \
  --project benchmarks/Fotbiler.RuleGate.Benchmarks \
  --configuration Release
```

Filter a suite:

```bash
dotnet run \
  --project benchmarks/Fotbiler.RuleGate.Benchmarks \
  --configuration Release \
  -- \
  --filter '*PolicyLookupBenchmarks*'
```

Validate definitions quickly without treating the result as a performance
baseline:

```bash
./scripts/test-benchmarks.sh
```

The dry job executes one cold-start iteration. Use normal BenchmarkDotNet jobs
and stable hardware for comparisons or regression decisions.

## Concurrency verification

The normal test matrix verifies:

- lock-free readers observing only complete immutable snapshots;
- parallel authorization during repeated atomic replacement;
- serialized source loading and snapshot version progression;
- cancellation while waiting for the reload semaphore;
- preservation of the active snapshot after rejected or cancelled reloads;
- reuse of the reload service after cancellation;
- bounded and value-safe telemetry under success and failure.

The stress harness runs concurrent authorization, reload, and cancelled reload
operations for a configurable duration:

```bash
# Default: 60 seconds
./scripts/test-concurrency-stress.sh

# CI-sized verification
./scripts/test-concurrency-stress.sh 3

# Longer local run
./scripts/test-concurrency-stress.sh 300
```

CI and release verification run the bounded three-second variant. Longer runs
are intended for dedicated performance or soak-test environments.

## Thread-safety contracts

### Authorization engine

The built-in engine and requirement evaluators are safe for concurrent use.
Each evaluation owns its request, requirement context, activity, timers, and
optional diagnostic session.

Custom policy providers, requirement evaluators, and diagnostic sinks used by
a singleton engine must also be safe for concurrent calls.

### Immutable providers

`InMemoryPolicyProvider` builds a frozen route dictionary during construction.
After construction it performs lock-free reads and never mutates the policy
set.

`AtomicPolicyProvider` reads one immutable snapshot through `Volatile.Read`.
A lookup therefore observes either the complete previous snapshot or the
complete next snapshot, never a partially built candidate.

### Reload coordination

One semaphore serializes reloads per `AtomicPolicyProvider`. Sources are loaded
in deterministic name order, candidates are completely validated, and one
`Volatile.Write` activates the new snapshot.

Rejected, cancelled, and failed reloads do not replace the active snapshot.
Cancellation while waiting for the semaphore does not release a lock owned by
another call, and the provider remains reusable afterward.

Application-defined sources are invoked serially by one atomic provider. If a
source instance is shared with other providers or application code, the source
owner remains responsible for its wider thread safety.

### Diagnostics and telemetry

Built-in instruments are static and thread-safe. Telemetry listeners and
exporters run outside RuleGate's authorization trust boundary. Built-in
diagnostic-sink failures remain isolated and cannot change an authorization
decision.

## Operational guidance

- Aggregate on the bounded built-in dimensions only.
- Alert on error/cancelled reload trends and rejected candidates without
  attaching policy contents.
- Use authorization duration histograms to establish service-specific SLOs;
  RuleGate does not ship universal latency thresholds.
- Correlate authorization activities with ASP.NET Core request traces rather
  than duplicating user or resource identifiers.
- Run full benchmarks and longer stress tests on controlled hardware before a
  production rollout.

## Related documentation

- [Diagnostics](diagnostics.md)
- [Policy sources and atomic reload](policy-sources.md)
- [Security model](security.md)
- [ASP.NET Core integration](aspnetcore.md)
- [Roadmap](roadmap.md)
