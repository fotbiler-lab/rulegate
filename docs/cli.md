# RuleGate CLI

`Fotbiler.RuleGate.Cli` is the RuleGate command-line tool for deterministic
manifest validation and linting, policy testing, redacted decision
explanations, C# constant generation, stale-output checks, and CI automation.

The installed command is `rulegate`.

## Current release

The current public tool package is `0.9.0-preview.3`.

## Supported runtimes

| Runtime | Target framework |
| ------- | ---------------- |
| .NET 8  | `net8.0`         |
| .NET 9  | `net9.0`         |
| .NET 10 | `net10.0`        |

The packaged tool, generated source, and generated-code consumers are verified
on all three target frameworks.

## Package source

The public RuleGate CLI package is published on NuGet.org:

```text
https://api.nuget.org/v3/index.json
```

GitHub Packages is not used as the public RuleGate package registry.

## Install

```bash
dotnet tool install \
  --global \
  Fotbiler.RuleGate.Cli \
  --version 0.9.0-preview.3
```

Update an existing global installation:

```bash
dotnet tool update \
  --global \
  Fotbiler.RuleGate.Cli \
  --version 0.9.0-preview.3
```

Verify the installation:

```bash
rulegate --version
rulegate info
```

## Command overview

```text
rulegate --help
rulegate --version
rulegate info
rulegate validate [file]
rulegate validate [file] --format text
rulegate validate [file] --format json
rulegate test [authorization.tests.yaml]
rulegate test [authorization.tests.yaml] --filter <text>
rulegate test [authorization.tests.yaml] --format json
rulegate explain [authorization.tests.yaml] --test <id>
rulegate explain [authorization.tests.yaml] --test <id> --format json
rulegate lint [file]
rulegate lint [file] --format json
rulegate generate csharp [file] --namespace <namespace>
rulegate generate csharp [file] --namespace <namespace> --output <file>
rulegate generate csharp [file] --namespace <namespace> --output <file> --check
```

Run command-specific help with:

```bash
rulegate validate --help
rulegate test --help
rulegate explain --help
rulegate lint --help
rulegate generate csharp --help
```

## Validate the default manifest

When no file is supplied, RuleGate searches for `rulegate.yaml` in the current
working directory:

```bash
rulegate validate
```

A valid manifest produces human-readable success output and exit code `0`.

## Validate an explicit manifest

```bash
rulegate validate ./policies/rulegate.yaml
```

Relative paths are resolved from the current working directory. Absolute paths
may also be supplied.

Validation covers the complete manifest requirement model, including typed
attributes, attribute comparisons, explicit-time-zone schedules, bounded
date-time rules, authentication age, and canonical context policies.

## Validation output

Text is the default interactive format:

```bash
rulegate validate --format text
```

Use JSON when another process consumes the result:

```bash
rulegate validate --format json
```

JSON mode writes one complete JSON document to standard output. Automation
should use its fields and the process exit code rather than parsing
human-readable text.

## Test policy behavior

Evaluate the default `authorization.tests.yaml` without starting an
application:

```bash
rulegate test
```

Use an explicit fixture, select test identifiers, or request JSON output:

```bash
rulegate test ./policies/authorization.tests.yaml
rulegate test ./policies/authorization.tests.yaml --filter organization
rulegate test ./policies/authorization.tests.yaml --format json
```

Fixtures contain explicit subjects, resources, actions, context, fixed
evaluation times, and expected `allow`, `deny`, or `indeterminate` outcomes.
They can also assert the complete set of failure codes. The referenced manifest
is compiled before any test runs, and invalid fixture or manifest input prevents
all evaluation.

See the [policy-testing guide](policy-testing.md) for the fixture schema, typed
attributes, deterministic-time contract, filtering, output model, and security
boundary.

## Explain a decision safely

Select one exact test identifier from an `authorization.tests.yaml` fixture:

```bash
rulegate explain \
  ./policies/authorization.tests.yaml \
  --test organization-mismatch

rulegate explain \
  ./policies/authorization.tests.yaml \
  --test organization-mismatch \
  --format json
```

The command evaluates the request through the runtime requirement pipeline and
reports the final outcome, failure codes, and evaluated requirement tree. It
omits subject/resource identities, request and literal values, random
evaluation IDs, durations, and test descriptions. A deny or indeterminate
decision is still a successful explanation.

## Lint policy structure

```bash
rulegate lint
rulegate lint ./policies/rulegate.yaml
rulegate lint ./policies/rulegate.yaml --format json
```

Lint runs only after full manifest validation. It reports stable codes for
duplicate or contradictory requirements, absorbed logical branches, excessive
depth or complexity, unnecessary logical layers, identifier collisions, and
risky negative operators. Any finding returns exit code `1` for strict CI.

See the [Explain and Lint guide](explain-and-lint.md) for the complete output,
redaction, rule-code, and CI contracts.

## Generate C# constants

```bash
rulegate generate csharp \
  ./rulegate.yaml \
  --namespace Sample.Authorization \
  --output Generated/RuleGate.g.cs
```

Use the same command with `--check` to detect missing or stale committed output
without modifying it:

```bash
rulegate generate csharp \
  ./rulegate.yaml \
  --namespace Sample.Authorization \
  --output Generated/RuleGate.g.cs \
  --check
```

See the [C# code-generation guide](code-generation.md) for generated classes,
deterministic file guarantees, stale-output checks, identifier rules, collision
diagnostics, and security boundaries.

## Exit codes

| Exit code | Name                              | Meaning                                                                                         |
| --------: | --------------------------------- | ----------------------------------------------------------------------------------------------- |
|       `0` | Success                           | Validation, testing, explanation, clean lint, generation, file output, or stale check completed |
|       `1` | Input, finding, or output failure | Input validation, a test, lint finding, generation, missing output, or stale output failed      |
|       `2` | Usage error                       | The command or option combination is invalid                                                    |
|       `3` | Internal error                    | An unexpected failure occurred                                                                  |
|     `130` | Cancelled                         | The operation was cancelled                                                                     |

## CI example

```bash
set -euo pipefail

TOOL_DIRECTORY="$PWD/.rulegate-tools"

dotnet tool install \
  Fotbiler.RuleGate.Cli \
  --tool-path "$TOOL_DIRECTORY" \
  --version 0.9.0-preview.3

"$TOOL_DIRECTORY/rulegate" \
  validate \
  ./rulegate.yaml \
  --format json

"$TOOL_DIRECTORY/rulegate" \
  test \
  ./authorization.tests.yaml \
  --format json

"$TOOL_DIRECTORY/rulegate" \
  lint \
  ./rulegate.yaml \
  --format json

"$TOOL_DIRECTORY/rulegate" \
  explain \
  ./authorization.tests.yaml \
  --test organization-mismatch \
  --format json

"$TOOL_DIRECTORY/rulegate" \
  generate csharp \
  ./rulegate.yaml \
  --namespace Sample.Authorization \
  --output Generated/RuleGate.g.cs \
  --check
```

Do not combine an exact `--version` with `--prerelease`.

## Information command

```bash
rulegate info
```

The information command reports safe runtime and tool information intended for
support and troubleshooting. It does not print secrets, environment variables,
manifest contents, or policy inputs.

## Security behavior

Validation, policy testing, explanation, linting, and generation preserve
fail-closed manifest behavior:

- invalid manifests never produce partial compiled policies or generated code;
- invalid fixtures or manifests prevent every policy-test evaluation;
- invalid fixtures or manifests prevent decision explanation;
- lint findings are produced only after complete manifest validation;
- explanation reports omit identity-specific data and all request values;
- fixtures require explicit evaluation times and never read the system clock;
- unsupported requirements cannot grant access;
- identifier collisions prevent all source output;
- existing output files are preserved when generation fails;
- check mode never rewrites a stale or missing file;
- unexpected failures do not print stack traces by default;
- validation does not start an application or evaluate an authorization
  request.

Generated constants are identifiers, not authorization decisions. The protected
backend operation remains the security boundary.

## Troubleshooting

### The command is not found

Confirm that the .NET global tools directory is on `PATH`, then run:

```bash
dotnet tool list --global
```

### The output is stale

Regenerate without `--check`, review the diff, commit the intended source, and
run the check again.

### Generation reports `RGCG004`

Two distinct manifest values normalize to the same C# identifier. Rename at
least one policy ID, resource type, or action so every generated identifier is
unique.

### The default manifest is not found

Run from the directory containing `rulegate.yaml`, or provide an explicit path.

## Related documentation

- [Getting started](getting-started.md)
- [Manifest guide](manifests.md)
- [C# code generation](code-generation.md)
- [Policy testing](policy-testing.md)
- [Explain and Lint](explain-and-lint.md)
- [Authorization model](authorization-model.md)
- [Security model](security.md)
- [Roadmap](roadmap.md)
- [Documentation index](README.md)
