# C# Code Generation

RuleGate can generate deterministic C# constants from a validated
`rulegate.yaml` manifest. Generated constants keep policy, resource-type, and
action identifiers aligned between the manifest and application code without
turning generated output into an authorization boundary.

## Prerequisites

Install the current RuleGate CLI preview:

```bash
dotnet tool install \
  --global \
  Fotbiler.RuleGate.Cli \
  --version 0.8.0-preview.2
```

You need a complete manifest that passes `rulegate validate`:

```bash
rulegate validate ./rulegate.yaml
```

## Generate constants

Generate a C# file from the manifest:

```bash
rulegate generate csharp \
  ./rulegate.yaml \
  --namespace Sample.Authorization \
  --output Generated/RuleGate.g.cs
```

The generated source contains three public static classes:

| Class                   | Manifest values    |
| ----------------------- | ------------------ |
| `RuleGatePolicies`      | Policy identifiers |
| `RuleGateResourceTypes` | Resource types     |
| `RuleGateActions`       | Actions            |

Each constant exposes a valid C# identifier while preserving the exact string
value used by RuleGate. Applications can therefore replace repeated string
literals with generated constants:

```csharp
var request =
    new AuthorizationRequest(
        subject,
        new AuthorizationResource(
            RuleGateResourceTypes.Document,
            documentId),
        RuleGateActions.Read,
        context);
```

Omit `--output` to write only the generated C# source to standard output:

```bash
rulegate generate csharp \
  ./rulegate.yaml \
  --namespace Sample.Authorization
```

## Keep generated output current

Commit the generated file when application code depends on it. In CI, use the
same generation arguments with `--check`:

```bash
rulegate generate csharp \
  ./rulegate.yaml \
  --namespace Sample.Authorization \
  --output Generated/RuleGate.g.cs \
  --check
```

Check mode compares the expected bytes with the existing file and never
rewrites it. It succeeds when the file is current and fails when the output is
missing or stale.

Regenerate and review the diff after changing:

- Policy identifiers, resource types, or actions
- The generated namespace or output path
- The RuleGate CLI version

## Deterministic output

Generated files are:

- UTF-8 without a byte-order mark
- LF-only
- Sorted by ordinal manifest value
- Written through atomic file replacement
- Produced only after complete manifest compilation and generation succeed

Manual edits, line-ending conversion, a byte-order mark, or changed generation
inputs make a committed file stale.

## Identifier rules and collisions

RuleGate converts manifest values into PascalCase C# identifiers.
Non-alphanumeric separators begin a new identifier segment, and identifiers
that begin with a number receive an `_` prefix.

Distinct values can normalize to the same identifier. For example,
`orders.read` and `orders-read` both become `OrdersRead`. RuleGate reports
diagnostic `RGCG004` and produces no source when a collision occurs.

Namespaces must be valid dotted C# namespaces. Invalid namespaces, empty
values, collisions, and invalid manifests fail the complete generation
operation.

## Security boundary

Generated constants reduce identifier drift; they do not authorize a request.
The protected backend operation and RuleGate authorization engine remain the
security boundary.

Generation preserves fail-closed behavior:

- Invalid manifests never produce partial source.
- Generation diagnostics prevent all output.
- Existing files remain unchanged when generation fails.
- Check mode never repairs or rewrites stale output.

## Related documentation

- [RuleGate CLI](cli.md) for installation, validation, commands, and exit codes
- [Manifest guide](manifests.md) for the source manifest format
- [Getting started](getting-started.md) for an end-to-end application flow
- [Security model](security.md) for trust boundaries and production controls
- [Roadmap](roadmap.md) for published and planned milestones
