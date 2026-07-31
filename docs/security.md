# RuleGate Security Model

This guide describes the runtime and integration security model of RuleGate.

It explains:

- The backend authorization boundary
- Default-deny and fail-closed behavior
- Requirement outcomes
- Identity, resource, and context trust boundaries
- Manifest security
- ASP.NET Core policy and HTTP response behavior
- Diagnostics and audit boundaries
- Custom extension responsibilities
- Cancellation and exception behavior
- Production deployment and testing guidance

This document explains secure RuleGate usage. It is not a vulnerability
reporting policy.

For concrete ASP.NET Core APIs, read the
[ASP.NET Core integration guide](aspnetcore.md).

For policy syntax, read the [manifest guide](manifests.md).

For diagnostic fields and sinks, read the
[diagnostics guide](diagnostics.md).

For client-side route and template APIs, read the
[Angular SDK guide](angular.md).

For package-consuming compositions and concrete trust boundaries, read the
[reference applications guide](reference-applications.md).

## Security boundary

The protected backend operation is the security boundary.

RuleGate authorization must run in a trusted backend process before that
operation is allowed to execute.

Client-side checks may:

- Hide unavailable controls
- Improve navigation
- Avoid obviously invalid requests
- Explain expected access requirements

They cannot:

- Protect an API
- Replace backend authorization
- Make browser state trustworthy
- Prevent a modified client from sending a request
- Grant authority based on UI visibility

Every protected operation must enforce authorization on the backend.

## Responsibility model

RuleGate evaluates the authorization request supplied by the application.

```text
Authenticated identity
        |
        v
Application mapping
        |
        v
AuthorizationSubject
        |
        +---------------------+
        |                     |
        v                     v
AuthorizationResource   AuthorizationContext
        |                     |
        +----------+----------+
                   |
                   v
          RuleGate policy engine
                   |
                   v
             Allow or deny
```

RuleGate is responsible for:

- Exact policy-route lookup
- Requirement dispatch
- Built-in requirement evaluation
- Logical requirement composition
- Structured failure information
- Default-deny decisions
- Optional diagnostics

The host application is responsible for:

- Authentication
- Token and issuer validation
- Claims transformation
- Subject mapping
- Domain-resource loading
- Resource attribute mapping
- Context attribute mapping
- Trusted time configuration
- Public response design
- Audit logging
- Deployment and policy provenance

RuleGate cannot determine whether application-supplied authorization data is
truthful. It evaluates that data according to the registered policy.

## Default deny

RuleGate grants access only when a matching policy exists and its root
requirement is satisfied.

No matching policy produces:

```text
RULEGATE_NO_MATCHING_POLICY
```

and a denied authorization decision.

Conceptually:

```text
No matching policy       -> deny
Satisfied requirement    -> allow
NotSatisfied requirement -> deny
Indeterminate requirement -> deny
```

An empty policy collection is valid configuration, but every authorization
request is denied because no policy can match.

## Fail-closed behavior

Fail-closed means incomplete, unsupported, malformed, or unreliable
authorization input cannot grant access.

RuleGate denies when:

- No matching policy exists
- A required permission is missing
- A required role is missing
- A required attribute is missing
- An attribute comparison returns false
- An attribute type is unsupported
- Runtime and policy attribute types differ
- The operator cannot evaluate the scalar kind
- No evaluator exists for a requirement type
- Subject mapping fails
- Resource mapping fails
- A required enrichment provider cannot resolve trusted data
- An enrichment provider throws, returns invalid data, or collides by default
- Enrichment is cancelled
- Required endpoint metadata is absent
- Required route data is absent or empty
- Policy and mapped resource types differ

Fail-closed behavior does not mean every programming error is converted into a
normal deny result.

Unexpected exceptions are discussed separately in
[Exception behavior](#exception-behavior).

## Requirement outcomes

A requirement evaluation has three possible outcomes.

| Outcome         | Meaning                                             | Final authorization |
| --------------- | --------------------------------------------------- | ------------------- |
| `Satisfied`     | The requirement passed                              | May allow           |
| `NotSatisfied`  | The requirement evaluated normally but did not pass | Deny                |
| `Indeterminate` | The requirement could not produce a reliable answer | Deny                |

A non-successful requirement result must contain at least one structured
failure.

## NotSatisfied

`NotSatisfied` means RuleGate understood the requirement and its inputs, but
the required condition was false.

Examples:

- Permission is absent
- Role is absent
- Attribute is absent
- Attribute value differs from the expected literal
- A negated child requirement was satisfied

Typical failure codes include:

```text
RULEGATE_MISSING_PERMISSION
RULEGATE_MISSING_ROLE
RULEGATE_ATTRIBUTE_NOT_FOUND
RULEGATE_ATTRIBUTE_COMPARISON_NOT_SATISFIED
RULEGATE_NEGATED_REQUIREMENT_SATISFIED
```

## Indeterminate

`Indeterminate` means RuleGate cannot safely determine whether the requirement
is satisfied.

Examples:

- No evaluator is registered for the requirement type
- Runtime attribute type is unsupported
- Runtime and expected scalar kinds differ
- The operator does not support the scalar kind

Typical failure codes include:

```text
RULEGATE_REQUIREMENT_EVALUATOR_NOT_FOUND
RULEGATE_ATTRIBUTE_TYPE_NOT_SUPPORTED
RULEGATE_ATTRIBUTE_TYPE_MISMATCH
RULEGATE_ATTRIBUTE_OPERATOR_NOT_SUPPORTED
```

Indeterminate never grants access.

Do not reinterpret indeterminate as success in application code.

## Logical requirement security

Logical requirements preserve fail-closed behavior.

### All

`all` evaluates every child.

The result is:

1. `NotSatisfied` when at least one child is not satisfied.
2. Otherwise `Indeterminate` when at least one child is indeterminate.
3. Otherwise `Satisfied`.

An indeterminate child cannot make `all` succeed.

### Any

`any` succeeds immediately when one child is satisfied.

When no child succeeds:

1. The result is `Indeterminate` when at least one child is indeterminate.
2. Otherwise the result is `NotSatisfied`.

This means an indeterminate alternative cannot be treated as a normal false
value when no safe alternative succeeds.

### Not

`not` behaves as follows:

| Child outcome   | `not` outcome   |
| --------------- | --------------- |
| `Satisfied`     | `NotSatisfied`  |
| `NotSatisfied`  | `Satisfied`     |
| `Indeterminate` | `Indeterminate` |

Negation does not turn uncertainty into success.

This is important for rules such as:

```text
not blocked
```

When RuleGate cannot determine whether the subject is blocked, access remains
denied.

## Exact matching

RuleGate uses ordinal, case-sensitive matching for security-relevant
identifiers.

This includes:

- Policy identifiers
- Resource types
- Actions
- Roles
- Permissions
- Attribute names
- Dynamic policy-name segments
- Claim types and mapped claim values

These are different values:

```text
document.read
Document.Read
DOCUMENT.READ
```

These resource types are also different:

```text
document
Document
```

RuleGate does not:

- Convert identifiers to lowercase
- Trim meaningful values automatically
- Apply culture-sensitive comparison
- Guess intended spelling
- Select a broader policy as fallback

Use stable identifiers and enforce casing conventions in source control and
tests.

## Policy-route uniqueness

Only one policy may exist for the same resource type and action pair.

```text
(resourceType, action)
```

Policy IDs must also be unique.

Duplicate IDs or routes are rejected by manifest validation and by the
in-memory policy provider.

This prevents registration order from silently deciding which policy wins.

## Authentication boundary

RuleGate is an authorization framework. It does not authenticate users.

The application must validate:

- Token signature
- Issuer
- Audience
- Expiration
- Authentication scheme
- Session validity
- Identity-provider-specific security requirements

Dynamic ASP.NET Core RuleGate policies require an authenticated principal.

An anonymous request is challenged before RuleGate can grant access.

A valid authentication result does not automatically imply authorization.

## ClaimsPrincipal trust boundary

The default ASP.NET Core subject factory maps:

- Subject ID
- Roles
- Permissions

Default claim types are:

| Value      | Default claim type          |
| ---------- | --------------------------- |
| Subject ID | `ClaimTypes.NameIdentifier` |
| Role       | `ClaimTypes.Role`           |
| Permission | `permission`                |

The configured authentication and claims-transformation pipeline must ensure
these claims are trustworthy.

Do not map authorization claims from:

- Unverified headers
- Client-controlled form values
- Unsigned tokens
- Untrusted query-string values
- Unvalidated external profile data

## Subject identifier rules

The default factory requires exactly one distinct, non-empty subject
identifier.

The following conditions fail closed:

- Subject identifier claim is missing
- Every subject identifier value is blank
- Multiple distinct subject identifiers exist

Repeated claims containing the same exact identifier are treated as one
identifier.

Example accepted values:

```text
user-42
user-42
```

Example rejected values:

```text
user-42
user-43
```

Ambiguous identity must never be resolved by selecting the first claim.

## Role and permission mapping

Role and permission claims:

- Ignore empty or whitespace-only values
- Remove exact duplicates
- Preserve case-distinct values
- Use ordinal comparison

For example:

```text
document.read
Document.Read
```

remain two distinct permissions.

Ensure the configured role and permission claim types match the identity
provider's validated token contract.

## Claims are not attributes

The default subject factory does not copy arbitrary claims into
`AuthorizationSubject.Attributes`.

For example, this claim:

```text
department = finance
```

does not automatically satisfy:

```yaml
attribute:
  source: subject
  name: department
  operator: equal
  valueType: string
  value: finance
```

A custom `IRuleGateSubjectFactory` must explicitly map the trusted claim.

Explicit mapping prevents every incoming claim from silently becoming
authorization input.

## Custom subject factories

A custom subject factory must:

- Accept only authenticated and validated identity data
- Produce one stable subject identifier
- Normalize provider-specific claims deliberately
- Avoid granting permissions from display-only claims
- Map attributes using documented scalar types
- Reject ambiguous identity
- Fail closed when required data is missing
- Be safe for singleton registration
- Be thread-safe

The ASP.NET Core handler converts `ArgumentException` and
`InvalidOperationException` thrown during subject mapping into authorization
failure.

Other unexpected exception types propagate.

## Resource trust boundary

Resource attributes must describe the protected server-side object.

Trusted sources include:

- A domain entity loaded by the backend
- A repository result
- An internal service response with established trust
- Server-derived ownership or hierarchy information
- Validated immutable metadata

Avoid treating these as authoritative resource data:

- Request-body ownership fields
- Client-supplied status values
- Hidden form fields
- Query-string classifications
- Browser-local state
- Unverified headers

A client may identify the target resource, but the backend must load and map
the authoritative object state.

## Default HTTP resource mapping

The default ASP.NET Core resource factory maps:

- Resource type from RuleGate endpoint metadata
- Optional resource ID from one configured route value

It does not:

- Load a domain entity
- Verify that the entity exists
- Read resource attributes
- Validate ownership
- Query a database
- Copy arbitrary request values
- Map request headers into authorization attributes

A route identifier is an identifier, not proof of ownership or access.

## Endpoint metadata validation

For endpoint authorization, RuleGate requires metadata matching the current:

- Resource type
- Action

The following conditions fail closed:

- No endpoint is resolved
- Matching RuleGate metadata is absent
- Matching metadata disagrees about the resource-ID route key
- Required route value is missing
- Required route value is null
- Required route value converts to an empty string
- Mapped resource type differs from the policy resource type

The RuleGate engine is not evaluated until subject and resource mapping
succeed.

## Custom resource factories

A custom `IRuleGateAuthorizationResourceFactory` may load or map application
domain resources.

It must:

- Use trusted server-side data
- Preserve endpoint mapping when endpoint helpers are used
- Return the correct resource type
- Map stable resource identifiers
- Avoid trusting client-supplied authorization attributes
- Reject unsupported resource types
- Fail closed on incomplete mapping
- Be thread-safe for singleton registration

Resource type consistency is enforced before the engine is evaluated.

## Context trust boundary

`AuthorizationContext` contains:

- Evaluation time
- Optional context attributes

The default ASP.NET Core handler supplies evaluation time from the registered
`TimeProvider`.

It does not automatically map:

- Request headers
- IP addresses
- Authentication method
- Device information
- Tenant information
- Network zone
- Risk score
- Correlation data

Applications requiring these values must construct trusted context attributes
explicitly or supply them through a trusted ASP.NET Core context attribute
provider.

## ASP.NET Core enrichment trust boundary

The optional ASP.NET Core enrichment pipeline provides standard extension
points for trusted subject, resource, and context attributes.

The pipeline does not make request-derived values trustworthy. A provider must
validate and normalize data before returning it. Do not directly promote an
arbitrary header, query value, route value, remote address, claim, cookie, or
device assertion into authorization state.

Providers run sequentially in subject, resource, and context stages. Lower
`Order` values run first; equal values retain dependency-injection registration
order. Existing attributes and earlier providers establish the initial
precedence.

Duplicate keys fail closed by default. `KeepExisting` and `ReplaceExisting`
must be selected explicitly and tested as security-relevant precedence rules.

The pipeline fails authorization before engine evaluation when:

- A provider reports missing required data
- A provider reports failure
- A provider throws
- Cancellation is requested
- An attribute name is empty or whitespace
- A provider returns an unsupported attribute value
- A duplicate key uses the default collision behavior

Provider exception messages, attribute names, and attribute values are not
copied into built-in enrichment logs.

For implementation guidance, read the
[ASP.NET Core enrichment guide](enrichment.md).

## Time security

Time-based authorization depends on a trustworthy clock.

The default ASP.NET Core registration uses:

```text
TimeProvider.System
```

Applications replacing `TimeProvider` must ensure that production time cannot
be influenced by an untrusted caller.

A test clock is useful in automated tests but must not accidentally replace
the production clock.

Use `DateTimeOffset` values with explicit offsets.

First-class time requirements read only
`AuthorizationContext.EvaluationTime`. Recurring windows require an explicit
time zone, while one-time boundaries require explicit offsets and are
normalized to UTC. Starts are inclusive and ends are exclusive. Overnight
windows associate their early-morning portion with the preceding configured
day.

Time-zone identifiers and daylight-saving rules come from the platform time
zone database. Keep production images and hosts updated, and test business
boundaries around clock transitions when a selected zone observes daylight
saving time.

Authentication-age and MFA-age requirements read canonical `DateTimeOffset`
attributes from the authorization context. A missing timestamp is not
satisfied. An incompatible type or a timestamp later than evaluation time is
indeterminate. These outcomes deny access and prevent absent or implausible
authentication history from granting authorization.

## First-class context policies

RuleGate defines canonical context properties for authentication method,
request channel, network zone, tenant, organization, trusted-device state, and
identity type. This standardizes policy vocabulary; it does not establish
trust in the value.

The default ASP.NET Core integration does not derive these properties from
headers, forwarded IP values, claims, cookies, or device assertions.
Applications must validate the source and explicitly construct
`AuthorizationContext.Attributes`. Missing properties deny the requirement.
Never copy client-controlled input into a canonical context attribute merely
because its name matches the policy.

## Attribute trust boundary

Supported runtime attribute values are:

- `null`
- `string`
- `bool`
- Integral numeric types
- `decimal`
- `DateTimeOffset`
- Homogeneous collections of supported non-null scalar values

Collections cannot be nested, cannot contain null elements, and cannot exceed
256 elements.

Runtime values outside the supported set produce an indeterminate result.

Examples of unsupported runtime inputs include application-specific objects
that have not been mapped into a supported value and heterogeneous or nested
collections.

RuleGate does not serialize arbitrary objects to create authorization values.

## Attribute comparison security

Attribute comparison is strict.

RuleGate does not perform:

- Culture-sensitive string comparison
- Implicit string-to-number conversion
- Implicit string-to-boolean conversion
- Arbitrary object conversion
- Culture-sensitive numeric parsing

String matching is ordinal and case-sensitive by default. A policy must
explicitly select `ordinalIgnoreCase` when case-insensitive comparison is
required.

Missing and null attributes remain distinct. `exists` is satisfied for any
present key, including a null value. `notExists` is satisfied only for a missing
key. `isNull` and `isNotNull` require the key to be present.

A missing required attribute is `NotSatisfied`.

An unsupported value, type mismatch, or unsupported operator/type combination
is `Indeterminate`.

Both deny access.

## Cross-attribute rules

The built-in attribute-comparison evaluator can compare trusted subject,
resource, and context attributes directly:

```text
Resource.ownerId equals Subject.id
```

Both operands are normalized through the same strict authorization-value
model. Missing attributes are `NotSatisfied`; unsupported values and
incompatible operand kinds are `Indeterminate`. Both outcomes deny access.

Do not populate either operand from an untrusted client assertion. Subject
attributes must come from validated identity data, resource attributes from
authoritative domain data, and context attributes from trusted server-side
state.

Diagnostics may identify both operand sources and names for custom sinks, but
never contain their resolved values or typed policy literal values. The
built-in logging sink omits attribute names.

## Dynamic ASP.NET Core policy names

RuleGate dynamic policy names use:

```text
RuleGate:<resource-type>:<action>
```

Each segment must:

- Be non-empty
- Contain no whitespace
- Contain no `:` separator

The prefix, resource type, and action are case-sensitive.

Example:

```text
RuleGate:document:read
```

## Owned-prefix protection

Ordinary policy names are delegated to ASP.NET Core's standard policy
provider.

Malformed names beginning with the owned prefix:

```text
RuleGate:
```

do not fall back to ordinary policies.

Examples:

```text
RuleGate:document:
RuleGate::read
RuleGate:document:read:extra
```

remain unresolved.

This prevents a malformed RuleGate policy reference from silently resolving
through a different authorization mechanism.

## Manifest security boundary

Treat `rulegate.yaml` as security-sensitive configuration.

A manifest can change who may perform protected operations.

Protect it with:

- Source control review
- Restricted write permissions
- Deployment provenance
- Integrity controls
- Environment promotion rules
- Automated validation
- Allowed and denied regression tests

Do not generate production manifests from untrusted runtime input.

## Declarative format

RuleGate manifests declare policy data.

They do not execute:

- Scripts
- Shell commands
- Embedded C#
- Remote code
- Arbitrary expressions

This reduces the execution surface, but a valid manifest can still create an
incorrect authorization policy.

Semantic review remains necessary.

## YAML parser hardening

The current loader:

- Rejects empty content
- Requires a root object
- Rejects malformed YAML
- Rejects duplicate keys
- Rejects unknown properties
- Limits YAML recursion to 64 levels
- Returns structured file and YAML errors
- Propagates cancellation

These controls prevent ambiguous or unexpectedly deep documents from silently
compiling.

They do not replace file-system and deployment security.

## Manifest validation

Validation rejects, among other conditions:

- Unsupported schema versions
- Missing application metadata
- Missing policies collection
- Null policy entries
- Missing policy identifiers
- Duplicate policy identifiers
- Missing resource types
- Missing actions
- Duplicate policy routes
- Missing requirements
- Multiple requirement kinds in one object
- Empty logical collections
- Invalid attribute source tokens
- Invalid operators
- Invalid scalar types
- Missing literal values
- Invalid operator/type combinations

Use `RuleGateManifestCompiler` for normal workflows rather than using only the
YAML loader.

## All-or-nothing compilation

Manifest compilation is atomic from the consumer's perspective.

| Result             |          Compiled policies |
| ------------------ | -------------------------: |
| Success            | Complete policy collection |
| Load failure       |                      Empty |
| Validation failure |                      Empty |

A failed compilation never returns a partial policy set.

Do not:

- Register policies when `IsSuccess` is false
- Continue startup with only successfully parsed entries
- Silently reuse an unknown stale manifest
- Ignore validation errors in deployment

A safe startup strategy is to fail application startup or fail deployment when
the intended policy set cannot be compiled.

## Policy-test fixtures

`rulegate test` compiles the complete fixture and referenced manifest before it
evaluates any request. Fixture validation rejects malformed YAML, duplicate
keys and identifiers, unsupported types, missing request boundaries, invalid
expectations, and evaluation times without an explicit offset.

Fixtures must contain synthetic or otherwise safe test data. Text and JSON
reports include test identifiers, descriptions, outcomes, policy identifiers,
and failure codes, but never subject, resource, or context attribute values.

The command evaluates the portable policy model only. It does not execute
identity-provider validation, ASP.NET Core enrichment providers, endpoint
handlers, or application data access. Continue to test those host trust
boundaries separately, and never treat a CLI fixture as a replacement for API
enforcement.

## Policy replacement and stale configuration

RuleGate does not currently provide manifest hot reload.

Applications implementing their own reload mechanism must avoid windows where:

- Old and new policies are mixed
- Only part of the new policy set is active
- A failed reload removes the last known valid policy unintentionally
- Different application instances evaluate different policy generations

Use atomic replacement and record the active policy version.

## HTTP authorization results

ASP.NET Core keeps normal authentication and authorization semantics:

| Situation                    | Framework result  |
| ---------------------------- | ----------------- |
| Anonymous protected request  | Challenge         |
| Authenticated denied request | Forbid            |
| Allowed request              | Endpoint executes |

RuleGate's ProblemDetails mapping is opt-in.

Enable it with:

```csharp
builder.Services
    .AddRuleGate()
    .AddHttpAuthorizationResultMapping()
    .AddPolicies(policies);
```

## Safe public ProblemDetails

The default RuleGate mapping returns generic public identifiers.

Authentication-required code:

```text
RULEGATE_AUTHENTICATION_REQUIRED
```

Forbidden code:

```text
RULEGATE_ACCESS_FORBIDDEN
```

The response may contain:

- Generic type URI
- Generic title
- HTTP status
- Generic detail
- Public problem code
- Trace ID

It intentionally excludes:

- Engine failure codes
- Policy IDs
- Requirement IDs
- Claims
- Role values
- Permission values
- Subject IDs
- Resource IDs
- Route values
- Attribute names
- Attribute values
- Requirement trees

Do not serialize `AuthorizationDecision.Failures` directly to an untrusted
client.

## Challenge and forbid behavior

The configured authentication scheme performs its challenge or forbid
operation before RuleGate writes the optional generic ProblemDetails body.

Authentication headers such as:

```text
WWW-Authenticate
```

are preserved.

When the authentication handler has already started the response, RuleGate
does not replace the response body.

A custom `IAuthorizationMiddlewareResultHandler` registered earlier is
preserved by the opt-in RuleGate registration.

## Trace identifiers

The public ProblemDetails response contains a trace identifier.

Treat trace IDs as correlation data, not authorization proof.

Do not allow possession of a trace ID to:

- Grant access
- Reveal protected logs
- Bypass log-access controls
- Select another user's diagnostic data

Operational tools must still enforce authentication and authorization.

## Diagnostics security

Authorization diagnostics are disabled by default.

The built-in logging sink omits:

- Attribute names
- Attribute values
- Subject identifiers
- Resource identifiers
- Raw claims
- Role values
- Permission values
- Raw authorization requests
- Subject attributes
- Resource attributes
- Context attributes

It may include:

- Policy IDs
- Requirement IDs
- Failure codes
- Evaluation outcomes
- Durations
- Requirement structure
- Attribute source

These fields can still reveal application behavior.

Store diagnostics only in trusted operational systems.

See the [diagnostics guide](diagnostics.md) for the complete field contract.

## Custom diagnostics sinks

A custom sink receives more structured information than the built-in logger
emits.

In particular, a requirement diagnostic can contain an attribute name.

A custom sink must decide which fields are safe to export.

It should be:

- Thread-safe
- Bounded
- Non-blocking where practical
- Protected from untrusted access
- Careful with retention
- Careful with cardinality
- Resilient to destination failures

Diagnostic sink exceptions are isolated and cannot change the authorization
decision.

## Diagnostics are not an audit trail

Diagnostics describe completed RuleGate evaluations.

They are not guaranteed for:

- Canceled evaluations
- Unexpected engine failures
- Process termination
- Storage outages
- Requests failing before authorization
- Business operations failing after authorization

Diagnostics also omit much of the subject and resource data normally required
for compliance records.

Use a separate durable, application-owned audit mechanism.

## Custom requirement evaluators

Custom evaluators extend the trusted computing base.

They must:

- Evaluate only the declared requirement type
- Use trusted authorization input
- Preserve cancellation
- Return `Indeterminate` when no reliable answer is possible
- Return structured failures for non-success outcomes
- Avoid hidden allow defaults
- Be deterministic where practical
- Be thread-safe for singleton registration
- Avoid unsafe shared mutable state
- Avoid leaking values through failure messages or logs
- Avoid slow unbounded work on the request path

An evaluator that cannot prove success must not return `Satisfied`.

## Evaluator registration

Only one evaluator may be registered for one exact requirement definition
type.

Duplicate evaluator registration causes construction failure rather than
allowing registration order to select an implementation.

A missing evaluator produces:

```text
RULEGATE_REQUIREMENT_EVALUATOR_NOT_FOUND
```

and an indeterminate result.

## Custom policy providers

A custom `IPolicyProvider` becomes part of the security boundary.

It must:

- Use exact route matching
- Preserve cancellation
- Avoid broader fallback policies
- Return one authoritative policy per route
- Be safe under concurrent requests
- Handle policy refresh atomically
- Protect remote or persistent policy storage
- Fail safely when policy data is unavailable

The built-in in-memory provider is immutable after construction.

## Exception behavior

RuleGate distinguishes expected authorization failure from unexpected
programming or infrastructure failure.

Expected conditions normally produce denied decisions or failed ASP.NET Core
authorization.

Examples:

- No matching policy
- Missing permission
- Missing attribute
- Unsupported attribute type
- Missing subject ID
- Missing route value
- Resource type mismatch

Unexpected exceptions are not always converted into deny decisions.

## ASP.NET Core mapping exceptions

The RuleGate ASP.NET Core handler converts these exceptions from subject or
resource mapping into failed authorization:

- `ArgumentException`
- `InvalidOperationException`

This covers the built-in mapping failure contract.

Other exception types from custom mapping code propagate.

Custom factories should use documented expected exception types for invalid
authorization input and reserve other exceptions for genuine operational
failure.

## ASP.NET Core enrichment exceptions

The default enrichment pipeline converts provider exceptions into failed
authorization and stops before the engine is invoked. A custom
`IRuleGateAuthorizationRequestEnricher` that throws is also failed closed by
the ASP.NET Core handler.

Built-in enrichment diagnostics report only the `ProviderException` outcome.
They do not expose the exception type, message, stack trace, returned
attributes, or attribute names.

Applications should still log and monitor infrastructure failures inside the
provider at an appropriate trusted boundary. Do not return sensitive exception
details through `RuleGateAttributeProviderResult`.

## Engine and evaluator exceptions

Unexpected exceptions from:

- Policy providers
- Requirement evaluators
- Requirement dispatch
- The authorization engine

propagate to the host application.

They are not silently converted into an ordinary denied decision.

This preserves operational visibility and prevents the protected endpoint from
executing successfully.

Applications must handle these failures through their normal:

- Exception middleware
- Logging
- Telemetry
- Availability strategy
- Incident response

Do not globally convert every authorization exception into an allowed result.

## Diagnostic exceptions

Diagnostic publication is intentionally different.

After a decision is created, exceptions thrown by the diagnostics sink are
caught and ignored.

A diagnostic failure cannot:

- Change allow to deny
- Change deny to allow
- Make authorization unavailable
- Replace the decision

The sink should monitor its own delivery failures when operational visibility
is required.

The same isolation applies to `IRuleGateEnrichmentDiagnosticsSink`.

## Cancellation behavior

Core RuleGate APIs honor cancellation tokens supplied by the caller.

Cancellation is checked during:

- Policy lookup
- Requirement dispatch
- Requirement evaluation
- Manifest file loading

Cancellation propagates as `OperationCanceledException`.

It is not converted into a deny decision.

## ASP.NET Core cancellation boundary

The ASP.NET Core handler forwards `HttpContext.RequestAborted` to every
attribute-enrichment provider and to the authorization engine.

Enrichment checks cancellation before starting each provider. Cancellation
during enrichment stops the pipeline and fails authorization before engine
evaluation. Providers must forward the token to database, network, and other
asynchronous operations.

If cancellation occurs during core engine evaluation, the engine's normal
`OperationCanceledException` behavior applies.

## Diagnostic publication cancellation

Once the engine has produced a decision, it publishes the final diagnostic
using:

```text
CancellationToken.None
```

Request cancellation therefore does not cancel the final best-effort
diagnostic write.

The built-in logging sink still checks whichever token is supplied directly
to it.

Enrichment diagnostic sinks receive the request cancellation token. Their
exceptions, including cancellation exceptions, are ignored so diagnostics do
not alter authorization behavior.

## Availability and denial of service

Authorization runs on the protected request path.

Custom components should avoid:

- Unbounded recursion
- Unbounded collections
- Slow remote calls
- Blocking I/O
- Unbounded retries
- High-cardinality logging
- Expensive repeated domain queries
- Large attribute payloads
- Unbounded diagnostic queues

Manifest YAML recursion is limited, but custom policy providers and evaluators
remain application responsibilities.

## Extension-point lifetimes

These default RuleGate extension points are singletons and custom
implementations must be concurrency-safe:

- `IPolicyProvider`
- `IRequirementEvaluator`
- `IRuleGateSubjectFactory`
- `IRuleGateAuthorizationResourceFactory`
- `IAuthorizationDiagnosticsSink`
- `IRuleGateEnrichmentDiagnosticsSink`
- `TimeProvider`

Avoid request-specific mutable state in singleton fields.

Use method-local state or concurrency-safe structures.

The ASP.NET Core authorization handler, authorization request enricher, and
attribute providers are scoped. The builder registers attribute providers as
scoped by default so they can depend on request-scoped application services.

An application may explicitly choose another provider lifetime. A singleton
provider must not capture scoped services and must remain safe under concurrent
requests.

## Secrets and personal data

RuleGate policies usually need identifiers and business attributes, not
credentials.

Do not place these values in manifests, diagnostics, or failure details:

- Passwords
- Access tokens
- Refresh tokens
- API keys
- Private keys
- Session cookies
- Unnecessary personal data
- Production credentials

Authorization attributes should contain only the minimum data needed for the
decision.

## Failure-code handling

Internal failure codes are valuable for:

- Tests
- Trusted diagnostics
- Alert grouping
- Troubleshooting

They may reveal authorization structure.

Do not automatically return them to public clients.

Map internal decisions to generic transport responses and keep detailed
failure information inside trusted boundaries.

## Deployment guidance

A secure deployment should:

1. Authenticate before authorization.
2. Compile and validate the complete manifest.
3. Refuse invalid policy configuration.
4. Register the complete policy collection atomically.
5. Protect manifest write access.
6. Record policy and application versions.
7. Use consistent identifiers and casing.
8. Configure trusted subject claims.
9. Map resources from authoritative server data.
10. Keep context attributes minimal and trusted.
11. Use generic public authorization responses.
12. Restrict diagnostics and logs.
13. Maintain a separate audit trail.
14. Test allowed, denied, malformed, and indeterminate paths.
15. Monitor authorization failures and unexpected exceptions.

## Production checklist

### Authentication

- [ ] Signature, issuer, audience, and expiration are validated.
- [ ] The expected authentication scheme protects RuleGate endpoints.
- [ ] Subject, role, and permission claims come from trusted identity data.
- [ ] Anonymous requests are challenged.

### Policies

- [ ] The complete manifest compiles successfully.
- [ ] No partial policy set is registered.
- [ ] Policy IDs and routes are unique.
- [ ] Identifier casing is consistent.
- [ ] Policy changes receive source-control review.
- [ ] The deployed manifest version is recorded.

### Subject mapping

- [ ] Exactly one stable subject ID is required.
- [ ] Ambiguous identity fails closed.
- [ ] Provider-specific role and permission claims are mapped explicitly.
- [ ] Arbitrary claims are not automatically trusted as attributes.
- [ ] Custom factories are thread-safe.

### Resource mapping

- [ ] Domain resources are loaded from authoritative server data.
- [ ] Route IDs are not treated as proof of ownership.
- [ ] Resource type matches the selected policy.
- [ ] Required route data fails closed when missing.
- [ ] Resource attributes cannot be overridden by the client.

### Context

- [ ] Time comes from a trusted `TimeProvider`.
- [ ] Context attributes are server-derived.
- [ ] Request headers are not trusted without validation.
- [ ] Custom temporal logic handles time zones explicitly.

### Attribute enrichment

- [ ] Provider data comes from authoritative server-side components.
- [ ] Missing trusted data fails closed.
- [ ] Provider exceptions and cancellation do not invoke the engine.
- [ ] Provider order is deterministic and covered by tests.
- [ ] Every collision behavior is intentional.
- [ ] Scoped providers do not leak request state across requests.
- [ ] Enrichment logs omit names, values, and exception messages.

### HTTP responses

- [ ] Anonymous requests return challenge behavior.
- [ ] Authenticated denials return forbid behavior.
- [ ] Public responses are generic.
- [ ] Internal failure codes are not exposed.
- [ ] Authentication headers are preserved.
- [ ] Trace-based support tools enforce their own authorization.

### Diagnostics and audit

- [ ] Diagnostics are enabled only when needed.
- [ ] Debug requirement logging is controlled.
- [ ] Log access and retention are restricted.
- [ ] Custom sinks omit unnecessary sensitive fields.
- [ ] Diagnostic queues are bounded.
- [ ] A separate durable audit mechanism exists when required.

### Custom extensions

- [ ] Missing or uncertain data never returns success.
- [ ] Cancellation is preserved where the integration supplies it.
- [ ] Implementations are concurrency-safe.
- [ ] Unexpected failures are observable.
- [ ] Remote calls are bounded and resilient.
- [ ] Regression tests cover failure behavior.

## Security testing strategy

Security tests should verify both success and failure.

### Policy routing

Test:

- Exact route matches
- Resource-type casing differences
- Action casing differences
- Missing policies
- Duplicate policy IDs
- Duplicate policy routes
- Malformed owned policy names

### Requirements

Test:

- Present and missing permissions
- Present and missing roles
- Missing attributes
- False comparisons
- Unsupported runtime attribute types
- Scalar type mismatches
- Unsupported operator/type combinations
- Missing custom evaluators
- Nested `all`, `any`, and `not`
- Indeterminate propagation

### Identity

Test:

- Missing subject ID
- Empty subject ID
- Repeated identical subject IDs
- Multiple distinct subject IDs
- Configured role claim types
- Configured permission claim types
- Case-distinct claim values
- Unauthenticated principals

### Resources

Test:

- Existing `AuthorizationResource`
- Valid endpoint route ID
- Missing endpoint
- Missing metadata
- Conflicting metadata
- Missing route value
- Empty route value
- Resource-type mismatch
- Unsupported custom resource

### Manifests

Test:

- Empty YAML
- Malformed YAML
- Unknown properties
- Duplicate keys
- Excessive nesting
- Unsupported schema version
- Invalid scalar literals
- Invalid operator/type combinations
- Compilation failure with zero returned policies
- Cancellation

### HTTP responses

Test:

- Anonymous `401`
- Authenticated `403`
- Allowed endpoint execution
- Preserved authentication headers
- Generic public problem codes
- Absence of internal failure details
- Absence of claims and identifiers
- Already-started response behavior
- Existing custom result-handler preservation

### Diagnostics

Test:

- Disabled sink
- Information event
- Debug requirement events
- Parent-child evaluation IDs
- Sensitive-field omission
- Custom sink field selection
- Sink exception isolation
- Concurrent evaluations
- Diagnostic and audit separation

### Attribute enrichment

Test:

- Subject, resource, and context stage order
- Equal-order registration stability
- Missing required data
- Provider exceptions
- Cancellation propagation
- Fail, keep-existing, and replace-existing collision behavior
- Unsupported attribute values
- Sensitive-field omission from diagnostics
- Scoped dependency isolation

## Current security boundaries

The current preview provides:

- Local in-process authorization
- Exact policy routing
- Default-deny decisions
- Fail-closed built-in requirements
- Typed scalar attribute comparison
- Attribute-to-attribute comparison
- Explicit-time-zone and bounded date-time requirements
- Authentication-age, MFA-age, and canonical context requirements
- Declarative YAML manifests
- Duplicate-key rejection
- Unknown-property rejection
- Bounded YAML recursion
- All-or-nothing manifest compilation
- Authenticated dynamic ASP.NET Core policies
- Fail-closed subject and resource mapping
- Ordered fail-closed ASP.NET Core attribute enrichment
- Explicit attribute collision and precedence behavior
- HTTP request cancellation propagation to enrichment and evaluation
- Generic opt-in HTTP ProblemDetails
- Opt-in structured diagnostics
- Diagnostic sink failure isolation

The current preview does not provide:

- Authentication
- Token validation
- Identity-provider-specific trust configuration
- Automatic arbitrary claim-to-attribute mapping
- Automatic domain-resource loading
- Automatic context attribute mapping
- Manifest signing
- Manifest encryption
- Manifest hot reload
- Remote policy-store security
- Durable audit storage
- OpenTelemetry integration
- Built-in distributed rate limiting
- Frontend security enforcement

These responsibilities remain with the host application or planned future
integrations.

## Next steps

Continue with:

- [Authorization model](authorization-model.md) for policy concepts.
- [Manifest guide](manifests.md) for complete YAML syntax.
- [ASP.NET Core integration](aspnetcore.md) for HTTP integration.
- [ASP.NET Core enrichment](enrichment.md) for trusted attribute providers.
- [Angular SDK](angular.md) for client-side user-experience controls.
- [Keycloak integration](keycloak.md) for optional identity claim mapping.
- [Diagnostics](diagnostics.md) for diagnostic contracts.
- [Documentation index](README.md) for all guides.
