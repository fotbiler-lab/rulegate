# Explain and Lint

RuleGate provides two deterministic inspection commands:

- `rulegate explain` evaluates one policy-test request and produces a
  value-free structural explanation of the decision;
- `rulegate lint` performs static analysis on a valid manifest and reports
  risky, contradictory, or unnecessarily complex requirement structures.

Both commands support human-readable text and stable JSON output for CI.

## Explain one decision

`explain` uses the same `authorization.tests.yaml` fixture format as
`rulegate test`. Select exactly one test by its case-sensitive identifier:

```bash
rulegate explain \
  ./authorization.tests.yaml \
  --test confidential-document-denied
```

Use JSON for automation:

```bash
rulegate explain \
  ./authorization.tests.yaml \
  --test confidential-document-denied \
  --format json
```

The fixture and referenced manifest are fully loaded and validated before
evaluation. The command then runs the selected request through the same policy
provider, requirement dispatcher, and built-in evaluators used by the runtime.
A deny or indeterminate decision is a successful explanation and returns exit
code `0`; invalid input or an unknown test identifier returns `1`.

## Explanation output

The report contains:

- the final `allow`, `deny`, or `indeterminate` outcome;
- the matched policy identifier, or no matching policy;
- stable failure codes;
- evaluated requirement kinds and parent-child structure;
- requirement identifiers and attribute names used for diagnostics;
- each evaluated requirement's outcome.

Logical `any` evaluation stops after the first satisfied child, matching
runtime behavior. The explanation therefore reports the branches actually
evaluated for that decision rather than inventing results for skipped branches.

## Redaction boundary

Explanation output never includes:

- subject or resource identifiers;
- role or permission values supplied by the request;
- subject, resource, or context attribute values;
- literal policy values;
- test descriptions;
- random evaluation identifiers or timing measurements.

Policy identifiers, requirement identifiers, attribute sources, and attribute
names are structural diagnostics and remain visible. Treat them as trusted
operational metadata. Do not return CLI explanations directly to untrusted HTTP
clients.

JSON includes `sensitiveValuesRedacted: true` so consumers can verify the
default security contract.

## Lint a manifest

Lint the default `rulegate.yaml`:

```bash
rulegate lint
```

Lint an explicit manifest or request JSON:

```bash
rulegate lint ./policies/rulegate.yaml
rulegate lint ./policies/rulegate.yaml --format json
```

Manifest loading and validation run before static analysis. Invalid YAML or an
invalid manifest produces validation diagnostics and no lint findings.

## Lint rules

| Code        | Severity | Detects                                                                 |
| ----------- | -------- | ----------------------------------------------------------------------- |
| `RGLINT001` | warning  | Structurally duplicate children in the same `all` or `any` requirement  |
| `RGLINT002` | error    | `X` with `not X`, or conflicting equality constraints inside `all`      |
| `RGLINT003` | warning  | Logical branches made irrelevant by `all`/`any` absorption              |
| `RGLINT004` | warning  | Requirement trees deeper than the recommended depth of eight            |
| `RGLINT005` | warning  | Single-child, nested same-kind, or difficult-to-audit negated logic     |
| `RGLINT006` | error    | Duplicate requirement identifiers anywhere in the manifest              |
| `RGLINT007` | warning  | Requirement identifiers that collide with policy identifiers            |
| `RGLINT008` | warning  | Negative attribute operators inside `any` that may grant access broadly |
| `RGLINT009` | warning  | Policy requirement trees containing more than 32 nodes                  |

Fingerprints ignore requirement identifiers and attribute values are used only
inside the local process for structural comparison. Lint output never prints
those values.

The current manifest schema has no reusable named-definition section. The
linter therefore does not claim that a policy is unused: only the host
application knows which resource/action routes it invokes. Requirement-ID and
policy-ID collisions that can be proven from the manifest are reported.

## Exit codes

|  Code | Meaning                                                                |
| ----: | ---------------------------------------------------------------------- |
|   `0` | Explanation completed, or the manifest has no lint findings            |
|   `1` | Input is invalid, selection failed, or one or more lint findings exist |
|   `2` | Command-line usage is invalid                                          |
|   `3` | An unexpected internal failure occurred                                |
| `130` | Operation was canceled                                                 |

Lint warnings intentionally return `1`, making the default command suitable
for a strict CI quality gate.

## CI example

```bash
set -euo pipefail

rulegate validate ./rulegate.yaml --format json
rulegate lint ./rulegate.yaml --format json
rulegate test ./authorization.tests.yaml --format json
rulegate explain \
  ./authorization.tests.yaml \
  --test confidential-document-denied \
  --format json
```

Keep fixtures free of production secrets even though explanation reports are
redacted. Fixture files remain ordinary local files and can be read by any
process with filesystem access.
