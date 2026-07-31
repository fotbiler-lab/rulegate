# RuleGate Core

Local, provider-independent authorization engine for RuleGate.

Core contains the built-in policy engine, role and permission evaluators,
logical requirements, typed attribute evaluators, requirement dispatch,
in-memory policy storage, and opt-in authorization diagnostics.

RuleGate is currently in preview. Public APIs may change before the first
stable release.

## Installation

    dotnet add package Fotbiler.RuleGate.Core --version 0.8.0-preview.2

## When to use this package

Use Core when you need:

- In-process authorization without a remote policy service
- A framework-independent authorization engine
- Built-in role, permission, logical, scalar, collection, presence, and null
  attribute requirements
- Attribute-to-attribute comparisons for ownership and organization scope
- Recurring time-window, bounded date-time, authentication-age, and canonical
  context policies
- Custom policy-provider or evaluator composition
- Direct control over authorization requests and decisions

ASP.NET Core applications should normally install
[Fotbiler.RuleGate.AspNetCore](https://www.nuget.org/packages/Fotbiler.RuleGate.AspNetCore),
which references Core and adds framework integration.

## Evaluation behavior

RuleGate grants access only when:

1. A matching policy exists.
2. The complete requirement tree evaluates successfully.
3. No requirement is denied, unsupported, malformed, or indeterminate.

RuleGate follows default-deny behavior: missing policies and incomplete
authorization inputs cannot grant access.

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

- [Getting started](https://github.com/fotbiler-lab/rulegate/blob/main/docs/getting-started.md)
- [Authorization model](https://github.com/fotbiler-lab/rulegate/blob/main/docs/authorization-model.md)
- [Diagnostics](https://github.com/fotbiler-lab/rulegate/blob/main/docs/diagnostics.md)
- [Security model](https://github.com/fotbiler-lab/rulegate/blob/main/docs/security.md)
- [RuleGate CLI](https://github.com/fotbiler-lab/rulegate/blob/main/docs/cli.md)
- [Policy testing](https://github.com/fotbiler-lab/rulegate/blob/main/docs/policy-testing.md)
- [Documentation index](https://github.com/fotbiler-lab/rulegate/blob/main/docs/README.md)

## Security

RuleGate uses ordinal, case-sensitive matching by default and requires an
explicit opt-in for ordinal case-insensitive string operations. Invalid types,
unsupported operators, and collection-limit violations fail closed. Custom
evaluators and policy providers must preserve these security boundaries.

Report suspected vulnerabilities through the
[private security reporting process](https://github.com/fotbiler-lab/rulegate/security/policy).

## License

RuleGate is licensed under the
[Apache License 2.0](https://github.com/fotbiler-lab/rulegate/blob/main/LICENSE).
