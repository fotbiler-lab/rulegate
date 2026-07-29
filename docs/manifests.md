# RuleGate Manifest Guide

This reference describes the complete `rulegate.yaml` format supported by the
current RuleGate preview.

For the shortest path to a running application, begin with
[Getting started](getting-started.md). Read the
[authorization model](authorization-model.md) when subjects, resources,
actions, policies, or requirements are new to you.

## What a manifest does

A RuleGate manifest declares application authorization policies in YAML.

The manifest compiler:

1. Reads YAML text or a YAML file.
2. Deserializes the YAML document.
3. Validates the RuleGate manifest structure.
4. Converts valid manifest policies into immutable runtime policy definitions.

```text
rulegate.yaml
      |
      v
YAML loader
      |
      v
Manifest validator
      |
      v
Manifest mapper
      |
      v
PolicyDefinition collection
```

A failed load or validation never returns a partially compiled policy
collection.

## Default file name

The conventional manifest file name is:

```text
rulegate.yaml
```

The compiler can also receive another explicit file path.

## Minimum valid manifest

```yaml
schemaVersion: 1

application:
  id: sample-application
  name: Sample Application

policies: []
```

The `policies` collection is required but may be empty.

An application with an empty policy collection will compile successfully, but
runtime authorization requests will be denied because no policy can match
them.

## Root structure

A manifest contains three root members:

| Member | Required | Description |
|---|---:|---|
| `schemaVersion` | Yes | Manifest schema version |
| `application` | Yes | Application identity and display information |
| `policies` | Yes | Authorization policy collection |

Unknown YAML properties are rejected.

Duplicate YAML keys are also rejected.

## Schema version

The current preview supports:

```yaml
schemaVersion: 1
```

Any other value produces:

```text
MANIFEST_UNSUPPORTED_SCHEMA_VERSION
```

The validation path is:

```text
schemaVersion
```

Schema versions are explicit so future manifest changes can be introduced
without silently changing the meaning of an existing policy file.

## Application

The application section requires both an identifier and a name:

```yaml
application:
  id: document-service
  name: Document Service
```

| Member | Required | Purpose |
|---|---:|---|
| `id` | Yes | Stable application identifier |
| `name` | Yes | Human-readable application name |

Both values must contain non-whitespace text.

The name is descriptive. Authorization behavior should not depend on display
text.

## Policies

Each policy connects one resource type and action to one requirement tree.

```yaml
policies:
  - id: document-read
    resourceType: document
    action: read
    requirement:
      permission: document.read
```

A policy requires:

| Member | Required | Purpose |
|---|---:|---|
| `id` | Yes | Stable policy identifier |
| `resourceType` | Yes | Protected resource category |
| `action` | Yes | Protected business operation |
| `requirement` | Yes | Root authorization condition |

### Policy identifier uniqueness

Policy identifiers must be unique using ordinal, case-sensitive comparison.

These identifiers are different:

```text
document-read
Document-Read
```

Using different casing for similar identifiers is allowed but generally
discouraged because it makes policies harder to maintain.

### Policy route uniqueness

Only one policy may exist for the same resource type and action pair.

This is invalid:

```yaml
policies:
  - id: first-document-read-policy
    resourceType: document
    action: read
    requirement:
      permission: document.read

  - id: second-document-read-policy
    resourceType: document
    action: read
    requirement:
      role: document.reader
```

The duplicate route produces:

```text
MANIFEST_DUPLICATE_POLICY_ROUTE
```

Combine alternative conditions with `any` instead:

```yaml
policies:
  - id: document-read
    resourceType: document
    action: read
    requirement:
      any:
        - permission: document.read
        - role: document.reader
```

Resource type and action matching is ordinal and case-sensitive.

## Requirements

A requirement may contain an optional `id` and must define exactly one
requirement kind.

Supported kinds are:

- `permission`
- `role`
- `attribute`
- `all`
- `any`
- `not`

This is invalid because it defines two kinds:

```yaml
requirement:
  permission: document.read
  role: document.reader
```

It produces:

```text
MANIFEST_REQUIREMENT_KIND_INVALID
```

## Requirement identifiers

A requirement identifier is optional:

```yaml
requirement:
  id: required-document-read-permission
  permission: document.read
```

Requirement identifiers improve diagnostics and nested evaluation traces.

When an `id` member is present, it cannot be empty or whitespace.

```yaml
requirement:
  id: ""
  permission: document.read
```

This produces:

```text
MANIFEST_REQUIREMENT_ID_INVALID
```

Identifiers should be stable and meaningful within the policy.

## Permission requirements

A permission requirement requires the subject to contain one exact
permission:

```yaml
requirement:
  permission: document.read
```

Permission matching is ordinal and case-sensitive.

These values are different:

```text
document.read
Document.Read
```

A blank permission is invalid.

## Role requirements

A role requirement requires the subject to contain one exact role:

```yaml
requirement:
  role: finance.approver
```

Role matching is ordinal and case-sensitive.

A blank role is invalid.

## Attribute requirements

An attribute requirement compares one attribute with one typed literal value.

```yaml
requirement:
  attribute:
    source: resource
    name: status
    operator: equal
    valueType: string
    value: pending-approval
```

Every attribute requirement needs:

| Member | Required | Purpose |
|---|---:|---|
| `source` | Yes | Attribute model to read |
| `name` | Yes | Attribute name |
| `operator` | Yes | Comparison operator |
| `valueType` | Yes | Literal scalar type |
| `value` | Yes | Literal comparison value |

The built-in attribute requirement does not compare one attribute directly
with another attribute.

For example, it cannot directly express:

```text
Resource.ownerId equals Subject.id
```

Use a custom evaluator or a trusted, application-computed attribute for that
kind of rule.

### Attribute sources

Supported `source` values are:

| Token | Reads from |
|---|---|
| `subject` | `AuthorizationSubject.Attributes` |
| `resource` | `AuthorizationResource.Attributes` |
| `context` | `AuthorizationContext.Attributes` |

Tokens are exact and case-sensitive.

For example, `Subject` is not the same token as `subject`.

### Attribute operators

Supported operators are:

| Operator | Meaning |
|---|---|
| `equal` | Values must be equal |
| `notEqual` | Values must be different |
| `greaterThan` | Runtime value must be greater |
| `greaterThanOrEqual` | Runtime value must be greater or equal |
| `lessThan` | Runtime value must be less |
| `lessThanOrEqual` | Runtime value must be less or equal |

### Attribute value types

Supported `valueType` tokens are:

| Token | Runtime scalar kind |
|---|---|
| `nullValue` | Explicit null |
| `string` | String |
| `boolean` | Boolean |
| `number` | Invariant decimal number |
| `dateTimeOffset` | Date and time with UTC marker or numeric offset |

### Operator compatibility

`equal` and `notEqual` support every scalar value kind.

Ordering operators support only:

- `number`
- `dateTimeOffset`

This is invalid because booleans are not ordered:

```yaml
attribute:
  source: context
  name: isTrusted
  operator: greaterThan
  valueType: boolean
  value: true
```

It produces:

```text
MANIFEST_ATTRIBUTE_OPERATOR_VALUE_TYPE_INVALID
```

### String values

```yaml
attribute:
  source: subject
  name: department
  operator: equal
  valueType: string
  value: finance
```

String comparison follows RuleGate's strict scalar comparison behavior.

Do not rely on implicit casing or whitespace normalization.

### Boolean values

Use canonical YAML boolean values:

```yaml
attribute:
  source: context
  name: multiFactorAuthenticated
  operator: equal
  valueType: boolean
  value: true
```

Use lowercase `true` and `false`.

### Number values

```yaml
attribute:
  source: subject
  name: clearanceLevel
  operator: greaterThanOrEqual
  valueType: number
  value: 3
```

Integral and decimal YAML values are converted to invariant-culture decimal
values.

Example:

```yaml
attribute:
  source: resource
  name: totalAmount
  operator: lessThan
  valueType: number
  value: 1000.50
```

Scientific notation and implicit string-to-number coercion are not supported.

### Date and time values

A `dateTimeOffset` value must contain an explicit UTC marker or numeric
offset:

```yaml
attribute:
  source: context
  name: evaluationTime
  operator: lessThan
  valueType: dateTimeOffset
  value: 2026-12-31T23:59:59Z
```

A numeric offset is also valid:

```yaml
value: 2026-12-31T23:59:59+03:00
```

A local date-time value without an offset is rejected.

### Explicit null values

Use `nullValue` as the value type and explicitly provide the `value` member:

```yaml
attribute:
  source: resource
  name: parentId
  operator: equal
  valueType: nullValue
  value: null
```

The `value` member is required even for null comparison.

Omitting it is different from explicitly declaring a null value.

### Runtime attribute behavior

When the requested runtime attribute is missing, the requirement is not
satisfied.

The following conditions produce an indeterminate requirement result:

- Runtime value has an unsupported type
- Runtime value cannot be normalized
- Runtime value and literal value have incompatible scalar kinds
- Operator and scalar kind are incompatible

Both not-satisfied and indeterminate outcomes deny access through the
fail-closed engine.

## Logical requirements

Logical requirements form nested requirement trees.

### All

Every child must be satisfied:

```yaml
requirement:
  all:
    - permission: document.approve
    - role: finance.approver
```

An `all` collection must contain at least one child.

### Any

At least one child must be satisfied:

```yaml
requirement:
  any:
    - role: finance.approver
    - role: system.administrator
```

An `any` collection must contain at least one child.

### Not

The nested requirement must not be satisfied:

```yaml
requirement:
  not:
    role: document.blocked
```

`not` contains one nested requirement object.

### Nested trees

Logical requirements can be nested:

```yaml
requirement:
  all:
    - permission: document.approve
    - any:
        - role: finance.approver
        - role: system.administrator
    - not:
        attribute:
          source: resource
          name: status
          operator: equal
          valueType: string
          value: archived
```

Null child requirements are invalid and include their exact collection index
in the validation path.

## Complete manifest example

The following example combines permissions, roles, subject attributes,
resource attributes, context attributes, and logical requirements.

<!-- executable-manifest-example -->

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
      any:
        - id: direct-read-permission
          permission: document.read
        - id: document-reader-role
          role: document.reader

  - id: document-approve
    resourceType: document
    action: approve
    requirement:
      all:
        - id: approval-permission
          permission: document.approve

        - id: finance-department
          attribute:
            source: subject
            name: department
            operator: equal
            valueType: string
            value: finance

        - id: pending-approval-status
          attribute:
            source: resource
            name: status
            operator: equal
            valueType: string
            value: pending-approval

        - id: multi-factor-authentication
          attribute:
            source: context
            name: multiFactorAuthenticated
            operator: equal
            valueType: boolean
            value: true

  - id: document-delete
    resourceType: document
    action: delete
    requirement:
      all:
        - permission: document.delete
        - not:
            attribute:
              source: resource
              name: status
              operator: equal
              valueType: string
              value: archived
```

## Loading a manifest

`RuleGateManifestYamlLoader` can load YAML from text:

```csharp
using Fotbiler.RuleGate.Manifest.Loading;

var loader =
    new RuleGateManifestYamlLoader();

var result =
    loader.LoadFromText(yaml);
```

Or from a file:

```csharp
var result =
    await loader.LoadFromFileAsync(
        "rulegate.yaml",
        cancellationToken);
```

The YAML loader only deserializes the document. It does not perform RuleGate
schema validation.

Use `RuleGateManifestCompiler` for normal application workflows.

The loader:

- Uses camel-case YAML member names
- Rejects duplicate keys
- Rejects unknown properties
- Limits YAML recursion depth
- Preserves cancellation
- Returns structured load errors instead of throwing for expected file and
  YAML failures

## Compiling a manifest

Compile from text:

```csharp
using Fotbiler.RuleGate.Manifest.Compilation;

var compiler =
    new RuleGateManifestCompiler();

var result =
    compiler.CompileFromText(yaml);
```

Compile from a file:

```csharp
var result =
    await compiler.CompileFromFileAsync(
        "rulegate.yaml",
        cancellationToken);
```

Check the result before registering policies:

```csharp
if (!result.IsSuccess)
{
    foreach (var error in result.LoadErrors)
    {
        Console.Error.WriteLine(
            $"{error.Code}: {error.Message}");
    }

    foreach (var error in result.ValidationErrors)
    {
        Console.Error.WriteLine(
            $"{error.Code} at {error.Path}: " +
            error.Message);
    }

    return;
}

services
    .AddRuleGate()
    .AddPolicies(result.Policies);
```

Do not register policies when `IsSuccess` is `false`.

## Compilation result

`ManifestCompilationResult` separates:

- Compiled policies
- Load errors
- Validation errors

| Result | Policies | Load errors | Validation errors |
|---|---:|---:|---:|
| Success | Compiled collection | Empty | Empty |
| Load failure | Empty | One or more | Empty |
| Validation failure | Empty | Empty | One or more |

A failed compilation never exposes policies successfully mapped before the
error.

This all-or-nothing behavior prevents applications from starting with only a
subset of the intended authorization policy set.

## Load errors

Load errors describe file access and YAML parsing failures.

| Code | Meaning |
|---|---|
| `MANIFEST_YAML_EMPTY_CONTENT` | YAML text is empty or whitespace |
| `MANIFEST_YAML_ROOT_REQUIRED` | YAML does not contain a root object |
| `MANIFEST_YAML_INVALID` | YAML is malformed or cannot map to the manifest model |
| `MANIFEST_FILE_NOT_FOUND` | File or containing directory does not exist |
| `MANIFEST_FILE_READ_FAILED` | File cannot be read |

A `ManifestLoadError` contains:

- `Code`
- `Message`
- Optional `Line`
- Optional `Column`

Line and column information is provided when the YAML parser supplies a valid
position.

File cancellation is propagated as `OperationCanceledException`; it is not
converted into a load error.

## Validation errors

A validation error contains:

- A stable error code
- A manifest path
- A human-readable message

Example:

```text
MANIFEST_POLICY_ACTION_REQUIRED
policies[1].action
Policy action is required.
```

Paths identify the exact invalid member or nested child.

Example nested path:

```text
policies[0].requirement.all[1].attribute.operator
```

### Root and application codes

| Code | Path |
|---|---|
| `MANIFEST_UNSUPPORTED_SCHEMA_VERSION` | `schemaVersion` |
| `MANIFEST_APPLICATION_REQUIRED` | `application` |
| `MANIFEST_APPLICATION_ID_REQUIRED` | `application.id` |
| `MANIFEST_APPLICATION_NAME_REQUIRED` | `application.name` |
| `MANIFEST_POLICIES_REQUIRED` | `policies` |

### Policy codes

| Code | Typical path |
|---|---|
| `MANIFEST_POLICY_REQUIRED` | `policies[index]` |
| `MANIFEST_POLICY_ID_REQUIRED` | `policies[index].id` |
| `MANIFEST_POLICY_RESOURCE_TYPE_REQUIRED` | `policies[index].resourceType` |
| `MANIFEST_POLICY_ACTION_REQUIRED` | `policies[index].action` |
| `MANIFEST_POLICY_REQUIREMENT_REQUIRED` | `policies[index].requirement` |
| `MANIFEST_DUPLICATE_POLICY_ID` | `policies[index].id` |
| `MANIFEST_DUPLICATE_POLICY_ROUTE` | `policies[index]` |

### Requirement codes

| Code | Typical path |
|---|---|
| `MANIFEST_REQUIREMENT_ID_INVALID` | Requirement `.id` |
| `MANIFEST_REQUIREMENT_KIND_INVALID` | Requirement object |
| `MANIFEST_PERMISSION_REQUIRED` | Requirement `.permission` |
| `MANIFEST_ROLE_REQUIRED` | Requirement `.role` |
| `MANIFEST_REQUIREMENT_CHILDREN_REQUIRED` | Empty `.all` or `.any` |
| `MANIFEST_REQUIREMENT_REQUIRED` | Null logical child |

### Attribute codes

| Code | Typical path |
|---|---|
| `MANIFEST_ATTRIBUTE_SOURCE_REQUIRED` | Attribute `.source` |
| `MANIFEST_ATTRIBUTE_SOURCE_INVALID` | Attribute `.source` |
| `MANIFEST_ATTRIBUTE_NAME_REQUIRED` | Attribute `.name` |
| `MANIFEST_ATTRIBUTE_OPERATOR_REQUIRED` | Attribute `.operator` |
| `MANIFEST_ATTRIBUTE_OPERATOR_INVALID` | Attribute `.operator` |
| `MANIFEST_ATTRIBUTE_VALUE_TYPE_REQUIRED` | Attribute `.valueType` |
| `MANIFEST_ATTRIBUTE_VALUE_TYPE_INVALID` | Attribute `.valueType` |
| `MANIFEST_ATTRIBUTE_VALUE_REQUIRED` | Attribute `.value` |
| `MANIFEST_ATTRIBUTE_VALUE_INVALID` | Attribute `.value` |
| `MANIFEST_ATTRIBUTE_OPERATOR_VALUE_TYPE_INVALID` | Attribute `.operator` |

Applications may use stable codes and paths for tooling. Human-facing messages
may evolve during preview releases.

## Common mistakes

### Omitting the policies member

Invalid:

```yaml
schemaVersion: 1

application:
  id: sample
  name: Sample
```

Use an explicit empty collection when no policies exist yet:

```yaml
policies: []
```

### Defining multiple requirement kinds

Invalid:

```yaml
requirement:
  permission: document.read
  role: document.reader
```

Use `all` or `any`.

### Duplicating a policy route

Invalid:

```yaml
- id: first
  resourceType: document
  action: read
  requirement:
    permission: document.read

- id: second
  resourceType: document
  action: read
  requirement:
    role: document.reader
```

Combine the alternatives in one policy.

### Using the wrong token casing

Invalid:

```yaml
source: Subject
operator: Equal
valueType: String
```

Valid:

```yaml
source: subject
operator: equal
valueType: string
```

### Omitting an explicit null value

Invalid:

```yaml
attribute:
  source: resource
  name: parentId
  operator: equal
  valueType: nullValue
```

Valid:

```yaml
attribute:
  source: resource
  name: parentId
  operator: equal
  valueType: nullValue
  value: null
```

### Using an ordering operator with a boolean

Invalid:

```yaml
attribute:
  source: context
  name: trusted
  operator: greaterThan
  valueType: boolean
  value: true
```

Use `equal` or `notEqual` for booleans.

### Using a date without an offset

Invalid:

```yaml
valueType: dateTimeOffset
value: 2026-12-31T23:59:59
```

Valid:

```yaml
valueType: dateTimeOffset
value: 2026-12-31T23:59:59Z
```

## Security guidance

Treat manifests as security-sensitive configuration.

Validate and compile the complete document before registration. Do not
continue with partial policies when loading or validation fails.

Protect manifest provenance, deployment access, and identifier consistency.

See the [security model](security.md) for parser hardening, all-or-nothing
compilation, deployment controls, stale-policy risks, and the production
checklist.

## Current boundaries

The current manifest format supports:

- Permission requirements
- Role requirements
- Typed attribute-to-literal requirements
- Nested `all`, `any`, and `not` requirements
- Structured load errors
- Structured validation errors
- Compilation from text and files

The current format does not directly support:

- Attribute-to-attribute comparison
- Includes or imported manifest fragments
- Environment-variable substitution
- Remote manifest loading
- Watch mode
- TypeScript code generation
- Generated requirement or domain-resource models

RuleGate CLI can generate deterministic C# constants for policy IDs,
resource types, and actions. Generation consumes only a completely compiled
manifest; load, schema, structural, semantic, namespace, or identifier
collision failures produce no source.

Use:

```bash
rulegate generate csharp \
  ./rulegate.yaml \
  --namespace Sample.Authorization \
  --output Generated/RuleGate.g.cs
```

Use `--check` with the same arguments in CI to reject missing or stale output
without modifying the file. See the
[C# code-generation guide](code-generation.md) for the complete generation
contract.

## Next steps

Continue with:

- [Getting started](getting-started.md) for an executable application.
- [Authorization model](authorization-model.md) for conceptual guidance.
- The root [README](../README.md) for current ASP.NET Core integration.
- [Documentation index](README.md) for all available guides.

## Validate manifests with the RuleGate CLI

`Fotbiler.RuleGate.Cli` exposes the manifest compiler through a deterministic
command-line interface.

Install the current preview:

```bash
dotnet tool install \
  --global \
  Fotbiler.RuleGate.Cli \
  --version 0.5.0-preview.2
```

Validate `rulegate.yaml` in the current directory:

```bash
rulegate validate
```

Validate an explicit file:

```bash
rulegate validate ./policies/rulegate.yaml
```

Request machine-readable JSON:

```bash
rulegate validate --format json
```

The CLI preserves the same fail-closed guarantees as
`RuleGateManifestCompiler`:

- loading and parsing failures cannot produce policies;
- schema and semantic failures cannot produce partial policies;
- JSON mode keeps machine-readable output isolated from standard-error
  diagnostics;
- unexpected failures do not expose exception details or stack traces.

The process exit-code contract is:

| Exit code | Meaning |
|---:|---|
| `0` | Manifest is valid |
| `1` | File loading, YAML, schema, structural, or semantic validation failed |
| `2` | Command-line usage is invalid |
| `3` | An unexpected internal failure occurred |
| `130` | Validation was cancelled |

See the [RuleGate CLI guide](cli.md) for installation, validation, C#
generation, stale-output detection, automation, CI, and operational details.
