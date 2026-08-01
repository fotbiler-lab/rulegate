# The RuleGate Guide

This guide teaches RuleGate as a complete authorization system, not as a list
of unrelated APIs. It begins with the authorization question, builds one
working application, and then extends that application through roles,
permissions, attributes, request context, resources, identity providers,
frontends, testing, diagnostics, reload, and production operations.

You do not need previous RBAC, ABAC, or CBAC experience. You should be
comfortable reading small C#, YAML, and—when using the frontend chapters—
TypeScript examples.

## What you will learn

By the end of the guide, you will be able to:

- explain the boundary between authentication and authorization;
- model role-, permission-, attribute-, context-, and resource-based rules;
- select and install the correct NuGet and npm packages;
- write, validate, test, explain, and lint `rulegate.yaml`;
- protect Minimal APIs, MVC controllers, and imperative service operations;
- load trusted subject, resource, and context data through providers;
- integrate any standards-based identity system and use the optional Keycloak
  helpers where appropriate;
- project backend grants into modern Angular, legacy Angular, or a
  framework-independent TypeScript application;
- load policies from files, embedded resources, configuration, memory, or an
  application-defined source and replace them atomically;
- diagnose decisions without exposing sensitive authorization data;
- test positive, negative, missing-data, and failure paths;
- extend RuleGate with custom factories, providers, requirements, evaluators,
  diagnostics sinks, clocks, and policy sources;
- deploy RuleGate with a fail-closed production posture.

## How to read this guide

For a first integration, read the chapters in order. Experienced readers can
use the package and capability indexes to jump directly to a recipe.

| Chapter                                                                   | Outcome                                                                                                  |
| ------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------- |
| [1. Authorization foundations](01-Authorization-Foundations.md)           | Understand authentication, authorization, RBAC, PBAC, ABAC, CBAC, resources, and fail-closed decisions   |
| [2. Packages and installation](02-Packages-and-Installation.md)           | Select the correct NuGet and npm packages for the host and framework version                             |
| [3. First protected API](03-First-Protected-API.md)                       | Run one policy through YAML, ASP.NET Core, authentication, and HTTP authorization                        |
| [4. Policy language](04-Policy-Language.md)                               | Express every built-in requirement and combine them safely                                               |
| [5. ASP.NET Core integration](05-ASP.NET-Core-Integration.md)             | Protect Minimal APIs, MVC endpoints, services, and custom resources                                      |
| [6. Trusted attributes and context](06-Trusted-Attributes-and-Context.md) | Implement subject, resource, and context providers with clear trust boundaries                           |
| [7. Identity and Keycloak](07-Identity-and-Keycloak.md)                   | Connect existing authentication while keeping policies provider-independent                              |
| [8. Frontend integration](08-Frontend-Integration.md)                     | Use the TypeScript client, Angular guards, directives, generators, and Keycloak adapter                  |
| [9. CLI and policy lifecycle](09-CLI-and-Policy-Lifecycle.md)             | Validate, generate, test, explain, and lint policies locally and in CI                                   |
| [10. Testing and diagnostics](10-Testing-and-Diagnostics.md)              | Prove allow and deny behavior and operate safe diagnostics and telemetry                                 |
| [11. Policy sources and reload](11-Policy-Sources-and-Reload.md)          | Choose a source and activate new policy snapshots atomically                                             |
| [12. Extensibility](12-Extensibility.md)                                  | Add application-specific mapping, requirements, providers, clocks, and sinks                             |
| [13. Real-world recipes](13-Real-World-Recipes.md)                        | Apply the model to ownership, tenancy, approvals, classification, MFA, schedules, and service identities |
| [14. Production checklist](14-Production-Checklist.md)                    | Review security, operations, troubleshooting, compatibility, and upgrades                                |
| [Glossary](Glossary.md)                                                   | Look up RuleGate and authorization terminology                                                           |

Every chapter ends with links to the next chapter and to the detailed reference
documents. The guide explains the journey; the references enumerate complete
contracts, operators, error codes, and security boundaries.

## Capability coverage

Use this table to locate a feature without reading the guide linearly.

| Capability                                                             | Teaching chapter                                           | Exhaustive reference                                                      |
| ---------------------------------------------------------------------- | ---------------------------------------------------------- | ------------------------------------------------------------------------- |
| Authentication vs authorization                                        | [Foundations](01-Authorization-Foundations.md)             | [Security](../security.md#authentication-boundary)                        |
| Roles and permissions                                                  | [Foundations](01-Authorization-Foundations.md)             | [Authorization model](../authorization-model.md#authorization-approaches) |
| Subject/resource/context attributes                                    | [Policy language](04-Policy-Language.md)                   | [Manifest reference](../manifests.md#attribute-requirements)              |
| String, number, date, collection, presence, null, and empty operators  | [Policy language](04-Policy-Language.md)                   | [Manifest operators](../manifests.md#attribute-operators)                 |
| Attribute-to-attribute comparison                                      | [Policy language](04-Policy-Language.md)                   | [Attribute comparison](../manifests.md#attribute-comparison-requirements) |
| `all`, `any`, and `not`                                                | [Policy language](04-Policy-Language.md)                   | [Logical requirements](../manifests.md#logical-requirements)              |
| Time, date-time, authentication age, and MFA age                       | [Policy language](04-Policy-Language.md)                   | [Time requirements](../authorization-model.md#time-and-date-time-windows) |
| Minimal API and MVC protection                                         | [ASP.NET Core](05-ASP.NET-Core-Integration.md)             | [ASP.NET Core reference](../aspnetcore.md)                                |
| Imperative and direct engine authorization                             | [ASP.NET Core](05-ASP.NET-Core-Integration.md)             | [Imperative authorization](../aspnetcore.md#imperative-authorization)     |
| Subject/resource/context providers                                     | [Trusted attributes](06-Trusted-Attributes-and-Context.md) | [Enrichment](../enrichment.md)                                            |
| Organization-specific application context                              | [Trusted attributes](06-Trusted-Attributes-and-Context.md) | [Context trust](../security.md#context-trust-boundary)                    |
| Generic identity-provider mapping                                      | [Identity](07-Identity-and-Keycloak.md)                    | [Subject mapping](../aspnetcore.md#map-a-claimsprincipal)                 |
| Keycloak realm/client roles                                            | [Identity and Keycloak](07-Identity-and-Keycloak.md)       | [Keycloak reference](../keycloak.md)                                      |
| Framework-independent frontend state                                   | [Frontend](08-Frontend-Integration.md)                     | [Frontend compatibility](../frontend-compatibility.md)                    |
| Modern and legacy Angular guards/directives                            | [Frontend](08-Frontend-Integration.md)                     | [Angular reference](../angular.md)                                        |
| C# and TypeScript generation                                           | [CLI lifecycle](09-CLI-and-Policy-Lifecycle.md)            | [C# generation](../code-generation.md)                                    |
| Policy validation, tests, explanation, and linting                     | [CLI lifecycle](09-CLI-and-Policy-Lifecycle.md)            | [CLI](../cli.md)                                                          |
| Safe diagnostics and OpenTelemetry                                     | [Testing and diagnostics](10-Testing-and-Diagnostics.md)   | [Diagnostics](../diagnostics.md)                                          |
| File, embedded, configuration, memory, and custom sources              | [Policy sources](11-Policy-Sources-and-Reload.md)          | [Policy sources reference](../policy-sources.md)                          |
| Atomic reload and last-valid snapshot                                  | [Policy sources](11-Policy-Sources-and-Reload.md)          | [Reload sequence](../policy-sources.md#atomic-activation-sequence)        |
| Factories, evaluators, sources, sinks, and clocks                      | [Extensibility](12-Extensibility.md)                       | Package-specific references linked in that chapter                        |
| Ownership, tenancy, approvals, confidential access, service identities | [Recipes](13-Real-World-Recipes.md)                        | [Reference applications](../reference-applications.md)                    |
| Production security and troubleshooting                                | [Production checklist](14-Production-Checklist.md)         | [Security model](../security.md)                                          |

## The running example

Most chapters use a document-approval system with these facts:

- a user is authenticated by an external identity provider;
- permissions describe capabilities such as `DOC.READ` and `DOC.APPROVE`;
- roles group responsibilities such as `DOCUMENT.APPROVER`;
- a user belongs to an organization and has a clearance level;
- a document has an owner, organization, status, and classification level;
- approval may require a trusted device, an internal network, fresh MFA, and a
  weekday business-hours window;
- the backend makes the authoritative decision;
- the frontend uses a limited grant projection only to shape the interface.

This example is deliberately richer than a single role check. Real systems
usually combine identity, business data, request context, and resource state.

## The one rule to remember

Frontend visibility is never the security boundary. Identity tokens,
JavaScript state, route guards, hidden buttons, and disabled controls do not
authorize a backend operation. The protected backend must evaluate RuleGate
against trusted data for every operation.

## Guide conventions

- Identifiers are exact, ordinal, and case-sensitive. `DOC.READ` and
  `doc.read` are different.
- Examples use stable `1.0.0` package APIs.
- YAML examples use schema version `1`.
- Missing policies, missing required data, invalid values, provider failures,
  evaluator failures, and unsupported input deny access.
- Placeholder authentication in the first sample is explicitly marked. Use a
  real validated authentication handler in production.
- Commands run from the repository or application root unless stated
  otherwise.

## Complete references

Use these after the teaching chapters when you need exhaustive detail:

- [Authorization model](../authorization-model.md)
- [Manifest reference](../manifests.md)
- [ASP.NET Core reference](../aspnetcore.md)
- [Attribute enrichment reference](../enrichment.md)
- [Angular reference](../angular.md)
- [CLI reference](../cli.md)
- [Diagnostics reference](../diagnostics.md)
- [Security model](../security.md)
- [Platform compatibility](../platform-compatibility.md)

---

Next: [Authorization foundations](01-Authorization-Foundations.md)
