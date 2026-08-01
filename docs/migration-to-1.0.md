# Migrating to RuleGate 1.0

This guide describes the intentional compatibility changes when moving from
the `0.9.0-preview.4` package family to the RuleGate 1.0 release line.

RuleGate preview APIs were explicitly allowed to change before the first
stable release. The 1.0 release candidate uses this final opportunity to
remove dependency contracts that were not compatible with the complete
declared .NET target matrix.

## Evaluation clock migration

The ASP.NET Core integration no longer uses `System.TimeProvider` as its
public evaluation-clock contract.

RuleGate 1.0 uses the RuleGate-owned `IRuleGateClock` abstraction:

    using Fotbiler.RuleGate.AspNetCore.Time;

    public sealed class ApplicationRuleGateClock
        : IRuleGateClock
    {
        public DateTimeOffset GetUtcNow()
        {
            return DateTimeOffset.UtcNow;
        }
    }

Applications that do not customize evaluation time require no additional
registration. `AddRuleGate()` registers a system-backed singleton clock.

### Before

With `0.9.0-preview.4`, an application could replace the evaluation clock by
registering `TimeProvider` before RuleGate:

    builder.Services.AddSingleton<TimeProvider>(
        applicationTimeProvider);

    builder.Services.AddRuleGate();

### RuleGate 1.0

Register `IRuleGateClock` instead:

    using Fotbiler.RuleGate.AspNetCore.Time;

    builder.Services.AddSingleton<IRuleGateClock>(
        applicationRuleGateClock);

    builder.Services.AddRuleGate();

`AddRuleGate()` uses `TryAddSingleton`, so an application-provided
`IRuleGateClock` remains authoritative.

Tests that need deterministic schedule or date-time evaluation should register
a controlled `IRuleGateClock` implementation.

## Direct handler construction

Most applications resolve RuleGate services through dependency injection and
do not construct `RuleGateAuthorizationHandler` directly.

Code that does construct the handler must replace the old `TimeProvider`
constructor argument with an `IRuleGateClock`:

    var handler =
        new RuleGateAuthorizationHandler(
            authorizationEngine,
            subjectFactory,
            resourceFactory,
            ruleGateClock,
            requestEnricher);

This constructor transition is the intentional binary API break between the
`0.9.0-preview.4` ASP.NET Core package and the 1.0 release line.

Package validation contains an exact compatibility suppression only for this
preview-to-1.0 constructor change. Other binary API breaks remain validation
failures.

## Time semantics are unchanged

The authorization model still evaluates temporal requirements through
`AuthorizationContext.EvaluationTime`.

The change affects how the ASP.NET Core integration obtains that trusted
timestamp, not the meaning of:

- `timeWindow`
- `dateTimeWindow`
- authentication-age requirements
- MFA-age requirements

The default clock uses system UTC time. Custom clocks must return trusted
`DateTimeOffset` values and must not derive authorization time from
caller-controlled input.

## Legacy .NET targets

The declared ASP.NET Core and Keycloak target matrix remains unchanged:

- .NET Core 3.1
- .NET 5
- .NET 6
- .NET 7
- .NET 8
- .NET 9
- .NET 10

The 1.0 hardening work removes the `Microsoft.Bcl.TimeProvider` dependency
from the ASP.NET Core package and selects dependency versions that can be
restored and compiled across the declared legacy matrix without package
TFM-support warning suppression.

These legacy runtimes remain end-of-life. Compatibility verification does not
restore vendor security support. See
[Platform compatibility](platform-compatibility.md).

## Migration checklist

Before moving an application to the 1.0 release line:

1. Search application code for `TimeProvider` registrations used specifically
   to control RuleGate evaluation time.
2. Replace those registrations with `IRuleGateClock`.
3. Update direct `RuleGateAuthorizationHandler` construction, if any.
4. Keep custom clocks server-controlled and concurrency-safe.
5. Re-run schedule, date-time, authentication-age, and MFA-age boundary tests.
6. Verify the application on its actual target framework using the packed
   RuleGate packages.

Applications that never customized RuleGate evaluation time normally require
no clock-related code change.
