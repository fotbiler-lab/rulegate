# Minimal ASP.NET Core Sample

This package-only sample shows the smallest runnable RuleGate integration:

- compile `rulegate.yaml` at startup;
- register immutable policies;
- protect a Minimal API endpoint;
- return fail-closed `401` and `403` responses.

Run it:

```bash
dotnet run --project samples/aspnetcore-minimal
```

Then call the endpoint with and without the required permission:

```bash
curl -i http://localhost:5000/documents/doc-1

curl -i http://localhost:5000/documents/doc-1 \
  -H 'X-Demo-User: alice' \
  -H 'X-Demo-Permissions: DOC.READ'
```

The header-based identity exists only to keep this example focused. Never use
it in production. The [document-approval sample](../document-approval/README.md)
shows validated Keycloak bearer tokens.
