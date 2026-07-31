# RuleGate CLI

`Fotbiler.RuleGate.Cli` is the RuleGate command-line tool for deterministic
manifest validation and linting, policy testing, redacted decision
explanations, C# constant generation, stale-output checks, and CI automation.

## Install

```bash
dotnet tool install \
  --global \
  Fotbiler.RuleGate.Cli \
  --version 0.9.0-preview.1
```

The installed command is `rulegate`.

## Supported runtimes

- .NET 8
- .NET 9
- .NET 10

## Validate manifests

```bash
rulegate validate
rulegate validate ./policies/rulegate.yaml
rulegate validate --format json
```

## Test policy behavior

```bash
rulegate test
rulegate test ./policies/authorization.tests.yaml
rulegate test ./policies/authorization.tests.yaml --filter organization
rulegate test ./policies/authorization.tests.yaml --format json
```

Fixtures evaluate explicit authorization requests against a compiled manifest
without starting the host application. They support allow, deny, indeterminate,
and exact failure-code expectations with fixed evaluation times.

## Explain and lint

```bash
rulegate explain \
  ./policies/authorization.tests.yaml \
  --test organization-mismatch

rulegate lint ./policies/rulegate.yaml
rulegate lint ./policies/rulegate.yaml --format json
```

Explanation uses the runtime evaluator pipeline but omits subject/resource
identities and every request or literal value. Lint reports deterministic
structural findings and returns `1` when any finding exists.

Validation covers the complete manifest requirement model, including typed
attributes, attribute comparisons, explicit-time-zone schedules, bounded
date-time rules, authentication age, and canonical context policies.

## Generate C# constants

```bash
rulegate generate csharp \
  ./rulegate.yaml \
  --namespace Sample.Authorization
```

Write deterministic UTF-8 source to a file:

```bash
rulegate generate csharp \
  ./rulegate.yaml \
  --namespace Sample.Authorization \
  --output Generated/RuleGate.g.cs
```

The generated file contains:

- `RuleGatePolicies`
- `RuleGateResourceTypes`
- `RuleGateActions`

## Detect stale output

```bash
rulegate generate csharp \
  ./rulegate.yaml \
  --namespace Sample.Authorization \
  --output Generated/RuleGate.g.cs \
  --check
```

Check mode does not modify the file. Current output returns `0`; missing or
stale output returns `1`.

## Fail-closed generation

Generation runs only after complete manifest compilation. Invalid manifests,
invalid namespaces, empty values, and identifier collisions produce no source.
Existing output files are preserved when generation fails.

## Help, version, and information

```bash
rulegate --help
rulegate validate --help
rulegate test --help
rulegate explain --help
rulegate lint --help
rulegate generate csharp --help
rulegate --version
rulegate info
```

## Exit codes

|  Code | Meaning                                                                        |
| ----: | ------------------------------------------------------------------------------ |
|   `0` | Command completed successfully                                                 |
|   `1` | Input, expectation, lint finding, generation, missing-output, or stale failure |
|   `2` | Command-line usage error                                                       |
|   `3` | Unexpected internal error                                                      |
| `130` | Operation canceled                                                             |

## RuleGate packages

| Package                                                                                         | Purpose                                                                     |
| ----------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------- |
| [Fotbiler.RuleGate.Abstractions](https://www.nuget.org/packages/Fotbiler.RuleGate.Abstractions) | Public authorization contracts and extension abstractions                   |
| [Fotbiler.RuleGate.Core](https://www.nuget.org/packages/Fotbiler.RuleGate.Core)                 | Local fail-closed authorization engine and built-in evaluators              |
| [Fotbiler.RuleGate.Manifest](https://www.nuget.org/packages/Fotbiler.RuleGate.Manifest)         | YAML manifest loading, validation, and compilation                          |
| [Fotbiler.RuleGate.AspNetCore](https://www.nuget.org/packages/Fotbiler.RuleGate.AspNetCore)     | ASP.NET Core integration and attribute enrichment                           |
| [Fotbiler.RuleGate.Cli](https://www.nuget.org/packages/Fotbiler.RuleGate.Cli)                   | Validation, testing, explanation, linting, C# generation, and CI automation |
| [Fotbiler.RuleGate.Keycloak](https://www.nuget.org/packages/Fotbiler.RuleGate.Keycloak)         | Optional Keycloak claim normalization and subject mapping                   |

## Documentation

- [RuleGate CLI guide](https://github.com/fotbiler-lab/rulegate/blob/main/docs/cli.md)
- [Policy testing](https://github.com/fotbiler-lab/rulegate/blob/main/docs/policy-testing.md)
- [Explain and Lint](https://github.com/fotbiler-lab/rulegate/blob/main/docs/explain-and-lint.md)
- [Manifest guide](https://github.com/fotbiler-lab/rulegate/blob/main/docs/manifests.md)
- [Security model](https://github.com/fotbiler-lab/rulegate/blob/main/docs/security.md)
- [Roadmap](https://github.com/fotbiler-lab/rulegate/blob/main/docs/roadmap.md)
- [Documentation index](https://github.com/fotbiler-lab/rulegate/blob/main/docs/README.md)
