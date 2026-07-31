# RuleGate Abstractions

Public authorization contracts for the RuleGate ecosystem.

This package contains policy definitions, authorization requests and
decisions, typed attribute values, requirement evaluation abstractions, and
diagnostics contracts. Policy-source, reload-result, and snapshot metadata
contracts are also defined here. It does not contain the RuleGate authorization
engine.

RuleGate is currently in preview. Public APIs may change before the first
stable release.

## Installation

    dotnet add package Fotbiler.RuleGate.Abstractions --version 0.9.0-preview.2

## When to use this package

Reference Abstractions directly when you are:

- Building a reusable RuleGate integration
- Implementing a custom requirement evaluator
- Sharing authorization contracts across assemblies
- Depending on RuleGate contracts without the built-in engine
- Defining typed scalar, collection, presence, and null attribute requirements
- Defining subject, resource, context, and literal operand comparisons
- Defining explicit-time-zone, date-time, authentication-age, and canonical
  context requirements
- Creating diagnostic or policy-provider extensions
- Implementing an application-defined `IPolicySource`
- Coordinating reload through `IPolicyReloadService`

Most applications should install
[Fotbiler.RuleGate.AspNetCore](https://www.nuget.org/packages/Fotbiler.RuleGate.AspNetCore)
or
[Fotbiler.RuleGate.Core](https://www.nuget.org/packages/Fotbiler.RuleGate.Core)
instead of referencing Abstractions directly.

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
- [Security model](https://github.com/fotbiler-lab/rulegate/blob/main/docs/security.md)
- [RuleGate CLI](https://github.com/fotbiler-lab/rulegate/blob/main/docs/cli.md)
- [Policy testing](https://github.com/fotbiler-lab/rulegate/blob/main/docs/policy-testing.md)
- [Policy sources and atomic reload](https://github.com/fotbiler-lab/rulegate/blob/main/docs/policy-sources.md)
- [Documentation index](https://github.com/fotbiler-lab/rulegate/blob/main/docs/README.md)

## Security

RuleGate uses default-deny and fail-closed authorization semantics.
Applications remain responsible for deriving identity, resource, and context
information from trusted server-side sources.

Report suspected vulnerabilities through the
[private security reporting process](https://github.com/fotbiler-lab/rulegate/security/policy).

## License

RuleGate is licensed under the
[Apache License 2.0](https://github.com/fotbiler-lab/rulegate/blob/main/LICENSE).
