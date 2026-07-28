# RuleGate CLI

`Fotbiler.RuleGate.Cli` is the RuleGate command-line tool for deterministic
manifest validation, C# constant generation, and CI automation.

The installed command is `rulegate`.

## Release status

The latest public tool package is `0.3.0-preview.1` and provides deterministic
manifest validation.

The current repository source additionally contains the C# generation command
planned for `0.3.0-preview.2`. Until that preview is published, run generation
from the repository with `dotnet run` as shown below. Release preparation will
update the exact installation version before publication.

## Supported runtimes

RuleGate CLI targets:

| Runtime | Target framework |
|---|---|
| .NET 8 | `net8.0` |
| .NET 9 | `net9.0` |
| .NET 10 | `net10.0` |

The packaged tool, generated source, and generated-code consumers are verified
on all three target frameworks.

## Package source

The public RuleGate CLI package is published on NuGet.org:

```text
https://api.nuget.org/v3/index.json
```

GitHub Packages is not used as the public RuleGate package registry.

## Install the published preview

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
rulegate generate csharp [file] --namespace <namespace>
rulegate generate csharp [file] --namespace <namespace> --output <file>
rulegate generate csharp [file] --namespace <namespace> --output <file> --check
```

Run command-specific help with:

```bash
rulegate validate --help
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

## Generate C# constants from current source

Until `0.3.0-preview.2` is published, invoke the repository build:

```bash
dotnet run \
  --project src/Fotbiler.RuleGate.Cli/Fotbiler.RuleGate.Cli.csproj \
  --framework net10.0 \
  -- \
  generate csharp \
  ./rulegate.yaml \
  --namespace Sample.Authorization
```

When `--output` is omitted, standard output contains only generated C# source.
This allows redirection and composition with other command-line tools.

The generated source contains three public static classes:

- `RuleGatePolicies`
- `RuleGateResourceTypes`
- `RuleGateActions`

Each constant preserves the exact manifest value while exposing a deterministic
C# identifier.

## Generate a file

```bash
dotnet run \
  --project src/Fotbiler.RuleGate.Cli/Fotbiler.RuleGate.Cli.csproj \
  --framework net10.0 \
  -- \
  generate csharp \
  ./rulegate.yaml \
  --namespace Sample.Authorization \
  --output Generated/RuleGate.g.cs
```

File output is:

- UTF-8 without a byte-order mark;
- LF-only;
- sorted deterministically by ordinal manifest value;
- written through a temporary file and atomically replaced;
- produced only after complete manifest compilation and generation succeed.

A failed manifest or generation diagnostic leaves an existing output file
unchanged.

## Check for stale generated output

Use `--check` in CI:

```bash
dotnet run \
  --project src/Fotbiler.RuleGate.Cli/Fotbiler.RuleGate.Cli.csproj \
  --framework net10.0 \
  -- \
  generate csharp \
  ./rulegate.yaml \
  --namespace Sample.Authorization \
  --output Generated/RuleGate.g.cs \
  --check
```

Check mode never modifies the output file. It compares the expected UTF-8 bytes
with the existing file:

- current output returns `0`;
- missing or stale output returns `1`;
- using `--check` without `--output` returns `2`.

A byte-order mark, CRLF conversion, manual edit, changed namespace, changed
manifest, or changed generator output makes the file stale.

## Identifier generation and collisions

Manifest policy IDs, resource types, and actions are converted into PascalCase
C# identifiers. Non-alphanumeric separators start a new identifier segment, and
a leading number is prefixed with `_`.

Different values may normalize to the same C# identifier. For example,
`orders.read` and `orders-read` both normalize to `OrdersRead`. RuleGate treats
this as generation diagnostic `RGCG004` and produces no source.

Namespace values must be valid dotted C# namespaces. Invalid namespaces and
empty or conflicting manifest values fail closed.

## Exit codes

| Exit code | Name | Meaning |
|---:|---|---|
| `0` | Success | Validation, generation, file output, or stale check completed successfully |
| `1` | Input or generated-output failure | Manifest loading/validation, generation, missing output, or stale output failed |
| `2` | Usage error | The command or option combination is invalid |
| `3` | Internal error | An unexpected failure occurred |
| `130` | Cancelled | The operation was cancelled |

## CI validation and generation example

```bash
set -euo pipefail

dotnet run \
  --project src/Fotbiler.RuleGate.Cli/Fotbiler.RuleGate.Cli.csproj \
  --framework net10.0 \
  -- \
  validate \
  ./rulegate.yaml \
  --format json

dotnet run \
  --project src/Fotbiler.RuleGate.Cli/Fotbiler.RuleGate.Cli.csproj \
  --framework net10.0 \
  -- \
  generate csharp \
  ./rulegate.yaml \
  --namespace Sample.Authorization \
  --output Generated/RuleGate.g.cs \
  --check
```

After `0.3.0-preview.2` is published, the same subcommands can be executed
through the installed `rulegate` tool.

## Information command

```bash
rulegate info
```

The information command reports safe runtime and tool information intended for
support and troubleshooting. It does not print secrets, environment variables,
manifest contents, or policy inputs.

## Security behavior

Validation and generation reuse `RuleGateManifestCompiler` and preserve its
fail-closed behavior:

- invalid manifests never produce partial compiled policies or generated code;
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

### The public package does not recognize `generate`

The public `0.3.0-preview.1` package predates code generation. Use the repository
`dotnet run` command until `0.3.0-preview.2` is published.

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
- [Authorization model](authorization-model.md)
- [Security model](security.md)
- [Roadmap](roadmap.md)
- [Documentation index](README.md)
