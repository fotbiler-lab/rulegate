# Policy Testing CLI

`rulegate test` evaluates authorization requests directly against a compiled
RuleGate manifest. It makes policy behavior repeatable in local development and
CI without starting ASP.NET Core, Angular, an identity provider, a database, or
another host dependency.

The command is implemented in the repository and scheduled for RuleGate CLI
`0.8.0-preview.2`.

## Run a fixture

The default fixture name is `authorization.tests.yaml`:

```bash
rulegate test
```

Pass another path when needed:

```bash
rulegate test ./policies/document-authorization.tests.yaml
```

Relative manifest paths inside a fixture are resolved from the fixture's
directory, not from the process working directory.

## Smallest complete fixture

```yaml
schemaVersion: 1
manifest: rulegate.yaml

tests:
  - id: document-reader-is-allowed
    request:
      subject:
        id: user-1
        permissions: [DOC.READ]
      resource:
        type: document
        id: document-1
      action: read
      context:
        evaluationTime: '2026-07-31T09:00:00Z'
    expect:
      outcome: allow

  - id: missing-permission-is-denied
    request:
      subject:
        id: user-2
      resource:
        type: document
        id: document-1
      action: read
      context:
        evaluationTime: '2026-07-31T09:00:00Z'
    expect:
      outcome: deny
      failureCodes:
        - RULEGATE_MISSING_PERMISSION
```

Every case supplies the complete request boundary used by RuleGate:

- `subject.id`, with optional `roles`, `permissions`, and `attributes`;
- `resource.type`, with optional `id` and `attributes`;
- `action`;
- `context.evaluationTime`, with optional `attributes`;
- the expected `allow`, `deny`, or `indeterminate` outcome.

`evaluationTime` is required and must include an explicit UTC offset. The test
runner never reads the system clock, which keeps time-window, date-time-window,
and age-policy tests deterministic.

## Expected outcomes and failure codes

Use one of these exact outcome values:

| Outcome         | Meaning                                                       |
| --------------- | ------------------------------------------------------------- |
| `allow`         | The matching requirement is satisfied                         |
| `deny`          | The requirement is not satisfied or no matching policy exists |
| `indeterminate` | The requirement cannot be evaluated safely                    |

`failureCodes` is optional. When omitted, the runner compares only the
outcome. When present, it must match the complete set of actual failure codes.
Comparison is ordinal and order-independent; duplicate expected codes are
invalid.

```yaml
expect:
  outcome: indeterminate
  failureCodes:
    - RULEGATE_ATTRIBUTE_TYPE_MISMATCH
```

This distinction is useful for proving both fail-closed behavior and its exact
reason. An `indeterminate` engine result still denies a protected operation,
but the fixture preserves that internal outcome instead of flattening it into
a generic denial.

## Typed attributes

Fixture attributes use explicit types so YAML scalar inference cannot change
authorization behavior between environments:

```yaml
attributes:
  - name: organizationId
    valueType: string
    value: records

  - name: clearanceLevel
    valueType: number
    value: '4'

  - name: trustedDevice
    valueType: boolean
    value: 'true'

  - name: authenticatedAt
    valueType: dateTimeOffset
    value: '2026-07-31T08:55:00Z'

  - name: groups
    valueType: stringCollection
    values: [records, legal]

  - name: archivedAt
    valueType: nullValue
```

Supported fixture value types are:

- `nullValue`;
- `string`;
- `boolean`;
- `number`;
- `dateTimeOffset`;
- `stringCollection`;
- `booleanCollection`;
- `numberCollection`;
- `dateTimeOffsetCollection`.

Scalar types use `value`; collection types use `values`. Attribute names are
ordinal and cannot be duplicated within one source. Collections are
homogeneous and preserve the framework's 256-element limit.

`subject.id` and `resource.id` are request properties, not implicit entries in
their attribute dictionaries. If a manifest compares an attribute named
`userId`, `ownerId`, or another identifier, add that attribute explicitly to
the fixture just as the host application would.

## Filter tests

`--filter` selects test identifiers containing the supplied text. Matching is
ordinal and case-insensitive:

```bash
rulegate test --filter organization
```

Descriptions are not searched. A filter that selects no tests fails with exit
code `1` and `RGTEST_FILTER_NO_MATCH`, preventing an empty selection from
silently passing CI.

## Text and JSON output

Text is the default interactive format:

```bash
rulegate test --format text
```

```text
PASS document-reader-is-allowed
  Outcome: allow
PASS missing-permission-is-denied
  Outcome: deny

Summary: 2 passed, 0 failed, 2 selected of 2 total.
```

Use JSON for automation:

```bash
rulegate test --format json
```

The JSON document includes fixture and manifest paths, the filter, total and
selected counts, pass/fail counts, input diagnostics, and per-test expected and
actual outcomes, failure codes, and matched policy identifier. JSON mode writes
one complete document to standard output, including fixture or manifest
failures.

Output never includes subject, resource, or context attribute values. Test
identifiers and descriptions are output, so do not put secrets or personal data
in those fields.

## Exit codes

|  Code | Meaning                                                   |
| ----: | --------------------------------------------------------- |
|   `0` | All selected policy expectations passed                   |
|   `1` | A fixture, manifest, filter, or policy expectation failed |
|   `2` | The command-line arguments or options are invalid         |
|   `3` | An unexpected internal error occurred                     |
| `130` | The operation was cancelled                               |

## CI example

```bash
set -euo pipefail

rulegate validate ./policies/rulegate.yaml --format json
rulegate test ./policies/authorization.tests.yaml --format json
```

Keep fixtures and their manifest in source control. Use fixed evaluation times
and supply all trusted inputs explicitly. A fixture validates the portable
RuleGate policy model; it does not execute application-specific ASP.NET Core
attribute providers, identity-provider adapters, endpoint handlers, or data
access.

## Complete repository example

The minimal ASP.NET Core sample contains a fixture covering permission, role,
logical, resource, attribute-to-attribute, context, null/empty, default-deny,
and indeterminate behavior:

- [`authorization.tests.yaml`](../samples/aspnetcore-minimal/authorization.tests.yaml)
- [`rulegate.yaml`](../samples/aspnetcore-minimal/rulegate.yaml)

Run it from the repository root:

```bash
rulegate test samples/aspnetcore-minimal/authorization.tests.yaml
```

## Related documentation

- [RuleGate CLI](cli.md)
- [Manifest guide](manifests.md)
- [Authorization model](authorization-model.md)
- [Security model](security.md)
- [Roadmap](roadmap.md)
