# RuleGate Authorization Model

This guide explains the concepts behind a RuleGate authorization decision.

Read [Getting started](getting-started.md) first when you need a complete,
executable example.

## The authorization question

Every RuleGate evaluation answers one question:

> May this subject perform this action on this resource under the current
> context?

RuleGate represents that question with an `AuthorizationRequest`.

```text
AuthorizationRequest
├── Subject
├── Resource
├── Action
└── Context
```

The engine selects the policy matching the resource type and action, evaluates
its requirement tree, and returns an `AuthorizationDecision`.

## Core model

```text
Subject ── performs ──> Action ── on ──> Resource
                           |
                           | under
                           v
                        Context
                           |
                           | evaluated against
                           v
                         Policy
                           |
                           | contains
                           v
                      Requirement tree
                           |
                           v
                        Decision
```

## Subject

An `AuthorizationSubject` represents the identity requesting access.

A subject can contain:

- An identifier
- Roles
- Permissions
- Attributes

Example:

```text
Subject
├── Id: user-42
├── Roles
│   └── finance.approver
├── Permissions
│   ├── document.read
│   └── document.approve
└── Attributes
    ├── department: finance
    └── clearanceLevel: 3
```

### Identifier

The identifier distinguishes the subject being evaluated.

In ASP.NET Core applications, RuleGate can map the identifier from a
`ClaimsPrincipal`. The default mapping uses the standard name-identifier
claim, but applications can configure another claim type.

### Roles

Roles describe responsibility or membership.

Examples:

```text
finance.approver
document.editor
system.administrator
```

A role requirement succeeds when the subject contains the required role.

Use roles when organizational responsibility or membership is itself part of
the authorization rule.

### Permissions

Permissions describe explicit capabilities.

Examples:

```text
document.read
document.update
document.approve
```

A permission requirement succeeds when the subject contains the required
permission.

Permissions usually provide a clearer and more stable contract than tying
application behavior directly to role names.

### Subject attributes

Subject attributes describe trusted properties of the requester.

Examples:

```text
department
organizationUnitId
clearanceLevel
employmentType
```

RuleGate does not retrieve these values from an identity provider. The
application maps trusted claims or application data into the subject.

## Resource

An `AuthorizationResource` represents the protected object or resource
category.

A resource can contain:

- A resource type
- An optional identifier
- Attributes

Example:

```text
Resource
├── Type: document
├── Id: document-1007
└── Attributes
    ├── ownerId: user-42
    ├── status: pending-approval
    └── classificationLevel: 2
```

### Resource type

The resource type identifies the category used during policy selection.

Examples:

```text
document
invoice
registry-book
organization-unit
```

Matching is ordinal and case-sensitive. These are different values:

```text
document
Document
DOCUMENT
```

Use stable identifiers and keep spelling and casing consistent across:

- Policy manifests
- Application code
- Endpoint metadata
- Tests
- Generated constants

### Resource identifier

The identifier distinguishes one resource instance from another.

Examples:

```text
document-1007
invoice-2026-0042
unit-17
```

An identifier is not always required. A create operation may be authorized
before the new resource has an identifier.

### Resource attributes

Resource attributes describe trusted properties of the protected object.

Examples:

```text
status
department
classificationLevel
ownerId
parentId
```

The application is responsible for loading the domain object and mapping its
trusted values into the authorization resource.

## Action

The action describes the operation the subject wants to perform.

Examples:

```text
read
create
update
delete
approve
dispatch
```

Action matching is ordinal and case-sensitive.

Prefer stable business operations:

```text
approve
```

instead of transport-level names:

```text
post
```

The same business action can be exposed through different endpoints without
changing its authorization identifier.

## Context

An `AuthorizationContext` contains evaluation-specific information.

It includes the evaluation time and can also contain attributes.

Example:

```text
Context
├── Evaluation time: 2026-07-27T12:00:00Z
└── Attributes
    ├── authenticationMethod: mfa
    ├── networkZone: internal
    └── requestChannel: web
```

Context attributes are appropriate for temporary or request-specific
conditions such as:

- Authentication method
- Authentication time and MFA time
- Network zone
- Request channel
- Tenant and organization
- Trusted-device state
- User or service identity type
- Operational mode

RuleGate defines canonical names for the built-in context properties and
timestamps through `AuthorizationContextAttributeNames`. Applications must
populate these attributes from trusted server-side state; RuleGate never
infers them from headers, IP addresses, or arbitrary claims.

ASP.NET Core applications can populate subject, resource, and context
attributes through the ordered, fail-closed
[attribute enrichment pipeline](enrichment.md). The pipeline standardizes how
trusted application services contribute data; it does not make an untrusted
source authoritative.

Long-lived requester properties belong on the subject. Long-lived object
properties belong on the resource.

## Policy

A policy connects a protected operation to a requirement.

Each policy defines:

- A policy identifier
- A resource type
- An action
- A root requirement

Conceptually:

```text
Policy
├── Id: document-read
├── Resource type: document
├── Action: read
└── Requirement
    └── Permission: document.read
```

Equivalent YAML:

```yaml
policies:
  - id: document-read
    resourceType: document
    action: read
    requirement:
      permission: document.read
```

The policy identifier supports maintenance and diagnostics. Resource type and
action determine which policy applies to the request.

## Requirements

A requirement defines a condition that must be satisfied.

RuleGate currently provides these built-in requirement categories:

| Requirement          | Purpose                                                                  |
| -------------------- | ------------------------------------------------------------------------ |
| Permission           | Require a subject permission                                             |
| Role                 | Require a subject role                                                   |
| Attribute            | Compare a subject, resource, or context attribute with a typed literal   |
| Attribute comparison | Compare two attribute or typed-literal operands                          |
| Time window          | Require configured days and local clock hours in an explicit time zone   |
| Date-time window     | Require an instant to be before, after, or between UTC-normalized bounds |
| Context age          | Limit the age of authentication or MFA                                   |
| Context              | Check a canonical request or identity context property                   |
| `all`                | Require every child requirement                                          |
| `any`                | Require at least one child requirement                                   |
| `not`                | Negate one child requirement                                             |

### Permission

```yaml
requirement:
  permission: document.read
```

### Role

```yaml
requirement:
  role: finance.approver
```

### Attribute

```yaml
requirement:
  attribute:
    source: resource
    name: status
    operator: equal
    valueType: string
    value: pending-approval
```

The built-in attribute requirement reads an attribute from:

- `subject`
- `resource`
- `context`

It checks attribute state or compares the attribute with a typed scalar or
collection literal declared in the policy.

Supported examples:

```text
Resource.status equals pending-approval
Subject.clearanceLevel greaterThanOrEqual 3
Context.authenticationMethod equals mfa
Subject.department startsWith finance
Subject.permissions containsAll [document.read, document.approve]
Resource.ownerId exists
```

String comparison is ordinal and case-sensitive by default. Policies may
explicitly select ordinal case-insensitive comparison. Collection values are
homogeneous, cannot contain null or nested collections, and are limited to 256
elements.

Use an attribute comparison requirement when both values must be resolved at
evaluation time:

```yaml
requirement:
  attributeComparison:
    left:
      source: resource
      name: ownerId
    operator: equal
    right:
      source: subject
      name: id
```

The equivalent programmatic definition is:

```csharp
new AttributeComparisonRequirementDefinition(
    AuthorizationAttributeOperand.Resource("ownerId"),
    AuthorizationAttributeOperator.Equal,
    AuthorizationAttributeOperand.Subject("id"));
```

Either operand may reference subject, resource, or context attributes, or a
typed literal. The comparison uses the same strict scalar, collection,
numeric, date/time, and ordinal string rules as the built-in attribute
requirement. Missing, unsupported, and incompatible values deny access.

The [manifest reference](manifests.md#attribute-comparison-requirements)
documents the complete operand and operator surface.

### Time and date-time windows

A `timeWindow` expresses recurring local hours with an explicit time zone:

```yaml
requirement:
  timeWindow:
    days: [monday, tuesday, wednesday, thursday, friday]
    start: '08:00'
    end: '18:00'
    timeZone: Europe/Istanbul
```

The start is inclusive and the end is exclusive. A start later than the end
creates an overnight window, so Friday `22:00` to `02:00` includes early
Saturday. The listed day identifies the day on which the window starts.

A `dateTimeWindow` expresses one-time before, after, or bounded rules:

```yaml
requirement:
  dateTimeWindow:
    startsAt: '2026-07-29T09:00:00Z'
    endsAt: '2026-08-01T18:00:00+03:00'
```

At least one boundary is required. `startsAt` is inclusive, `endsAt` is
exclusive, and both require an explicit UTC marker or numeric offset.

Both requirements evaluate `AuthorizationContext.EvaluationTime`. ASP.NET
Core creates it from the registered `IRuleGateClock`. The default registration
uses system UTC time, while applications and tests can replace the clock
through the RuleGate-owned interface for deterministic boundary verification.

### Context age

`contextAge` limits how long an authentication event remains acceptable:

```yaml
requirement:
  contextAge:
    timestamp: mfa
    maximumAge: '00:15:00'
```

The supported timestamp tokens are `authentication` and `mfa`. They read the
canonical `authenticationTime` and `multiFactorAuthenticationTime` context
attributes as `DateTimeOffset` values. A missing timestamp is not satisfied;
an incompatible or future timestamp is indeterminate. Both deny access.

### Canonical context policies

A `context` requirement checks a defined request or identity property:

```yaml
requirement:
  context:
    property: networkZone
    operator: in
    valueType: stringCollection
    value: [internal, vpn]
```

Supported properties are `authenticationMethod`, `requestChannel`,
`networkZone`, `tenantId`, `organizationId`, `trustedDevice`, and
`identityType`. `trustedDevice` is a boolean and accepts only `equal` or
`notEqual`. The other properties are strings and accept equality, string
matching, or membership operations. Missing or untrusted values never receive
defaults.

### Logical composition

Requirements can form nested trees.

Require both a permission and a department:

```yaml
requirement:
  all:
    - permission: document.approve
    - attribute:
        source: subject
        name: department
        operator: equal
        valueType: string
        value: finance
```

Allow either of two roles:

```yaml
requirement:
  any:
    - role: finance.approver
    - role: system.administrator
```

Reject archived resources:

```yaml
requirement:
  not:
    attribute:
      source: resource
      name: status
      operator: equal
      valueType: string
      value: archived
```

Logical composition allows several authorization approaches to participate in
one decision.

## Authorization approaches

RuleGate uses one policy model for multiple authorization approaches.

| Approach                       | Example                                   |
| ------------------------------ | ----------------------------------------- |
| Permission-based               | Subject has `document.read`               |
| Role-based access control      | Subject has `finance.approver`            |
| Attribute-based access control | Subject department equals `finance`       |
| Context-based access control   | Authentication method equals `mfa`        |
| Resource-based authorization   | Resource status equals `pending-approval` |

A policy can combine them:

```text
Allow document approval when:

- The subject has document.approve
- The subject belongs to finance
- The resource is pending approval
- The request used multi-factor authentication
```

Applications do not need to choose only one authorization model.

## Policy selection

The authorization engine follows this flow:

```text
1. Receive AuthorizationRequest
2. Match resource type and action
3. Load the matching policy
4. Evaluate the requirement tree
5. Produce AuthorizationDecision
```

Resource type and action matching is exact, ordinal, and case-sensitive.

When no matching policy exists, access is denied.

RuleGate does not guess a policy, normalize identifiers, or silently fall back
to a broader rule.

## Evaluation behavior

Requirement evaluation is fail-closed.

Access is not granted when:

- A matching policy does not exist
- A required value is missing
- A requirement type is unsupported
- A value cannot be normalized safely
- A custom evaluator cannot produce a valid result
- Evaluation becomes indeterminate

Logical requirements preserve the same behavior. An unevaluable branch does
not silently become successful.

## Decision

An `AuthorizationDecision` reports whether access is allowed.

```text
AuthorizationDecision
├── IsAllowed
└── Failures
```

A denied decision can include failure information for trusted application,
testing, and diagnostic boundaries.

Failure details should not automatically be returned to external API clients.
They may reveal:

- Policy structure
- Requirement identifiers
- Roles or permissions
- Subject attributes
- Resource attributes

ASP.NET Core HTTP-result mapping therefore uses generic public `401` and `403`
responses.

## Diagnostics

Diagnostics are optional and do not change the authorization result.

They can provide:

- Requirement identifiers
- Parent-child relationships
- Evaluation outcome
- Failure information
- Evaluation duration

Diagnostic data is intended for trusted logs and observability systems. Raw
subject, resource, claim, role, permission, and policy data should not be
exposed by default.

## Identity providers

RuleGate does not authenticate users and does not manage identity-provider
data.

An identity provider may supply:

- Subject identifier
- Roles
- Permissions
- Claims used as attributes

The application maps trusted identity data into an
`AuthorizationSubject`. RuleGate then evaluates that subject against local
policies.

This keeps the core authorization engine independent from providers such as:

- Keycloak
- Microsoft Entra ID
- Auth0
- Custom identity systems

Provider-specific helpers can simplify mapping, but they must remain optional.

## Frontend authorization

Frontend permission, policy, and role checks improve user experience by hiding
or disabling unavailable actions.

They are not a security boundary.

Every protected backend operation must perform its own authorization
evaluation using trusted subject, resource, and context data.

## Modeling guidance

### Use stable identifiers

Prefer:

```text
document.read
document.approve
finance.approver
```

Avoid display labels:

```text
Read Document
Finance Approver
```

Display text can change without changing the authorization contract.

### Keep policies business-oriented

Prefer:

```text
resourceType: document
action: approve
```

Avoid identifiers tied directly to:

- Controller names
- Endpoint paths
- UI pages
- Button labels
- HTTP methods

### Put values on the correct model

Use:

- Subject attributes for requester properties
- Resource attributes for protected-object properties
- Context attributes for request-specific properties

Do not duplicate values merely to make a policy easier to write.

### Keep inputs trustworthy

RuleGate evaluates the values provided by the application.

Do not construct trusted authorization attributes directly from
user-controlled request values without validation.

Examples of trusted sources include:

- Validated identity claims
- Server-side domain entities
- Trusted application configuration
- Server-generated request context

### Deny incomplete mappings

When required subject, resource, or context data cannot be mapped reliably,
deny the operation.

Do not replace missing values with broad defaults that could accidentally
satisfy a policy.

### Use custom evaluators deliberately

A custom evaluator is appropriate when a rule requires:

- Domain service access
- Hierarchy traversal
- Specialized temporal logic
- Application-specific decision semantics

Custom evaluators must preserve cancellation, deterministic behavior,
structured failures, and fail-closed evaluation.

## RuleGate 1.0 boundaries

RuleGate 1.0 includes:

- Permission requirements
- Role requirements
- Typed attribute-to-literal comparison
- Attribute-to-attribute comparison
- Explicit-time-zone and bounded date-time requirements
- Authentication-age, MFA-age, and canonical context requirements
- Logical requirement trees
- YAML manifest compilation
- ASP.NET Core integration
- Diagnostics and logging
- Generic HTTP authorization-result mapping
- Deterministic CLI manifest validation with text and JSON output
- Deterministic host-independent policy fixtures with allow, deny,
  indeterminate, and failure-code expectations
- Deterministic C# constants generated from manifest identifiers
- Deterministic TypeScript constants generated from manifest identifiers
- Angular authorization helpers
- Optional Keycloak claim-normalization helpers
- Exporter-neutral OpenTelemetry activities and low-cardinality metrics

The following areas are planned separately:

- Domain resource mapping helpers
- Decision visualization

See the [roadmap](roadmap.md) for milestone planning.

## Next steps

Continue with:

- [Getting started](getting-started.md) for an executable example.
- The root [README](../README.md) for current ASP.NET Core usage.
- The [Keycloak integration guide](keycloak.md) for optional provider mapping.
- [Policy testing](policy-testing.md) for executable authorization examples.
- [Documentation index](README.md) for all available guides.
- [Roadmap](roadmap.md) for upcoming capabilities.
