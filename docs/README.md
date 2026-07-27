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
| Integrate RuleGate with ASP.NET Core | [ASP.NET Core integration](aspnetcore.md) |
| Operate authorization diagnostics safely | [Diagnostics](diagnostics.md) |
| Understand runtime and integration security | [Security model](security.md) |
| Understand current and planned capabilities | [Roadmap](roadmap.md) |
| Prepare and verify a preview release | [Preview release checklist](releases/preview-release-checklist.md) |

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
3. Follow the [ASP.NET Core integration](aspnetcore.md) guide to protect HTTP
   endpoints and map authenticated identities.
4. Use the [diagnostics guide](diagnostics.md) to configure logging and custom
   observability safely.
5. Review the [security model](security.md) before production integration.
6. Use the root [README](../README.md) for the repository overview and current
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
