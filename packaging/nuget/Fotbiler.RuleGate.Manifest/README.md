# Fotbiler.RuleGate.Manifest

YAML manifest loading, validation, and policy compilation for Fotbiler
RuleGate.

Manifest converts `rulegate.yaml` documents into immutable RuleGate policy
definitions. Loading and validation failures are structured, and failed
compilation does not return a partial policy collection.

RuleGate is currently in preview. Public APIs and the manifest schema may
change before the first stable release.

## Installation

    dotnet add package Fotbiler.RuleGate.Manifest --prerelease

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

The compiled policy collection can be registered with the RuleGate engine or
the ASP.NET Core integration.

## RuleGate packages

| Package | Purpose |
|---|---|
| [Fotbiler.RuleGate.Abstractions](https://www.nuget.org/packages/Fotbiler.RuleGate.Abstractions) | Public authorization contracts and extension abstractions |
| [Fotbiler.RuleGate.Core](https://www.nuget.org/packages/Fotbiler.RuleGate.Core) | Local fail-closed authorization engine and built-in evaluators |
| [Fotbiler.RuleGate.Manifest](https://www.nuget.org/packages/Fotbiler.RuleGate.Manifest) | YAML manifest loading, validation, and compilation |
| [Fotbiler.RuleGate.AspNetCore](https://www.nuget.org/packages/Fotbiler.RuleGate.AspNetCore) | ASP.NET Core integration |

## Documentation

- [Manifest guide](https://github.com/fotbiler-lab/rulegate/blob/main/docs/manifests.md)
- [Getting started](https://github.com/fotbiler-lab/rulegate/blob/main/docs/getting-started.md)
- [Authorization model](https://github.com/fotbiler-lab/rulegate/blob/main/docs/authorization-model.md)
- [Security model](https://github.com/fotbiler-lab/rulegate/blob/main/docs/security.md)
- [Documentation index](https://github.com/fotbiler-lab/rulegate/blob/main/docs/README.md)

## Security

Treat manifest files as security-sensitive configuration. Load policies from
controlled sources, reject failed compilation, and never continue with a
partial or stale policy set.

Report suspected vulnerabilities through the
[private security reporting process](https://github.com/fotbiler-lab/rulegate/security/policy).

## License

Fotbiler RuleGate is licensed under the
[Apache License 2.0](https://github.com/fotbiler-lab/rulegate/blob/main/LICENSE).
