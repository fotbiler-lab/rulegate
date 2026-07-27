# Changelog

All notable changes to Fotbiler RuleGate are documented in this file.

The project follows Semantic Versioning. Preview releases may introduce breaking changes before the first stable release.

## [Unreleased]

### Added

- Dynamic ASP.NET Core authorization policy resolution through `RuleGateAuthorizationPolicyProvider`.
- Structured policy names using `RuleGate:<resource-type>:<action>`.
- Public `RuleGatePolicyName` construction, formatting, and parsing.
- `AuthorizeRuleGateAsync` extensions for concise `IAuthorizationService` integration.
- Automatic policy-name construction from `AuthorizationResource.Type` and an action.
- Explicit resource-type authorization overload for applications using custom domain-resource mapping.
- Minimal API authorization through `RequireRuleGate`.
- Controller and action authorization through `RuleGateAuthorizeAttribute`.
- Endpoint metadata carrying resource type, action, and an optional resource-ID route-value name.
- Default `HttpContext` resource mapping into `AuthorizationResource`.
- Backward-compatible requirement-aware authorization-resource factory extension.
- Live HTTP pipeline tests for Minimal API and controller authorization.
- Standard ASP.NET Core challenge and forbid behavior verification.
- Typed scalar authorization attribute values with numeric normalization.
- Subject, resource, and context attribute requirement definitions.
- Equality, inequality, numeric ordering, and `DateTimeOffset` ordering operators.
- Built-in fail-closed attribute requirement evaluator and default dependency-injection registration.
- Attribute policy verification through the package-only consumer.
- YAML manifest syntax for subject, resource, and context attribute requirements.
- Explicit manifest attribute source, operator, value type, and value validation.
- Manifest conversion of boolean, decimal, null, string, and `DateTimeOffset` values.
- Nested attribute requirements inside `all`, `any`, and `not` logical requirements.
- End-to-end YAML compilation and authorization-engine verification for attribute policies.

- Multi-targeted package assets for .NET 8, .NET 9, and .NET 10.
- Full test-suite execution across all supported target frameworks.
- Package-only consumer verification on .NET 8, .NET 9, and .NET 10.
- Resource type and action propagation into `RuleGateAuthorizationRequirement`.
- Fallback delegation for standard ASP.NET Core named policies.
- Default and fallback authorization-policy preservation.
- Dynamic-policy caching support.
- Dependency injection registration of the RuleGate policy provider.
- Package-consumer verification without manual `AuthorizationOptions.AddPolicy` registration.
- Resource-type mismatch verification in the authorization handler and package consumer.

### Security

- Malformed policy names owned by the `RuleGate:` prefix do not fall back to ordinary ASP.NET Core policies.
- Dynamic policies require an authenticated principal.
- Policy-name parsing uses ordinal and case-sensitive matching.
- A policy/resource type mismatch fails closed before the RuleGate authorization engine is evaluated.
- Missing endpoints and missing or empty required route values fail closed.
- Conflicting endpoint metadata for the same RuleGate policy fails closed.
- Endpoint authorization does not evaluate the RuleGate engine until a valid resource has been constructed.
- Missing attributes fail as not satisfied.
- Attribute type mismatches, unsupported runtime types, and unsupported operator/type combinations fail as indeterminate.
- Attribute comparison is ordinal, case-sensitive, and does not perform implicit string or numeric coercion.
- Manifest `dateTimeOffset` values require an explicit UTC marker or numeric offset.
- The `nullValue` type token avoids ambiguity with YAML's native null scalar.


## [0.2.0-preview.1] - 2026-07-26

### Added

- `Fotbiler.RuleGate.AspNetCore` package.
- ASP.NET Core dependency injection registration through `AddRuleGate`.
- `RuleGateBuilder` for fluent RuleGate configuration.
- Policy registration through `AddPolicy` and `AddPolicies`.
- Custom requirement evaluator registration through `AddRequirementEvaluator`.
- Default singleton registrations for the policy engine, policy provider, requirement dispatcher, and built-in evaluators.
- ASP.NET Core dependency injection tests.
- Package-only consumer verification using the ASP.NET Core integration package.
- Four-package release verification and Trusted Publishing support.
- `ClaimsPrincipal` to `AuthorizationSubject` mapping.
- Configurable subject identifier, role, and permission claim types.
- Fail-closed handling for missing or ambiguous subject identifiers.
- Ordinal, case-sensitive claim mapping with duplicate removal.
- Claims principal mapping tests and package-consumer verification.
- ASP.NET Core `IAuthorizationRequirement` and authorization handler foundation.
- Resource-based RuleGate authorization through `IAuthorizationService`.
- Replaceable ASP.NET Core authorization-resource factory.
- Fail-closed subject and resource mapping behavior.
- Testable evaluation timestamps through `TimeProvider`.
- Package-consumer verification of allowed and denied handler flows.

### Known limitations

- The preview targets .NET 10.
- Dynamic authorization policy providers and policy-name parsing are not included.
- Authorization attributes, endpoint helpers, and automatic HTTP result mapping are not included.
- The default ASP.NET Core resource factory accepts only `AuthorizationResource` instances.
- Subject, resource, and context attribute extraction is not included.
- Advanced ABAC and CBAC evaluators are not included.
- CLI, Angular, and provider-specific identity integrations are not included.
- Public APIs may change before the first stable release.

## [0.1.0-preview.1] - 2026-07-26

### Added

- Preview release verification script.
- Preview release checklist and manual publishing procedure.

- Authorization subjects, resources, contexts, requests, decisions, and failures.
- Permission-based and role-based authorization requirements.
- Logical `all`, `any`, and `not` requirements.
- Default-deny policy authorization engine.
- Fail-closed requirement evaluation.
- Requirement evaluation dispatcher and built-in evaluators.
- Immutable in-memory policy provider.
- Ordinal and case-sensitive policy route matching.
- YAML manifest models and schema validation.
- YAML loading with duplicate-key and recursion protection.
- Structured manifest loading and validation errors.
- Manifest-to-domain policy mapping.
- Text-based and file-based manifest compilation.
- End-to-end manifest authorization integration tests.
- Local NuGet package consumer smoke test.
- NuGet package metadata and symbol package generation.
- GitHub Actions build, format, test, package, and consumer-smoke workflow.
- Packages:
  - `Fotbiler.RuleGate.Abstractions`
  - `Fotbiler.RuleGate.Core`
  - `Fotbiler.RuleGate.Manifest`

### Security

- Authorization defaults to deny when no policy matches.
- Unsupported requirement types produce indeterminate results and are denied.
- Requirement evaluation failures never produce an allowed decision.
- Policy identifiers, resource types, actions, roles, and permissions use ordinal case-sensitive matching.
- Policy manifests do not execute arbitrary scripts.

### Known limitations

- The preview targets .NET 10.
- ASP.NET Core dependency injection integration is not included.
- Attribute-based, context-based, and resource-based evaluators are not yet included.
- CLI, Angular, and Keycloak integration packages are not yet included.
- Public APIs may change before the first stable release.

[Unreleased]: https://github.com/fotbiler-lab/rulegate/compare/v0.2.0-preview.1...HEAD
[0.2.0-preview.1]: https://github.com/fotbiler-lab/rulegate/releases/tag/v0.2.0-preview.1
[0.1.0-preview.1]: https://github.com/fotbiler-lab/rulegate/releases/tag/v0.1.0-preview.1
