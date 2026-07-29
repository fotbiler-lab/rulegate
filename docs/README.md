# RuleGate Documentation

Welcome to the RuleGate documentation.

RuleGate is a local-first and provider-independent authorization framework for
.NET and Angular applications. It supports permission, role, attribute,
contextual, and resource-based authorization through one composable policy
model.

The policy model includes explicit-time-zone schedules, bounded date-time
rules, authentication and MFA age, and canonical trusted request context.

## Start here

| Goal                                                                | Document                                                           |
| ------------------------------------------------------------------- | ------------------------------------------------------------------ |
| Make your first authorization decision                              | [Getting started](getting-started.md)                              |
| Understand subjects, resources, actions, policies, and requirements | [Authorization model](authorization-model.md)                      |
| Define and validate `rulegate.yaml` policies                        | [Manifest guide](manifests.md)                                     |
| Validate manifests locally or in CI                                 | [RuleGate CLI](cli.md)                                             |
| Generate C# constants and detect stale output                       | [C# code generation](code-generation.md)                           |
| Integrate RuleGate with ASP.NET Core                                | [ASP.NET Core integration](aspnetcore.md)                          |
| Supply trusted subject, resource, and context attributes            | [ASP.NET Core enrichment](enrichment.md)                           |
| Add permission, policy, and role checks to Angular                  | [Angular SDK](angular.md)                                          |
| Map Keycloak roles on ASP.NET Core and Angular                      | [Keycloak integration](keycloak.md)                                |
| Operate authorization diagnostics safely                            | [Diagnostics](diagnostics.md)                                      |
| Understand runtime and integration security                         | [Security model](security.md)                                      |
| Understand current and planned capabilities                         | [Roadmap](roadmap.md)                                              |
| Prepare and verify a NuGet preview                                  | [NuGet release checklist](releases/preview-release-checklist.md)   |
| Prepare and verify an npm preview                                   | [npm release checklist](releases/npm-preview-release-checklist.md) |

## Published packages

The latest published RuleGate NuGet preview is
[`0.7.0-preview.2`](https://github.com/fotbiler-lab/rulegate/releases/tag/v0.7.0-preview.2).

| Package                                                                                           | Purpose                                                                                           |
| ------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------- |
| [`Fotbiler.RuleGate.Abstractions`](https://www.nuget.org/packages/Fotbiler.RuleGate.Abstractions) | Public authorization contracts and extension abstractions                                         |
| [`Fotbiler.RuleGate.Core`](https://www.nuget.org/packages/Fotbiler.RuleGate.Core)                 | Local fail-closed authorization engine and built-in evaluators                                    |
| [`Fotbiler.RuleGate.Manifest`](https://www.nuget.org/packages/Fotbiler.RuleGate.Manifest)         | YAML manifest loading, validation, and compilation                                                |
| [`Fotbiler.RuleGate.AspNetCore`](https://www.nuget.org/packages/Fotbiler.RuleGate.AspNetCore)     | ASP.NET Core integration and trusted attribute enrichment                                         |
| [`Fotbiler.RuleGate.Cli`](https://www.nuget.org/packages/Fotbiler.RuleGate.Cli)                   | .NET tool for manifest validation, deterministic C# generation, stale-output checks, and CI usage |
| [`Fotbiler.RuleGate.Keycloak`](https://www.nuget.org/packages/Fotbiler.RuleGate.Keycloak)         | Optional Keycloak claim normalization and RuleGate subject mapping                                |
| [`@fotbiler/rulegate-angular`](https://www.npmjs.com/package/@fotbiler/rulegate-angular)          | Angular authorization client, route guards, UI directives, and TypeScript generation              |

All RuleGate NuGet packages are `0.7.0-preview.2`. The independently versioned
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
3. Use the [C# code-generation guide](code-generation.md) when application code
   should consume manifest identifiers as constants.
4. Follow the [ASP.NET Core integration](aspnetcore.md) guide to protect HTTP
   endpoints and map authenticated identities.
5. Add the [ASP.NET Core enrichment pipeline](enrichment.md) when trusted
   authorization attributes come from application services.
6. Use the [Angular SDK guide](angular.md) for route and template visibility
   after backend authorization is in place.
7. Follow the [Keycloak integration](keycloak.md) guide when Keycloak supplies
   the authenticated identity.
8. Use the [diagnostics guide](diagnostics.md) to configure logging and custom
   observability safely.
9. Review the [security model](security.md) before production integration.
10. Use the root [README](../README.md) for the repository overview and current
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
