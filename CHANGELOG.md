# Changelog

All notable changes to RuleGate are documented in this file.

The project follows Semantic Versioning. Preview releases may introduce breaking changes before the first stable release.

## [Unreleased]

## [0.7.0-preview.1] - 2026-07-29

### Added

- Added recurring `timeWindow` policies with explicit time zones, weekday
  schedules, overnight intervals, and half-open boundaries.
- Added `dateTimeWindow` policies for before, after, and bounded rules with
  UTC-normalized explicit-offset timestamps.
- Added `contextAge` policies for authentication age, MFA age, and
  reauthentication windows.
- Added canonical `context` policies for authentication method, request
  channel, network zone, tenant, organization, trusted-device state, and
  identity type.
- Added manifest validation, mapping, diagnostics, failure codes, dependency
  injection, and package-consumer support for the new requirements.

### Changed

- Updated `@fotbiler/rulegate-angular` to recognize backend-only attribute,
  time, and context requirement kinds while generating frontend identifiers.
- Published the independently versioned Angular package and synchronized
  NuGet family at `0.7.0-preview.1` for this coordinated release.

### Security

- Request-derived context remains untrusted and is never inferred from
  headers, IP addresses, arbitrary claims, or device assertions.
- Missing trusted context denies access; unsupported or incompatible values
  and future authentication timestamps fail closed as indeterminate.
- Time policies use the authorization evaluation time supplied through the
  registered `TimeProvider` and require explicit timezone or offset data.

### Verification

- Added contract, evaluator, manifest, diagnostics, integration, and packed
  NuGet consumer coverage across .NET 8, .NET 9, and .NET 10.
- Added Angular generator and packed npm consumer coverage for all current
  backend requirement kinds.
- Audited every repository and package README, Markdown formatting, and local
  documentation link.

## [0.6.0-preview.2] - 2026-07-29

### Added

- Added public subject, resource, context, and typed-literal operand contracts.
- Added attribute-to-attribute comparison requirements for ownership,
  organization scope, numeric, collection, and date/time policies.
- Added validated `attributeComparison` YAML manifest syntax with explicit
  left and right operands.

### Security

- Missing operands are denied as not satisfied; unsupported runtime values,
  incompatible operand kinds, and unsupported operator/type combinations are
  denied as indeterminate.
- Diagnostics identify operand structure without exposing resolved attribute
  or literal values. Built-in logging omits both attribute names.

### Verification

- Added public-contract, evaluator, manifest, diagnostics, and end-to-end
  authorization coverage across .NET 8, .NET 9, and .NET 10.
- Extended the package-only consumer to compile and evaluate ownership
  policies from packed NuGet artifacts.

## [0.6.0-preview.1] - 2026-07-29

### Added

- Added ordinal `contains`, `startsWith`, and `endsWith` string operators with
  explicit case-sensitive and case-insensitive behavior.
- Added collection `contains`, `containsAny`, `containsAll`, `in`, `notIn`,
  `intersects`, `isEmpty`, and `isNotEmpty` operators.
- Added `exists`, `notExists`, `isNull`, and `isNotNull` attribute-state
  operators with defined missing-versus-null semantics.
- Added typed manifest collections and `stringComparison` configuration.

### Security

- Attribute collections must be homogeneous, cannot contain null or nested
  collections, and are limited to 256 elements.
- Unsupported operators, invalid collection values, type mismatches, and
  exceeded collection limits fail closed.
- String comparison remains ordinal and case-sensitive unless
  `ordinalIgnoreCase` is explicitly selected.

### Verification

- Added unit and integration coverage for the new operators across .NET 8,
  .NET 9, and .NET 10.
- Extended package-only consumers to compile and evaluate advanced attribute
  policies from the packed NuGet artifacts.

## [0.5.0-preview.2] - 2026-07-29

### Changed

- Synchronized all six RuleGate NuGet packages at `0.5.0-preview.2`, including
  package-to-package dependency versions.
- Centralized the NuGet release version and made release verification reject
  package-specific version overrides.
- Updated Trusted Publishing to verify and publish the complete NuGet package
  family for every release, including packages without code changes.

### Documentation

- Standardized the product name as RuleGate across every NuGet package README.
- Aligned NuGet installation examples and package inventories with the shared
  release version.

## [0.5.0-preview.1] - 2026-07-28

### Added

- Added generic Angular role snapshots, requirements, route guards, and
  deterministic manifest-derived role constants.
- Added the optional `@fotbiler/rulegate-angular/keycloak` secondary entrypoint
  for lifecycle-safe `keycloak-js` session synchronization without a
  `keycloak-js` package dependency.
- Added the optional `Fotbiler.RuleGate.Keycloak` package for authenticated
  subject creation from realm roles, explicitly selected client roles, and
  explicit permission claims.
- Added shared UTF-8 role-normalization vectors and package-only Angular and
  .NET consumers.

### Security

- Keycloak integrations fail closed for unauthenticated or malformed identity
  input and do not own authentication, token storage, refresh, or Admin API
  access.
- Client roles are imported only for explicitly configured client IDs, and
  frontend projections remain user-experience controls rather than security
  boundaries.

### Documentation

- Added a Keycloak integration guide covering package independence, canonical
  role names, ASP.NET Core and Angular composition, and security boundaries.

## [0.4.0-preview.2] - 2026-07-28

### Added

- Added declarative Angular route authorization metadata with one shared
  fail-closed guard.
- Added application-defined denied-navigation handling for Angular guard
  results and redirects.
- Added `else` template composition to `RuleGateCanDirective` and a standalone
  disabled-state directive for native and custom interactive hosts.
- Added deterministic TypeScript identifier generation from `rulegate.yaml`,
  including atomic writes, collision diagnostics, and byte-exact `--check`
  mode.
- Expanded the package-only Angular consumer to exercise generated identifiers,
  declarative routes, denied redirects, template fallbacks, and disabled state.

### Security

- Missing or malformed route metadata denies without invoking application
  redirect behavior.
- TypeScript generation fails closed without replacing an existing output file
  when the manifest cannot be parsed or validated for generation.

### Documentation

- Expanded the Angular guide with declarative routes, denied-navigation
  handling, template composition, disabled-state behavior, and TypeScript
  generation.

## [0.4.0-preview.1] - 2026-07-28

### Added

- Published the first `@fotbiler/rulegate-angular` package for Angular 22.
- Added a signal-backed frontend authorization client, permission and policy
  route guards, and a standalone structural authorization directive.
- Added package-only npm tarball verification through a minimal Angular
  consumer application.
- Added npm release verification and a tokenless staged-publishing workflow for
  releases after the initial package bootstrap.

### Security

- Angular checks deny when state is uninitialized, cleared, malformed, or does
  not contain the exact case-sensitive identifier.
- Documented that browser-side guards and visibility controls never replace
  backend authorization.

### Documentation

- Added an Angular SDK guide covering state, guards, template visibility,
  generated constants, and the frontend trust boundary.
- Added a dedicated C# code-generation guide and aligned related CLI, manifest,
  authorization-model, and documentation-index references.
- Simplified published roadmap milestones and made the Angular SDK foundation
  the explicit next milestone.
- Added a scalable RuleGate wordmark and refreshed the root README badges and
  capability wording.
- Corrected the supported-preview table to match the latest-preview-only
  security-fix policy.
- Added the npm bootstrap, Trusted Publishing, staged approval, and public
  package verification procedure.

## [0.3.0-preview.2] - 2026-07-28

### Added

- Deterministic `rulegate generate csharp` command for manifest-derived policy,
  resource-type, and action constants.
- C# source output to standard output or atomically replaced UTF-8 files.
- Byte-exact `--check` mode for missing and stale generated output.
- Fail-closed namespace, identifier, empty-value, and identifier-collision
  diagnostics.
- Generated-code compilation and execution smoke coverage on .NET 8, .NET 9,
  and .NET 10.
- Normal CI and preview-release verification for packaged code generation.

### Security

- Invalid manifests and generation diagnostics produce no partial source.
- Existing output files remain unchanged when validation or generation fails.
- Stale-output checks never rewrite the inspected file.

### Documentation

- Added a dedicated RuleGate CLI guide covering installation, manifest
  validation, JSON output, stable exit codes, automation, and security
  behavior.
- Updated repository guides and package README sources for
  `0.3.0-preview.2` and the five-package release inventory.
- Replaced the original preview checklist with the current release-branch,
  Trusted Publishing, package verification, and documentation-gate workflow.
- Audited all repository Markdown documentation, linked the public package
  catalog directly to NuGet.org, updated the supported preview and project
  status, and removed stale CLI milestone references.

## [0.3.0-preview.1] - 2026-07-28

### Added

- Multi-targeted `Fotbiler.RuleGate.Cli` .NET tool for .NET 8, .NET 9, and .NET 10.
- `rulegate validate` with default `rulegate.yaml` discovery and explicit manifest paths.
- Human-readable and machine-readable JSON manifest validation output.
- Stable CLI exit codes for success, invalid manifests, usage errors, internal errors, and cancellation.
- Automatic root and command help, CLI version output, and safe runtime information through `rulegate info`.
- Package-level CLI installation and execution smoke tests across all supported runtimes.
- Five-package preview release verification and CLI publication support.

### Security

- CLI validation reuses the fail-closed manifest compiler and never returns partially compiled policies.
- Unexpected CLI failures do not expose exception details or stack traces.
- Machine-readable JSON output is isolated from standard error diagnostics.

## [0.2.0-preview.2] - 2026-07-27

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
- Opt-in HTTP authorization-result mapping through `AddHttpAuthorizationResultMapping`.
- Generic `application/problem+json` responses for RuleGate `401` challenge and `403` forbid results.
- Public RuleGate HTTP authorization problem type and problem code constants.
- Authentication challenge and forbid header preservation during RuleGate problem-response mapping.
- Standard ASP.NET Core policies remain delegated to the framework's default result handler.
- Minimal API and controller pipeline verification for RuleGate problem responses.
- Package-only consumer verification of the HTTP authorization-result mapping registration API.
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
- Public authorization and requirement diagnostics contracts.
- Opt-in `IAuthorizationDiagnosticsSink` integration with a disabled fast path.
- Nested requirement evaluation traces with parent-child identifiers, outcomes, failure codes, and durations.
- Policy-level diagnostics for successful, denied, and unmatched-policy decisions.
- ASP.NET Core structured logging diagnostics through `AddLoggingDiagnostics`.
- Package-only consumer verification of the logging diagnostics registration API.

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
- Default HTTP authorization problem responses do not expose RuleGate failure codes, requirement identifiers, policy details, claims, roles, permissions, subject identifiers, resource identifiers, or route values.
- Existing custom `IAuthorizationMiddlewareResultHandler` registrations are not replaced by the opt-in RuleGate mapping.
- Missing attributes fail as not satisfied.
- Attribute type mismatches, unsupported runtime types, and unsupported operator/type combinations fail as indeterminate.
- Attribute comparison is ordinal, case-sensitive, and does not perform implicit string or numeric coercion.
- Manifest `dateTimeOffset` values require an explicit UTC marker or numeric offset.
- The `nullValue` type token avoids ambiguity with YAML's native null scalar.
- Authorization diagnostics are disabled by default and do not contain attribute values.
- The built-in logging diagnostics sink omits attribute names, subject and resource identifiers, claims, role and permission values, and raw requests.
- Diagnostics sink failures are isolated and cannot alter authorization decisions.

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

[Unreleased]: https://github.com/fotbiler-lab/rulegate/compare/v0.7.0-preview.1...HEAD
[0.7.0-preview.1]: https://github.com/fotbiler-lab/rulegate/compare/v0.6.0-preview.2...v0.7.0-preview.1
[0.6.0-preview.2]: https://github.com/fotbiler-lab/rulegate/compare/v0.6.0-preview.1...v0.6.0-preview.2
[0.6.0-preview.1]: https://github.com/fotbiler-lab/rulegate/compare/v0.5.0-preview.2...v0.6.0-preview.1
[0.5.0-preview.2]: https://github.com/fotbiler-lab/rulegate/compare/v0.5.0-preview.1...v0.5.0-preview.2
[0.5.0-preview.1]: https://github.com/fotbiler-lab/rulegate/compare/v0.4.0-preview.2...v0.5.0-preview.1
[0.4.0-preview.2]: https://github.com/fotbiler-lab/rulegate/compare/v0.4.0-preview.1...v0.4.0-preview.2
[0.4.0-preview.1]: https://github.com/fotbiler-lab/rulegate/compare/v0.3.0-preview.2...v0.4.0-preview.1
[0.3.0-preview.2]: https://github.com/fotbiler-lab/rulegate/compare/v0.3.0-preview.1...v0.3.0-preview.2
[0.3.0-preview.1]: https://github.com/fotbiler-lab/rulegate/compare/v0.2.0-preview.2...v0.3.0-preview.1
[0.2.0-preview.2]: https://github.com/fotbiler-lab/rulegate/compare/v0.2.0-preview.1...v0.2.0-preview.2
[0.2.0-preview.1]: https://github.com/fotbiler-lab/rulegate/releases/tag/v0.2.0-preview.1
[0.1.0-preview.1]: https://github.com/fotbiler-lab/rulegate/releases/tag/v0.1.0-preview.1
