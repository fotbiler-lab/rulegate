# 4. Policy Language

`rulegate.yaml` is the human-readable source of authorization policy. RuleGate
loads the complete document, validates it, compiles it into typed policy
definitions, and activates it only when the entire candidate is valid.

## Document structure

```yaml
schemaVersion: 1

application:
  id: document-service
  name: Document Service

policies:
  - id: document-read
    resourceType: document
    action: read
    requirement:
      permission: DOC.READ
```

| Member                    | Meaning                                                |
| ------------------------- | ------------------------------------------------------ |
| `schemaVersion`           | Exact manifest schema; currently `1`                   |
| `application`             | Stable application metadata                            |
| `policies`                | Non-empty collection of policy definitions             |
| `id`                      | Unique policy identifier used by tools and projections |
| `resourceType` + `action` | Unique route selected by the engine                    |
| `requirement`             | One built-in or logical requirement tree               |

Every requirement may have an optional `id`. Add IDs to security-significant
leaves so tests and redacted diagnostics can identify them without exposing
values.

## Permission and role requirements

```yaml
requirement:
  all:
    - id: approve-capability
      permission: DOC.APPROVE
    - id: approver-responsibility
      role: DOCUMENT.APPROVER
```

Matching is exact and case-sensitive. Empty or duplicate values in the runtime
subject are normalized safely, but no wildcard or implicit hierarchy exists.

## Logical requirements

```yaml
requirement:
  all:
    - any:
        - permission: DOC.READ
        - role: DOCUMENT.READER
    - not:
        role: DOCUMENT.BLOCKED
```

| Operator | Meaning                                  | Security behavior                                                                      |
| -------- | ---------------------------------------- | -------------------------------------------------------------------------------------- |
| `all`    | Every child must be satisfied            | One denied or indeterminate child denies                                               |
| `any`    | At least one child must be satisfied     | Allows only after a satisfied child; indeterminate input cannot become allow by itself |
| `not`    | Child must be conclusively not satisfied | Indeterminate stays indeterminate; missing data is not inverted into access            |

Keep trees shallow enough to review. Manifest and runtime depth limits protect
the application from unbounded input.

## Literal attribute requirements

Read an attribute from `subject`, `resource`, or `context` and compare it to a
typed literal:

```yaml
requirement:
  attribute:
    source: resource
    name: status
    operator: in
    valueType: stringCollection
    value: [draft, returned]
```

### Operators

| Family     | Operators                                                          | Typical use                         |
| ---------- | ------------------------------------------------------------------ | ----------------------------------- |
| Equality   | `equal`, `notEqual`                                                | status, organization, boolean flags |
| Ordering   | `greaterThan`, `greaterThanOrEqual`, `lessThan`, `lessThanOrEqual` | limits, classification, dates       |
| String     | `contains`, `startsWith`, `endsWith`                               | normalized domains or prefixes      |
| Collection | `contains`, `containsAny`, `containsAll`, `intersects`             | groups, labels, regions             |
| Membership | `in`, `notIn`                                                      | scalar in an approved/blocked set   |
| Presence   | `exists`, `notExists`                                              | whether a key was supplied          |
| Null       | `isNull`, `isNotNull`                                              | present explicit null state         |
| Empty      | `isEmpty`, `isNotEmpty`                                            | present collection state            |

### Value types

| Token                      | Runtime type                                        |
| -------------------------- | --------------------------------------------------- |
| `string`                   | `string`                                            |
| `boolean`                  | `bool`                                              |
| `number`                   | integer or invariant decimal normalized as a number |
| `dateTimeOffset`           | ISO 8601 value with `Z` or a numeric offset         |
| `nullValue`                | explicit null literal                               |
| `stringCollection`         | homogeneous strings                                 |
| `booleanCollection`        | homogeneous booleans                                |
| `numberCollection`         | homogeneous numbers                                 |
| `dateTimeOffsetCollection` | homogeneous date/time values                        |

Not every operator accepts every type. For example, boolean ordering is
invalid, and collection operations require compatible element kinds. The CLI
rejects incompatible combinations.

### String comparison

String matching is ordinal and case-sensitive by default:

```yaml
attribute:
  source: subject
  name: department
  operator: startsWith
  stringComparison: ordinalIgnoreCase
  valueType: string
  value: operations
```

Use `ordinalIgnoreCase` only when the business identifier is intentionally
case-insensitive. Do not use culture-sensitive display text as a security
identifier.

### Missing, null, and empty are different

| Runtime state         | `exists` | `notExists` | `isNull` | `isNotNull` |       `isEmpty` |
| --------------------- | -------: | ----------: | -------: | ----------: | --------------: |
| Key absent            |       no |         yes |       no |          no |              no |
| Key present with null |      yes |          no |      yes |          no |              no |
| Empty collection      |      yes |          no |       no |         yes |             yes |
| Non-empty value       |      yes |          no |       no |         yes | depends on kind |

Missing data never becomes implicit null. Use the operator that represents the
domain state you actually intend.

## Attribute-to-attribute comparison

Compare trusted values from two sources:

```yaml
requirement:
  all:
    - attributeComparison:
        left:
          source: subject
          name: organizationId
        operator: equal
        right:
          source: resource
          name: organizationId
    - attributeComparison:
        left:
          source: subject
          name: clearanceLevel
        operator: greaterThanOrEqual
        right:
          source: resource
          name: classificationLevel
```

An operand can be an attribute or a literal. This example caps an amount:

```yaml
attributeComparison:
  left:
    source: resource
    name: totalAmount
  operator: lessThanOrEqual
  right:
    valueType: number
    value: 50000
```

Both values must have compatible types. Missing or incompatible values deny.

## Canonical context requirements

Canonical context properties give common request facts stable names:

```yaml
requirement:
  all:
    - context:
        property: networkZone
        operator: in
        valueType: stringCollection
        value: [internal, vpn]
    - context:
        property: requestChannel
        operator: equal
        valueType: string
        value: web
    - context:
        property: trustedDevice
        operator: equal
        valueType: boolean
        value: true
```

Canonical properties include authentication method, request channel, network
zone, tenant ID, organization ID, trusted device, and identity type. The
application must still provide trustworthy values.

Use a normal `attribute` with `source: context` for application-specific facts
such as a validated risk score or correlation category.

## Authentication and MFA age

```yaml
requirement:
  all:
    - contextAge:
        timestamp: authentication
        maximumAge: '08:00:00'
    - contextAge:
        timestamp: mfa
        maximumAge: '00:15:00'
```

`authentication` reads the canonical authentication timestamp. `mfa` reads
the multi-factor timestamp. A missing, future, malformed, or too-old timestamp
does not satisfy the requirement.

## Recurring time windows

```yaml
timeWindow:
  days: [monday, tuesday, wednesday, thursday, friday]
  start: '08:00'
  end: '18:00'
  timeZone: Europe/Istanbul
```

RuleGate converts the trusted evaluation time into the named time zone. Use
exact `HH:mm` values and lowercase day tokens. Overnight windows are supported
by the defined time semantics; test boundary instants, daylight-saving
transitions, and the host's available time-zone database.

An organization-specific schedule should not be hard-coded into a shared
policy when every organization differs. A context provider can resolve the
current organization's schedule into trusted attributes, or applications can
maintain separate policy routes/snapshots when that is the clearer model.

## Bounded date-time windows

```yaml
dateTimeWindow:
  startsAt: '2026-09-01T00:00:00Z'
  endsAt: '2026-10-01T00:00:00Z'
```

Use this for a release, campaign, emergency exception, or migration interval
with fixed absolute boundaries. Always include a UTC marker or numeric offset.

## Complete approval policy

```yaml
schemaVersion: 1

application:
  id: document-approval
  name: Document Approval

policies:
  - id: document-approve
    resourceType: document
    action: approve
    requirement:
      id: complete-approval-rule
      all:
        - permission: DOC.APPROVE
        - role: DOCUMENT.APPROVER
        - attribute:
            source: resource
            name: status
            operator: equal
            valueType: string
            value: submitted
        - attributeComparison:
            left: { source: subject, name: organizationId }
            operator: equal
            right: { source: resource, name: organizationId }
        - attributeComparison:
            left: { source: resource, name: totalAmount }
            operator: lessThanOrEqual
            right: { source: subject, name: approvalLimit }
        - not:
            attributeComparison:
              left: { source: subject, name: userId }
              operator: equal
              right: { source: resource, name: ownerId }
        - context:
            property: networkZone
            operator: in
            valueType: stringCollection
            value: [internal, vpn]
        - context:
            property: trustedDevice
            operator: equal
            valueType: boolean
            value: true
        - contextAge:
            timestamp: mfa
            maximumAge: '00:15:00'
        - timeWindow:
            days: [monday, tuesday, wednesday, thursday, friday]
            start: '08:00'
            end: '18:00'
            timeZone: Europe/Istanbul
```

The manifest states the rule. The host is responsible for supplying every
referenced value from the correct trusted source.

## Validate and test every change

```bash
rulegate validate rulegate.yaml
rulegate lint rulegate.yaml
rulegate test authorization.tests.yaml
```

Validation proves structural correctness. Linting finds maintainability risks.
Policy tests prove behavior for explicit subjects, resources, context, and
times. None of these replaces endpoint integration tests.

## Further reference

- [Complete manifest reference](../manifests.md)
- [Policy testing](../policy-testing.md)
- [Explain and lint](../explain-and-lint.md)
- [Minimal sample manifest](../../samples/aspnetcore-minimal/rulegate.yaml)

---

Previous: [First protected API](03-First-Protected-API.md) · Next:
[ASP.NET Core integration](05-ASP.NET-Core-Integration.md)
