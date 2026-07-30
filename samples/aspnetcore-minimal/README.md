# Minimal ASP.NET Core Sample

This package-only sample shows a minimal runnable RuleGate host with a detailed
manifest:

- compile `rulegate.yaml` at startup;
- register immutable policies;
- protect a Minimal API endpoint;
- return fail-closed `401` and `403` responses;
- provide copyable permission-, role-, attribute-, context-, resource-, time-,
  and logical-policy examples.

Run it:

```bash
dotnet run --project samples/aspnetcore-minimal
```

Then exercise anonymous, denied, and allowed requests:

```bash
# 401: no authenticated demo identity
curl -i http://localhost:5000/documents/doc-1

# 403: authenticated, but no matching permission or role
curl -i http://localhost:5000/documents/doc-1 \
  -H 'X-Demo-User: alice'

# 200: permission-based access
curl -i http://localhost:5000/documents/doc-1 \
  -H 'X-Demo-User: alice' \
  -H 'X-Demo-Permissions: DOC.READ'

# 200: role-based access
curl -i http://localhost:5000/documents/doc-1 \
  -H 'X-Demo-User: alice' \
  -H 'X-Demo-Roles: DOCUMENT.READER'

# 403: explicit blocked-role rule wins
curl -i http://localhost:5000/documents/doc-1 \
  -H 'X-Demo-User: alice' \
  -H 'X-Demo-Permissions: DOC.READ' \
  -H 'X-Demo-Roles: DOCUMENT.BLOCKED'
```

The header-based identity exists only to keep this example focused. Never use
it in production. The [document-approval sample](../document-approval/README.md)
shows validated Keycloak bearer tokens.

## Manifest catalog

Only `document/read` is bound to an HTTP endpoint so the host stays minimal.
The remaining policies in [`rulegate.yaml`](rulegate.yaml) are valid,
copyable examples of:

- permissions, roles, and nested `all`, `any`, and `not` requirements;
- subject, resource, and context attribute requirements;
- ownership, organization, numeric, and other attribute-to-attribute checks;
- string, collection, presence, null, and empty-state operators;
- canonical request context, recurring `timeWindow`, one-time
  `dateTimeWindow`, and `contextAge` requirements.

The default ASP.NET Core integration maps identity, roles, and permissions. It
does not invent application attributes. Before binding one of the catalog
policies to an endpoint, add trusted subject, resource, and context attribute
providers for every value it references. Missing or incompatible data denies
access.
