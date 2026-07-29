# RuleGate CLI

`Fotbiler.RuleGate.Cli` is the RuleGate command-line tool for deterministic
manifest validation, C# constant generation, stale-output checks, and CI
automation.

## Install

```bash
dotnet tool install \
  --global \
  Fotbiler.RuleGate.Cli \
  --version 0.7.0-preview.2
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
rulegate generate csharp --help
rulegate --version
rulegate info
```

## Exit codes

|  Code | Meaning                                                       |
| ----: | ------------------------------------------------------------- |
|   `0` | Command completed successfully                                |
|   `1` | Manifest, generation, missing-output, or stale-output failure |
|   `2` | Command-line usage error                                      |
|   `3` | Unexpected internal error                                     |
| `130` | Operation canceled                                            |

## RuleGate packages

| Package                                                                                         | Purpose                                                                                  |
| ----------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------- |
| [Fotbiler.RuleGate.Abstractions](https://www.nuget.org/packages/Fotbiler.RuleGate.Abstractions) | Public authorization contracts and extension abstractions                                |
| [Fotbiler.RuleGate.Core](https://www.nuget.org/packages/Fotbiler.RuleGate.Core)                 | Local fail-closed authorization engine and built-in evaluators                           |
| [Fotbiler.RuleGate.Manifest](https://www.nuget.org/packages/Fotbiler.RuleGate.Manifest)         | YAML manifest loading, validation, and compilation                                       |
| [Fotbiler.RuleGate.AspNetCore](https://www.nuget.org/packages/Fotbiler.RuleGate.AspNetCore)     | ASP.NET Core integration and attribute enrichment                                        |
| [Fotbiler.RuleGate.Cli](https://www.nuget.org/packages/Fotbiler.RuleGate.Cli)                   | Manifest validation, deterministic C# generation, stale-output checks, and CI automation |
| [Fotbiler.RuleGate.Keycloak](https://www.nuget.org/packages/Fotbiler.RuleGate.Keycloak)         | Optional Keycloak claim normalization and subject mapping                                |

## Documentation

- [RuleGate CLI guide](https://github.com/fotbiler-lab/rulegate/blob/main/docs/cli.md)
- [Manifest guide](https://github.com/fotbiler-lab/rulegate/blob/main/docs/manifests.md)
- [Security model](https://github.com/fotbiler-lab/rulegate/blob/main/docs/security.md)
- [Roadmap](https://github.com/fotbiler-lab/rulegate/blob/main/docs/roadmap.md)
- [Documentation index](https://github.com/fotbiler-lab/rulegate/blob/main/docs/README.md)
