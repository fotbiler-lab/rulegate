# RuleGate Documentation

Welcome to the RuleGate documentation.

RuleGate is a local-first and provider-independent authorization framework for
.NET and Angular applications. It supports permission, role, attribute,
contextual, and resource-based authorization through one composable policy
model.

The policy model includes explicit-time-zone schedules, bounded date-time
rules, authentication and MFA age, and canonical trusted request context.

## Start here

| Goal                                                                | Document                                                                        |
| ------------------------------------------------------------------- | ------------------------------------------------------------------------------- |
| Make your first authorization decision                              | [Getting started](getting-started.md)                                           |
| Understand subjects, resources, actions, policies, and requirements | [Authorization model](authorization-model.md)                                   |
| Define and validate `rulegate.yaml` policies                        | [Manifest guide](manifests.md)                                                  |
| Validate manifests locally or in CI                                 | [RuleGate CLI](cli.md)                                                          |
| Test policy outcomes without starting an application                | [Policy testing](policy-testing.md)                                             |
| Explain decisions and lint policy structure safely                  | [Explain and Lint](explain-and-lint.md)                                         |
| Load and atomically replace local policy sources                    | [Policy sources](policy-sources.md)                                             |
| Export telemetry and verify performance or concurrency              | [Telemetry, performance, and concurrency](telemetry-performance-concurrency.md) |
| Generate C# constants and detect stale output                       | [C# code generation](code-generation.md)                                        |
| Integrate RuleGate with ASP.NET Core                                | [ASP.NET Core integration](aspnetcore.md)                                       |
| Supply trusted subject, resource, and context attributes            | [ASP.NET Core enrichment](enrichment.md)                                        |
| Add permission, policy, and role checks to Angular                  | [Angular SDK](angular.md)                                                       |
| Map Keycloak roles on ASP.NET Core and Angular                      | [Keycloak integration](keycloak.md)                                             |
| Run the official package-consuming samples                          | [Reference applications](reference-applications.md)                             |
| Operate authorization diagnostics safely                            | [Diagnostics](diagnostics.md)                                                   |
| Understand runtime and integration security                         | [Security model](security.md)                                                   |
| Understand current and planned capabilities                         | [Roadmap](roadmap.md)                                                           |
| Prepare and verify a NuGet preview                                  | [NuGet release checklist](releases/preview-release-checklist.md)                |
| Prepare and verify an npm preview                                   | [npm release checklist](releases/npm-preview-release-checklist.md)              |

## Published packages

The latest published RuleGate NuGet preview is
[`0.9.0-preview.2`](https://github.com/fotbiler-lab/rulegate/releases/tag/v0.9.0-preview.2).

| Package                                                                                           | Purpose                                                                                      |
| ------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------- |
| [`Fotbiler.RuleGate.Abstractions`](https://www.nuget.org/packages/Fotbiler.RuleGate.Abstractions) | Public authorization, policy-source, reload, telemetry-name, and extension contracts         |
| [`Fotbiler.RuleGate.Core`](https://www.nuget.org/packages/Fotbiler.RuleGate.Core)                 | Local fail-closed engine, built-in evaluators, immutable snapshots, and telemetry            |
| [`Fotbiler.RuleGate.Manifest`](https://www.nuget.org/packages/Fotbiler.RuleGate.Manifest)         | YAML loading, validation, compilation, file sources, and embedded-resource sources           |
| [`Fotbiler.RuleGate.AspNetCore`](https://www.nuget.org/packages/Fotbiler.RuleGate.AspNetCore)     | ASP.NET Core integration, configuration sources, atomic reload, and trusted enrichment       |
| [`Fotbiler.RuleGate.Cli`](https://www.nuget.org/packages/Fotbiler.RuleGate.Cli)                   | .NET tool for validation, testing, explanation, linting, deterministic C# generation, and CI |
| [`Fotbiler.RuleGate.Keycloak`](https://www.nuget.org/packages/Fotbiler.RuleGate.Keycloak)         | Optional Keycloak claim normalization and RuleGate subject mapping                           |
| [`@fotbiler/rulegate-angular`](https://www.npmjs.com/package/@fotbiler/rulegate-angular)          | Angular authorization client, route guards, UI directives, and TypeScript generation         |

All RuleGate NuGet packages are `0.9.0-preview.2`. The independently versioned
Angular npm package remains at `0.7.0-preview.1`.

## Recommended learning path

New users should begin with
[Getting started](getting-started.md).

That guide introduces the smallest complete RuleGate flow:

1. Install the packages.
2. Create a YAML policy manifest.
3. Compile and validate the manifest.
4. Register RuleGate.
5. Build an authorization request.
6. Evaluate an allowed decision.
7. Observe fail-closed denial behavior.

After completing that guide:

1. Read the [authorization model](authorization-model.md) to understand the
   concepts behind each decision.
2. Use the [manifest guide](manifests.md) to define and validate policies.
3. Add deterministic allow, deny, and indeterminate expectations with the
   [policy-testing guide](policy-testing.md).
4. Use [Explain and Lint](explain-and-lint.md) to inspect a decision safely and
   enforce maintainable policy structure in CI.
5. Use the [C# code-generation guide](code-generation.md) when application code
   should consume manifest identifiers as constants.
6. Use [Policy sources](policy-sources.md) to load local policies and preserve
   the last valid immutable snapshot during reload.
7. Use [Telemetry, performance, and concurrency](telemetry-performance-concurrency.md)
   to register OpenTelemetry signals and run benchmarks or stress checks.
8. Follow the [ASP.NET Core integration](aspnetcore.md) guide to protect HTTP
   endpoints and map authenticated identities.
9. Add the [ASP.NET Core enrichment pipeline](enrichment.md) when trusted
   authorization attributes come from application services.
10. Use the [Angular SDK guide](angular.md) for route and template visibility
    after backend authorization is in place.
11. Follow the [Keycloak integration](keycloak.md) guide when Keycloak supplies
    the authenticated identity.
12. Use the [diagnostics guide](diagnostics.md) to configure logging and custom
    observability safely.
13. Run the [reference applications](reference-applications.md) to see the
    packages composed in minimal and full-stack hosts.
14. Review the [security model](security.md) before production integration.
15. Use the root [README](../README.md) for the repository overview and current
    package status.

## Documentation principles

RuleGate documentation follows these principles:

- Examples must use public package APIs.
- Primary examples must represent tested behavior.
- Every guide must state its outcome and prerequisites.
- Security-relevant behavior must be explicit.
- Fail-closed outcomes must be documented alongside successful outcomes.
- Guides explain workflows; reference documents describe complete API or
  manifest surfaces.
- Information should have one authoritative location instead of being copied
  across multiple documents.

## Maintainer documentation

Documents under [`releases`](releases/) describe release preparation,
verification, publication, and post-release checks. They are intended for
project maintainers rather than package consumers.

## Command-line interface

- [RuleGate CLI](cli.md) — install or run the `rulegate` .NET tool,
  validate manifests, inspect tool information, and use the stable process
  exit-code contract.
- [Policy testing](policy-testing.md) — evaluate deterministic authorization
  fixtures, assert outcomes and failure codes, and filter CI test runs.
- [Explain and Lint](explain-and-lint.md) — produce redacted structural
  decision explanations and enforce deterministic manifest-quality findings.
- [C# code generation](code-generation.md) — generate deterministic constants,
  enforce stale-output checks in CI, and understand identifier diagnostics.

## Angular SDK

- [Angular SDK](angular.md) — supply frontend authorization state, protect
  routes, control template visibility, and preserve the backend security
  boundary.

## Identity-provider integrations

- [Keycloak integration](keycloak.md) — normalize effective realm and selected
  client roles into the same provider-independent RuleGate model on ASP.NET
  Core and Angular.
