# 13. Real-World Recipes

Each recipe follows the same reasoning order: problem, authorization model,
policy, trusted backend data, frontend projection, tests, and mistakes.

## Recipe 1: owner may edit a draft

### Problem

A user may update a document only while it is a draft and only when the user
owns that document.

### Authorization reasoning

- permission: broad update capability;
- subject attribute: current user ID;
- resource attributes: owner ID and status;
- action: `update`.

### Policy

```yaml
- id: document-update-own-draft
  resourceType: document
  action: update
  requirement:
    all:
      - permission: DOC.UPDATE
      - attribute:
          source: resource
          name: status
          operator: equal
          valueType: string
          value: draft
      - attributeComparison:
          left: { source: subject, name: userId }
          operator: equal
          right: { source: resource, name: ownerId }
```

### Backend

A subject provider supplies `userId`; a resource provider loads `ownerId` and
`status` from the database. The update command must use the same protected
resource version or re-check before commit.

### Frontend

The snapshot may include `DOC.UPDATE` to show an edit affordance. The frontend
cannot know authoritative ownership or current status; handle backend `403`
after a concurrent change.

### Tests

Test owner/draft allow, other owner deny, submitted status deny, missing owner
indeterminate/deny, and a status change between read and update.

### Common mistake

Comparing the route ID to the subject ID does not prove ownership. Load the
document.

## Recipe 2: organization and tenant isolation

### Problem

Users can read documents only in their current organization and tenant.

### Policy

```yaml
- id: document-read-organization
  resourceType: document
  action: read
  requirement:
    all:
      - permission: DOC.READ
      - attributeComparison:
          left: { source: subject, name: tenantId }
          operator: equal
          right: { source: resource, name: tenantId }
      - attributeComparison:
          left: { source: subject, name: organizationId }
          operator: equal
          right: { source: resource, name: organizationId }
```

### Backend

Resolve the current tenant/organization from a trusted application assignment,
not a caller-selected header. Scope repository queries by tenant before
authorization. RuleGate provides defense in depth and business policy; it is
not a substitute for data-layer isolation.

### Tests

Create a matrix:

| Subject tenant | Subject org | Resource tenant | Resource org | Result |
| -------------- | ----------- | --------------- | ------------ | ------ |
| A              | records     | A               | records      | allow  |
| A              | records     | A               | legal        | deny   |
| A              | records     | B               | records      | deny   |
| missing        | records     | A               | records      | deny   |

### Common mistake

Using a global admin role as an implicit tenant bypass. If a reviewed support
workflow needs cross-tenant access, model it as a separate policy/action with
strong context and audit requirements.

## Recipe 3: four-eyes approval with amount limit

### Problem

An approver may approve a submitted request within their amount limit, but
cannot approve their own request.

### Policy

```yaml
- id: purchase-request-approve
  resourceType: purchase-request
  action: approve
  requirement:
    all:
      - permission: PURCHASE.APPROVE
      - role: PURCHASE.APPROVER
      - attribute:
          source: resource
          name: status
          operator: equal
          valueType: string
          value: submitted
      - attributeComparison:
          left: { source: resource, name: totalAmount }
          operator: lessThanOrEqual
          right: { source: subject, name: approvalLimit }
      - not:
          attributeComparison:
            left: { source: subject, name: userId }
            operator: equal
            right: { source: resource, name: requesterId }
```

### Backend

Load amount, requester, and status from a transactionally appropriate read.
After authorization, change only from `submitted` using an optimistic
concurrency check or transaction.

### Frontend

Show the approvals area from `PURCHASE.APPROVE`; show the final Approve button
as tentative UX. Backend resource checks remain authoritative.

### Tests

Test exact limit, one unit above limit, self-approval, wrong status, missing
limit, and two approvers racing.

## Recipe 4: confidential document with clearance and business hours

### Problem

Confidential content requires sufficient clearance, an internal/VPN network,
a trusted device, fresh MFA, and organization-specific operating hours.

### Policy

For a shared fixed schedule:

```yaml
- id: confidential-document-read
  resourceType: document
  action: read-confidential
  requirement:
    all:
      - permission: DOC.CONFIDENTIAL.READ
      - attributeComparison:
          left: { source: subject, name: clearanceLevel }
          operator: greaterThanOrEqual
          right: { source: resource, name: classificationLevel }
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
          maximumAge: '00:10:00'
      - timeWindow:
          days: [monday, tuesday, wednesday, thursday, friday]
          start: '08:00'
          end: '18:00'
          timeZone: Europe/Istanbul
```

For per-organization schedules, load the schedule from application settings or
a trusted directory through a context provider. Then use a reviewed custom
requirement/evaluator or separate organization policy snapshots. A request
parameter must never choose the allowed hours.

### Tests

Test clearance equal/less, every network state, trusted false/missing, MFA at
the exact age boundary, before/at/after schedule boundaries, weekends, and
daylight-saving behavior for zones that observe it.

## Recipe 5: collection/list authorization

### Problem

A list endpoint must return only resources visible to the caller.

### Correct pattern

1. authorize the collection action `document/list`;
2. derive tenant/organization/clearance filters from trusted subject data;
3. apply those filters in the repository query;
4. optionally authorize returned high-risk items individually;
5. never fetch all tenants and hide rows only in Angular.

Collection policy:

```yaml
- id: documents-list
  resourceType: document
  action: list
  requirement:
    all:
      - permission: DOC.LIST
      - attribute:
          source: subject
          name: organizationId
          operator: exists
```

Resource policies cannot automatically rewrite database queries. The
application must enforce filtering.

## Recipe 6: service identity and request channel

### Problem

A worker may post an invoice only through the worker channel and only for its
assigned organization.

```yaml
- id: invoice-worker-post
  resourceType: invoice
  action: post
  requirement:
    all:
      - permission: INVOICE.POST
      - context:
          property: identityType
          operator: equal
          valueType: string
          value: service
      - context:
          property: requestChannel
          operator: equal
          valueType: string
          value: worker
      - attributeComparison:
          left: { source: subject, name: organizationId }
          operator: equal
          right: { source: resource, name: organizationId }
```

Authenticate the service with an appropriate machine credential. Do not use a
human role as a substitute for a service identity. Construct a direct
`AuthorizationRequest` or adapt the worker host around the same engine.

## Recipe 7: emergency access without a hidden bypass

### Problem

An incident team needs temporary read access during a declared emergency.

### Safer model

Use a separate action/policy with explicit capability, context, fixed window,
and application audit—not an `if (isAdmin) return true` hidden in code:

```yaml
- id: document-emergency-read
  resourceType: document
  action: emergency-read
  requirement:
    all:
      - permission: INCIDENT.EMERGENCY.READ
      - role: INCIDENT.RESPONDER
      - context:
          property: authenticationMethod
          operator: equal
          valueType: string
          value: phishing-resistant-mfa
      - contextAge:
          timestamp: mfa
          maximumAge: '00:05:00'
      - dateTimeWindow:
          startsAt: '2026-08-01T00:00:00Z'
          endsAt: '2026-08-02T00:00:00Z'
```

Promote/remove the window through the governed policy lifecycle. Record a
domain audit event for every use. Do not log sensitive document content in
RuleGate diagnostics.

## Recipe 8: modern and legacy frontends, one backend policy

An Angular 22 application uses signals/functional guards; an Angular 15
application uses the legacy observable adapter; an Angular 10 application uses
the framework-independent store. All send operations to the same protected
backend.

The UI technology changes how the snapshot is consumed, not the meaning of
`DOC.READ`, `document-read`, or the backend's organization/resource/context
rules. Keep generated identifiers and backend policies aligned, but never
duplicate the complete ABAC/CBAC policy in JavaScript.

## Recipe review template

Use this template for new domains:

```text
Problem:
Subject:
Resource:
Action:
Requirements:
Trusted source for every fact:
Manifest policy:
Backend enforcement point:
Frontend projection (optional):
Allow tests:
Deny/indeterminate tests:
Concurrency and stale-data behavior:
Diagnostics and audit behavior:
Common bypasses to prevent:
```

## Further reference

- [Reference applications](../reference-applications.md)
- [Minimal sample](../../samples/aspnetcore-minimal/README.md)
- [Document approval case study](../../samples/document-approval/README.md)
- [Document approval verification](../../samples/document-approval/verification.md)

---

Previous: [Extensibility](12-Extensibility.md) · Next:
[Production checklist](14-Production-Checklist.md)
