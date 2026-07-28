# Fotbiler RuleGate CLI

`Fotbiler.RuleGate.Cli` is the RuleGate command-line tool for validating policy
manifests locally and in CI pipelines.

## Install

```bash
dotnet tool install \
  --global \
  Fotbiler.RuleGate.Cli \
  --version 0.3.0-preview.1
```

The installed command is:

```text
rulegate
```

## Supported runtimes

The tool package contains assets for:

- .NET 8
- .NET 9
- .NET 10

## Validate the default manifest

From a directory containing `rulegate.yaml`:

```bash
rulegate validate
```

## Validate an explicit manifest

```bash
rulegate validate ./policies/rulegate.yaml
```

## JSON output

```bash
rulegate validate \
  --format json
```

JSON output is written to standard output so automation can capture it.

## Help, version, and information

```bash
rulegate --help
rulegate validate --help
rulegate --version
rulegate info
```

## Exit codes

| Code | Meaning |
|---:|---|
| `0` | Command completed successfully |
| `1` | Manifest loading or validation failed |
| `2` | Command-line usage error |
| `3` | Unexpected internal error |
| `130` | Operation canceled |

Manifest validation reuses `Fotbiler.RuleGate.Manifest`. A failed load or
validation never produces a partial policy collection.

## RuleGate packages

| Package | Purpose |
|---|---|
| [Fotbiler.RuleGate.Abstractions](https://www.nuget.org/packages/Fotbiler.RuleGate.Abstractions) | Public authorization contracts and extension abstractions |
| [Fotbiler.RuleGate.Core](https://www.nuget.org/packages/Fotbiler.RuleGate.Core) | Local fail-closed authorization engine and built-in evaluators |
| [Fotbiler.RuleGate.Manifest](https://www.nuget.org/packages/Fotbiler.RuleGate.Manifest) | YAML manifest loading, validation, and compilation |
| [Fotbiler.RuleGate.AspNetCore](https://www.nuget.org/packages/Fotbiler.RuleGate.AspNetCore) | ASP.NET Core integration |
| [Fotbiler.RuleGate.Cli](https://www.nuget.org/packages/Fotbiler.RuleGate.Cli) | .NET tool for deterministic manifest validation and CI usage |

## Documentation

- [RuleGate documentation](https://github.com/fotbiler-lab/rulegate/tree/main/docs)
- [Manifest guide](https://github.com/fotbiler-lab/rulegate/blob/main/docs/manifests.md)
- [Security model](https://github.com/fotbiler-lab/rulegate/blob/main/docs/security.md)
- [Roadmap](https://github.com/fotbiler-lab/rulegate/blob/main/docs/roadmap.md)

## Documentation

- [RuleGate CLI](https://github.com/fotbiler-lab/rulegate/blob/main/docs/cli.md)
- [Documentation index](https://github.com/fotbiler-lab/rulegate/blob/main/docs/README.md)
