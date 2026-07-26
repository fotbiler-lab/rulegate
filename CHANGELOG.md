# Changelog

All notable changes to Fotbiler RuleGate are documented in this file.

The project follows Semantic Versioning. Preview releases may introduce breaking changes before the first stable release.

## [Unreleased]

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

[Unreleased]: https://github.com/fotbiler-lab/rulegate/compare/v0.1.0-preview.1...HEAD
[0.1.0-preview.1]: https://github.com/fotbiler-lab/rulegate/releases/tag/v0.1.0-preview.1
