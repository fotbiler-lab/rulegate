# RuleGate Manifest

YAML manifest loading, validation, and policy compilation for RuleGate.

Manifest converts `rulegate.yaml` documents into immutable RuleGate policy
definitions. Loading and validation failures are structured, and failed
compilation does not return a partial policy collection. YAML files and
embedded YAML resources can be used directly as reloadable policy sources.

RuleGate is currently in preview. Public APIs and the manifest schema may
change before the first stable release.

## Installation

    dotnet add package Fotbiler.RuleGate.Manifest --version 0.9.0-preview.4

## Compatibility

This package targets .NET Standard 2.0, .NET 8, .NET 9, and .NET 10.
Manifest compilation remains fail-closed on every target.

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
            - attributeComparison:
                left:
                  source: resource
                  name: ownerId
                operator: equal
                right:
                  source: subject
                  name: id
            - timeWindow:
                days: [monday, tuesday, wednesday, thursday, friday]
                start: "08:00"
                end: "18:00"
                timeZone: Europe/Istanbul
            - contextAge:
                timestamp: mfa
                maximumAge: "00:15:00"
            - context:
                property: trustedDevice
                operator: equal
                valueType: boolean
                value: true

The compiled policy collection can be registered with the RuleGate engine or
the ASP.NET Core integration.

ASP.NET Core applications can register a validated YAML source directly:

    builder.Services
        .AddRuleGate()
        .AddYamlPolicyFile(
            "rulegate.yaml",
            options => options.ReloadOnChange = true);

Failed reloads preserve the last valid immutable policy snapshot.

The manifest supports typed scalar and collection literals, ordinal string
comparison, value-less presence, null and collection-state operators, and
subject, resource, context, or literal operand comparisons.
It also supports explicit-time-zone schedules, bounded date-time rules,
authentication and MFA age, and canonical context-property requirements.

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

- [Manifest guide](https://github.com/fotbiler-lab/rulegate/blob/main/docs/manifests.md)
- [Getting started](https://github.com/fotbiler-lab/rulegate/blob/main/docs/getting-started.md)
- [Authorization model](https://github.com/fotbiler-lab/rulegate/blob/main/docs/authorization-model.md)
- [Security model](https://github.com/fotbiler-lab/rulegate/blob/main/docs/security.md)
- [RuleGate CLI](https://github.com/fotbiler-lab/rulegate/blob/main/docs/cli.md)
- [Policy testing](https://github.com/fotbiler-lab/rulegate/blob/main/docs/policy-testing.md)
- [Policy sources and atomic reload](https://github.com/fotbiler-lab/rulegate/blob/main/docs/policy-sources.md)
- [Telemetry, performance, and concurrency](https://github.com/fotbiler-lab/rulegate/blob/main/docs/telemetry-performance-concurrency.md)
- [Documentation index](https://github.com/fotbiler-lab/rulegate/blob/main/docs/README.md)
- [Detailed minimal manifest](https://github.com/fotbiler-lab/rulegate/blob/main/samples/aspnetcore-minimal/rulegate.yaml)
- [Document approval policy manifest](https://github.com/fotbiler-lab/rulegate/blob/main/samples/document-approval/api/rulegate.yaml)

## Security

Treat manifest files as security-sensitive configuration. Load policies from
controlled sources, reject failed compilation, and never continue with a
partial policy set. Reloadable hosts preserve the last valid complete snapshot
when a candidate source fails.

Report suspected vulnerabilities through the
[private security reporting process](https://github.com/fotbiler-lab/rulegate/security/policy).

## License

RuleGate is licensed under the
[MIT License](https://github.com/fotbiler-lab/rulegate/blob/main/LICENSE).

## Defensive manifest limits

The manifest loader and validator fail closed for oversized or structurally
excessive input. A manifest is limited to 1 MiB of source content, 4,096
policies, requirement depth 64, 4,096 nodes per policy, 1,024 children per
logical requirement, and 65,536 total requirement nodes. Programmatically
constructed requirement cycles are rejected before semantic validation.

## YAML security profile

RuleGate manifests use a deliberately restricted YAML profile:

- a manifest file must contain valid UTF-8; an optional UTF-8 BOM is accepted;
- UTF-16, UTF-32, and malformed UTF-8 files are rejected;
- exactly one YAML document is allowed;
- anchors and aliases are rejected;
- explicit local and global YAML tags are rejected;
- duplicate mapping keys and unknown model properties are rejected;
- parser recursion and manifest resource limits remain enforced.

These restrictions avoid alias expansion, object-graph cycles introduced during
deserialization, ambiguous encoding behavior, type-tag activation, and partial
multi-document interpretation. Rejected inputs produce
`ManifestLoadCodes.InvalidYaml`; they are never partially compiled into active
policies.
