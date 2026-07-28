# RuleGate CLI

`Fotbiler.RuleGate.Cli` is the RuleGate command-line tool for deterministic
policy-manifest validation locally and in CI pipelines.

The installed command is `rulegate`.

## Supported runtimes

The `0.3.0-preview.1` preview tool package contains assets for:

| Runtime | Target framework |
|---|---|
| .NET 8 | `net8.0` |
| .NET 9 | `net9.0` |
| .NET 10 | `net10.0` |

## Package source

The public RuleGate CLI package is published on NuGet.org:

```text
https://api.nuget.org/v3/index.json
```

GitHub Packages is not used as the public RuleGate package registry. Standard
.NET installations can therefore use the default NuGet.org source without
adding a GitHub package source or authentication token.

## Install

Install the exact published preview:

```bash
dotnet tool install \
  --global \
  Fotbiler.RuleGate.Cli \
  --version 0.3.0-preview.1
```

Update an existing global installation:

```bash
dotnet tool update \
  --global \
  Fotbiler.RuleGate.Cli \
  --version 0.3.0-preview.1
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
```

Run command-specific help with:

```bash
rulegate validate --help
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

## Text output

Text is the default output format:

```bash
rulegate validate --format text
```

Text mode is intended for interactive terminal use. Validation diagnostics are
formatted for humans and may evolve while the stable exit-code contract is
preserved.

## JSON output

Use JSON when another process consumes the result:

```bash
rulegate validate --format json
```

JSON mode writes one complete JSON document to standard output. Diagnostic
messages that are not part of that document are kept on standard error so that
stdout remains machine-readable.

Automation should use the JSON fields and process exit code rather than parsing
human-readable text.

## Exit codes

| Exit code | Name | Meaning |
|---:|---|---|
| `0` | Success | The manifest loaded, validated, and compiled successfully |
| `1` | Validation failure | File loading, YAML, schema, structural, or semantic validation failed |
| `2` | Usage error | The command or option combination is invalid |
| `3` | Internal error | An unexpected failure occurred |
| `130` | Cancelled | The operation was cancelled |

Manifest validation errors use exit code `1`, regardless of whether the failure
originated from file access, YAML parsing, schema validation, structural
validation, or semantic validation.

## CI example

Install and validate the exact tool version in a CI job:

```bash
set -euo pipefail

TOOL_DIRECTORY="$PWD/.rulegate-tools"

dotnet tool install \
  Fotbiler.RuleGate.Cli \
  --tool-path "$TOOL_DIRECTORY" \
  --version 0.3.0-preview.1

"$TOOL_DIRECTORY/rulegate" \
  validate \
  ./rulegate.yaml \
  --format json
```

The job fails automatically when `rulegate validate` returns a non-zero exit
code.

Do not combine an exact `--version` with `--prerelease`. Exact release
installation uses only `--version`.

## Information command

```bash
rulegate info
```

The information command reports safe runtime and tool information intended for
support and troubleshooting. It does not print secrets, environment variables,
manifest contents, or policy inputs.

## Security behavior

CLI validation reuses `RuleGateManifestCompiler` and preserves its fail-closed
behavior:

- invalid manifests never return a partial compiled policy collection;
- unsupported or malformed requirements cannot grant access;
- unexpected failures do not print stack traces by default;
- JSON output remains isolated from standard-error diagnostics;
- cancellation has a distinct exit code;
- validation does not start an application or evaluate an authorization
  request.

Treat manifests as security-sensitive configuration. Validate only files from
trusted or intentionally reviewed sources.

## Troubleshooting

### The command is not found

Confirm that the .NET global tools directory is on `PATH`, then run:

```bash
dotnet tool list --global
```

### The default manifest is not found

Run the command from the directory containing `rulegate.yaml`, or provide an
explicit path:

```bash
rulegate validate ./path/to/rulegate.yaml
```

### JSON cannot be parsed

Ensure that the command uses `--format json` and that the caller reads stdout
separately from stderr.

### The tool reports a usage error

Display the supported command and option surface:

```bash
rulegate --help
rulegate validate --help
```

## Related documentation

- [Getting started](getting-started.md)
- [Manifest guide](manifests.md)
- [Authorization model](authorization-model.md)
- [Security model](security.md)
- [Roadmap](roadmap.md)
- [Documentation index](README.md)
