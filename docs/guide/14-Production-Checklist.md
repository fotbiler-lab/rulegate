# 14. Production Checklist

Use this chapter as the final review before enabling a RuleGate-protected
operation in production.

## Architecture review

- [ ] Authentication and authorization responsibilities are separate.
- [ ] Every protected operation has a stable resource type and action.
- [ ] Every policy fact has an identified trusted source.
- [ ] The backend—not only the frontend—evaluates every protected operation.
- [ ] Repository/data-layer tenant isolation is enforced independently of UI
      filtering.
- [ ] No undocumented administrator, support, or emergency bypass exists.

## Authentication

- [ ] Issuer, audience, signature, lifetime, and required token properties are
      validated.
- [ ] Subject ID claim is stable, unique, and required.
- [ ] Role and permission claim types are explicit.
- [ ] Ambiguous or malformed structured claims fail authentication/mapping.
- [ ] MFA/authentication timestamps are validated before becoming context.
- [ ] Logout, token refresh failure, and identity switching clear frontend
      grants.

## Policies and manifests

- [ ] `rulegate validate` passes.
- [ ] `rulegate lint` passes or every accepted finding is reviewed.
- [ ] Policy IDs and resource/action routes are unique.
- [ ] Identifiers use intentional casing and stable domain language.
- [ ] Logical trees are understandable and bounded.
- [ ] Negative rules do not turn missing data into access.
- [ ] Time zones and boundaries are explicit and tested.
- [ ] Manifest/source write permissions are restricted.
- [ ] Generated C# and TypeScript output passes `--check`.

## Subject, resource, and context data

- [ ] Claims are explicitly mapped; claims are not automatically attributes.
- [ ] Current assignments come from an application-owned trusted source.
- [ ] Resource attributes come from the authoritative protected object.
- [ ] Route IDs are not mistaken for loaded domain state.
- [ ] Network/device/channel values are validated server-side.
- [ ] Missing provider data returns `MissingRequiredData()` or `Fail()`.
- [ ] Unsupported types and collisions fail closed.
- [ ] Provider lifetimes match dependencies and cancellation is propagated.

## HTTP and domain operations

- [ ] `UseAuthentication()` runs before `UseAuthorization()`.
- [ ] Anonymous requests challenge (`401`) and authenticated denials forbid
      (`403`) according to the host contract.
- [ ] Public error bodies do not expose policies, requirements, claims,
      permissions, roles, attributes, or exceptions.
- [ ] The state mutation occurs only after authorization.
- [ ] Stale resource state and concurrency are handled transactionally or with
      re-checks.
- [ ] Collection endpoints enforce query filtering as well as collection-level
      authorization.

## Frontend

- [ ] Correct package is installed for Angular/framework version.
- [ ] Snapshot replacement is atomic and malformed state clears grants.
- [ ] Route guards and directives are described as UX controls.
- [ ] Hidden/disabled controls do not replace backend checks.
- [ ] Stale frontend state results in a safely handled backend denial.
- [ ] Custom disabled controls handle keyboard/focus accessibility.

## Policy sources and reload

- [ ] Initial load failure leaves an empty deny-by-default snapshot.
- [ ] Reload failure preserves the last valid snapshot.
- [ ] All sources have unique names, policy IDs, and routes.
- [ ] Fleet rollout uses a versioned artifact/configuration process.
- [ ] Reload success/failure and active policy count are monitored.
- [ ] Local snapshot counters are not treated as distributed versions.

## Diagnostics, telemetry, and audit

- [ ] Diagnostics are explicitly enabled only where required.
- [ ] Logs and metrics contain no sensitive or high-cardinality values.
- [ ] Custom sinks isolate failure and bounded work.
- [ ] RuleGate diagnostics are not used as the business audit log.
- [ ] Authorized domain changes produce appropriate application audit events.
- [ ] OpenTelemetry sampling/exporters are configured by the host.

## Tests and release gates

- [ ] Every policy has minimum allow and targeted deny cases.
- [ ] Missing/wrong-type inputs prove fail-closed behavior.
- [ ] Ownership, organization, tenant, and context boundaries are tested.
- [ ] Time/MFA boundaries use fixed explicit-offset instants.
- [ ] Provider exception/cancellation/collision tests exist.
- [ ] Endpoint tests cover `401`, `403`, and success.
- [ ] Unauthorized mutations leave state unchanged.
- [ ] Supported .NET/Angular consumers build against packages, not project
      references.

## Troubleshooting map

| Symptom                             | First checks                                                                       |
| ----------------------------------- | ---------------------------------------------------------------------------------- |
| Every request is `401`              | authentication scheme, token validation, middleware order, authenticated principal |
| Authenticated requests are `403`    | exact route, subject ID, role/permission claim types and casing                    |
| Attribute policy always denies      | provider registration, source, exact name, runtime type, missing data              |
| Resource policy has no attributes   | route mapping supplies only ID; add resource provider or imperative resource       |
| Time policy differs by environment  | time-zone data, explicit offsets, trusted clock, boundary tests                    |
| Reload rejected                     | structured source diagnostics, duplicate IDs/routes, full manifest validation      |
| Angular guard always denies         | snapshot initialized, exact identifiers, valid route metadata, correct adapter     |
| Angular shows action but API denies | expected trust boundary; inspect backend resource/context rule and refresh UI      |
| CLI `--check` fails                 | generated file missing/stale or generator options/path changed                     |
| Explanation lacks skipped branch    | logical short-circuit reports only evaluated branches                              |

## Upgrade strategy

1. Read the changelog and migration guide.
2. Upgrade every package in one ecosystem together.
3. Restore locked dependencies.
4. Validate, lint, and test policies.
5. Regenerate and check C#/TypeScript identifiers.
6. Run package-only compatibility and endpoint tests.
7. Roll out policies and code through the normal governed deployment path.
8. Monitor denial categories and reload state without logging sensitive data.

RuleGate `1.x` follows stable compatibility expectations documented by the
project. Preview versions are not covered by the same guarantee.

## Where to continue

- [Security model](../security.md) — complete threat and failure boundaries
- [Diagnostics](../diagnostics.md) — safe operational contracts
- [Platform compatibility](../platform-compatibility.md) — tested runtimes
- [Migration to 1.0](../migration-to-1.0.md) — preview-to-stable changes
- [Reference applications](../reference-applications.md) — runnable hosts
- [Roadmap](../roadmap.md) — future documentation and sample work

---

Previous: [Real-world recipes](13-Real-World-Recipes.md) · Back to:
[The RuleGate Guide](README.md)
