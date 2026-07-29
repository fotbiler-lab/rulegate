# RuleGate Manifest

YAML manifest loading, validation, and policy compilation for RuleGate.

Manifest converts `rulegate.yaml` documents into immutable RuleGate policy
definitions. Loading and validation failures are structured, and failed
compilation does not return a partial policy collection.

RuleGate is currently in preview. Public APIs and the manifest schema may
change before the first stable release.

## Installation

    dotnet add package Fotbiler.RuleGate.Manifest --version 0.6.0-preview.1

## Example manifest

    schemaVersion: 1

    application:
      id: sample-application
      name: Sample Application

    policies:
      - id: sample-resource-read
        resourceType: sample-resource
        action: read
        requirement:
          all:
            - permission: sample.read
            - role: sample.editor
            - attribute:
                source: subject
                name: departments
                operator: contains
                stringComparison: ordinalIgnoreCase
                valueType: string
                value: finance

The compiled policy collection can be registered with the RuleGate engine or
the ASP.NET Core integration.

The manifest supports typed scalar and collection literals, ordinal string
comparison, and value-less presence, null, and collection-state operators.

## RuleGate packages

| Package                                                                                         | Purpose                                                        |
| ----------------------------------------------------------------------------------------------- | -------------------------------------------------------------- |
| [Fotbiler.RuleGate.Abstractions](https://www.nuget.org/packages/Fotbiler.RuleGate.Abstractions) | Public authorization contracts and extension abstractions      |
| [Fotbiler.RuleGate.Core](https://www.nuget.org/packages/Fotbiler.RuleGate.Core)                 | Local fail-closed authorization engine and built-in evaluators |
| [Fotbiler.RuleGate.Manifest](https://www.nuget.org/packages/Fotbiler.RuleGate.Manifest)         | YAML manifest loading, validation, and compilation             |
| [Fotbiler.RuleGate.AspNetCore](https://www.nuget.org/packages/Fotbiler.RuleGate.AspNetCore)     | ASP.NET Core integration                                       |
| [Fotbiler.RuleGate.Cli](https://www.nuget.org/packages/Fotbiler.RuleGate.Cli)                   | .NET tool for deterministic manifest validation and CI usage   |
| [Fotbiler.RuleGate.Keycloak](https://www.nuget.org/packages/Fotbiler.RuleGate.Keycloak)         | Optional Keycloak claim normalization and subject mapping      |

## Documentation

- [Manifest guide](https://github.com/fotbiler-lab/rulegate/blob/main/docs/manifests.md)
- [Getting started](https://github.com/fotbiler-lab/rulegate/blob/main/docs/getting-started.md)
- [Authorization model](https://github.com/fotbiler-lab/rulegate/blob/main/docs/authorization-model.md)
- [Security model](https://github.com/fotbiler-lab/rulegate/blob/main/docs/security.md)
- [RuleGate CLI](https://github.com/fotbiler-lab/rulegate/blob/main/docs/cli.md)
- [Documentation index](https://github.com/fotbiler-lab/rulegate/blob/main/docs/README.md)

## Security

Treat manifest files as security-sensitive configuration. Load policies from
controlled sources, reject failed compilation, and never continue with a
partial or stale policy set.

Report suspected vulnerabilities through the
[private security reporting process](https://github.com/fotbiler-lab/rulegate/security/policy).

## License

RuleGate is licensed under the
[Apache License 2.0](https://github.com/fotbiler-lab/rulegate/blob/main/LICENSE).
