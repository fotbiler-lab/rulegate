# RuleGate Documentation

Welcome to the Fotbiler RuleGate documentation.

RuleGate is a local-first and provider-independent authorization framework for
.NET applications. It supports permission, role, attribute, contextual, and
resource-based authorization through one composable policy model.

## Start here

| Goal | Document |
|---|---|
| Make your first authorization decision | [Getting started](getting-started.md) |
| Understand subjects, resources, actions, policies, and requirements | [Authorization model](authorization-model.md) |
| Define and validate `rulegate.yaml` policies | [Manifest guide](manifests.md) |
| Validate manifests locally or in CI | [RuleGate CLI](cli.md) |
| Generate C# constants and detect stale output | [C# code generation](code-generation.md) |
| Integrate RuleGate with ASP.NET Core | [ASP.NET Core integration](aspnetcore.md) |
| Add permission and policy checks to Angular | [Angular SDK](angular.md) |
| Operate authorization diagnostics safely | [Diagnostics](diagnostics.md) |
| Understand runtime and integration security | [Security model](security.md) |
| Understand current and planned capabilities | [Roadmap](roadmap.md) |
| Prepare and verify a preview release | [Preview release checklist](releases/preview-release-checklist.md) |

## Published packages

The latest published preview is
[`0.3.0-preview.2`](https://github.com/fotbiler-lab/rulegate/releases/tag/v0.3.0-preview.2).

| Package | Purpose |
|---|---|
| [`Fotbiler.RuleGate.Abstractions`](https://www.nuget.org/packages/Fotbiler.RuleGate.Abstractions) | Public authorization contracts and extension abstractions |
| [`Fotbiler.RuleGate.Core`](https://www.nuget.org/packages/Fotbiler.RuleGate.Core) | Local fail-closed authorization engine and built-in evaluators |
| [`Fotbiler.RuleGate.Manifest`](https://www.nuget.org/packages/Fotbiler.RuleGate.Manifest) | YAML manifest loading, validation, and compilation |
| [`Fotbiler.RuleGate.AspNetCore`](https://www.nuget.org/packages/Fotbiler.RuleGate.AspNetCore) | ASP.NET Core integration |
| [`Fotbiler.RuleGate.Cli`](https://www.nuget.org/packages/Fotbiler.RuleGate.Cli) | .NET tool for manifest validation, deterministic C# generation, stale-output checks, and CI usage |

## Next npm package

`@fotbiler/rulegate-angular` is implemented for `0.4.0-preview.1` but is not
yet published to npm. See the [Angular SDK guide](angular.md) for its public API
and security boundary.

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
5. Use the [Angular SDK guide](angular.md) for route and template visibility
   after backend authorization is in place.
6. Use the [diagnostics guide](diagnostics.md) to configure logging and custom
   observability safely.
7. Review the [security model](security.md) before production integration.
8. Use the root [README](../README.md) for the repository overview and current
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
