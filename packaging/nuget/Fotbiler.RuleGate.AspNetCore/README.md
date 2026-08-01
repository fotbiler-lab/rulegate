# RuleGate ASP.NET Core

ASP.NET Core integration for the RuleGate authorization framework.

This package provides dependency injection, configurable claims mapping,
dynamic policies, Minimal API endpoint helpers, controller and action
attributes, imperative authorization extensions, resource mapping, structured
diagnostics, ordered subject/resource/context attribute enrichment, local
policy sources, atomic reload hosting, and opt-in safe HTTP authorization
results. Exporter-neutral RuleGate activities automatically correlate with the
current ASP.NET Core request activity when the host registers the public
RuleGate activity source and meter.
The default handler supplies deterministic evaluation time through the
registered `IRuleGateClock`; trusted context values remain application-owned.

RuleGate 1.0 is stable. Public API compatibility follows Semantic Versioning.

## Installation

    dotnet add package Fotbiler.RuleGate.AspNetCore --version 1.0.0

## Compatibility

This package targets .NET Core 3.1 and every .NET release
from 5 through 10. .NET Core 3.1 and .NET 5–7 are end-of-life and receive
compatibility verification only; they do not receive security support from
RuleGate or Microsoft.

## Register RuleGate

    using Fotbiler.RuleGate.AspNetCore.DependencyInjection;

    builder.Services
        .AddRuleGate()
        .AddYamlPolicyFile(
            "rulegate.yaml",
            options => options.ReloadOnChange = true);

YAML file, embedded-resource, structured configuration, in-memory, and
application-defined sources are supported. A candidate is activated only after
complete validation; failed reloads preserve the last valid snapshot.

The application must configure ASP.NET Core authentication and authorization
using its trusted identity provider and claim model.

## Customize evaluation time

`AddRuleGate()` registers a system-backed `IRuleGateClock` by default.
Applications that need a controlled trusted clock can register their own
implementation before RuleGate:

    using Fotbiler.RuleGate.AspNetCore.Time;

    builder.Services.AddSingleton<IRuleGateClock>(
        applicationRuleGateClock);

    builder.Services.AddRuleGate();

RuleGate preserves the application registration. Custom production clocks must
not derive authorization time from caller-controlled input.

## Add trusted attribute providers

    using Fotbiler.RuleGate.AspNetCore.Enrichment;

    builder.Services
        .AddRuleGate()
        .AddSubjectAttributeProvider<TenantAttributeProvider>()
        .AddResourceAttributeProvider<DocumentAttributeProvider>()
        .AddContextAttributeProvider<RequestContextAttributeProvider>()
        .AddPolicies(compilation.Policies);

Providers are scoped by default. They run sequentially in subject, resource,
and context stages. Missing required data, provider exceptions, cancellation,
unsupported values, and default attribute collisions fail closed before the
authorization engine runs.

## Protect a Minimal API endpoint

    using Fotbiler.RuleGate.AspNetCore.Endpoints;

    app.MapGet(
            "/documents/{id}",
            (string id) =>
            {
                return Results.Ok(
                    new
                    {
                        id,
                    });
            })
        .RequireRuleGate(
            resourceType: "document",
            action: "read",
            resourceIdRouteValue: "id");

Dynamic policy names use this form:

    RuleGate:<resource-type>:<action>

## RuleGate packages

| Package                                                                                         | Purpose                                                        |
| ----------------------------------------------------------------------------------------------- | -------------------------------------------------------------- |
| [Fotbiler.RuleGate.Abstractions](https://www.nuget.org/packages/Fotbiler.RuleGate.Abstractions) | Public authorization contracts and extension abstractions      |
| [Fotbiler.RuleGate.Core](https://www.nuget.org/packages/Fotbiler.RuleGate.Core)                 | Local fail-closed authorization engine and built-in evaluators |
| [Fotbiler.RuleGate.Manifest](https://www.nuget.org/packages/Fotbiler.RuleGate.Manifest)         | YAML manifest loading, validation, and compilation             |
| [Fotbiler.RuleGate.AspNetCore](https://www.nuget.org/packages/Fotbiler.RuleGate.AspNetCore)     | ASP.NET Core integration and attribute enrichment              |
| [Fotbiler.RuleGate.Cli](https://www.nuget.org/packages/Fotbiler.RuleGate.Cli)                   | Manifest validation, policy testing, generation, and CI usage  |
| [Fotbiler.RuleGate.Keycloak](https://www.nuget.org/packages/Fotbiler.RuleGate.Keycloak)         | Optional Keycloak claim normalization and subject mapping      |

## Documentation

- [ASP.NET Core integration](https://github.com/fotbiler-lab/rulegate/blob/main/docs/aspnetcore.md)
- [Migrating to RuleGate 1.0](https://github.com/fotbiler-lab/rulegate/blob/main/docs/migration-to-1.0.md)
- [ASP.NET Core enrichment](https://github.com/fotbiler-lab/rulegate/blob/main/docs/enrichment.md)
- [Getting started](https://github.com/fotbiler-lab/rulegate/blob/main/docs/getting-started.md)
- [Diagnostics](https://github.com/fotbiler-lab/rulegate/blob/main/docs/diagnostics.md)
- [Security model](https://github.com/fotbiler-lab/rulegate/blob/main/docs/security.md)
- [RuleGate CLI](https://github.com/fotbiler-lab/rulegate/blob/main/docs/cli.md)
- [Policy testing](https://github.com/fotbiler-lab/rulegate/blob/main/docs/policy-testing.md)
- [Policy sources and atomic reload](https://github.com/fotbiler-lab/rulegate/blob/main/docs/policy-sources.md)
- [Telemetry, performance, and concurrency](https://github.com/fotbiler-lab/rulegate/blob/main/docs/telemetry-performance-concurrency.md)
- [Documentation index](https://github.com/fotbiler-lab/rulegate/blob/main/docs/README.md)
- [Minimal ASP.NET Core reference](https://github.com/fotbiler-lab/rulegate/tree/main/samples/aspnetcore-minimal)
- [Full-stack document approval reference](https://github.com/fotbiler-lab/rulegate/tree/main/samples/document-approval)

## Security

Authentication establishes identity; RuleGate evaluates authorization. Claims,
resource identifiers, and context attributes must be mapped from trusted
server-side sources.

Report suspected vulnerabilities through the
[private security reporting process](https://github.com/fotbiler-lab/rulegate/security/policy).

## License

RuleGate is licensed under the
[MIT License](https://github.com/fotbiler-lab/rulegate/blob/main/LICENSE).
