# Dedicated Keycloak Setup

The reference application must use a dedicated realm before publication. Do
not give the application Keycloak Admin API credentials and do not commit a
realm export containing credentials or test-user passwords.

## Realm

Create one realm:

```text
rulegate-samples
```

## Clients

Create these OpenID Connect clients:

| Client ID                        | Type and flow                                            | Purpose                           |
| -------------------------------- | -------------------------------------------------------- | --------------------------------- |
| `rulegate-document-approval-web` | Public; standard flow; PKCE `S256`                       | Angular login                     |
| `rulegate-document-approval-api` | Confidential resource client; interactive flows disabled | API audience and permission roles |

Configure the web client with:

```text
Valid redirect URIs:           http://localhost:4200/*
Valid post logout redirect URIs: http://localhost:4200/*
Web origins:                   http://localhost:4200
```

The API does not use the resource client's secret. Client authentication keeps
the client out of browser use; standard flow, direct access grants, service
accounts, and authorization services remain disabled.

## Roles

Create these normal, non-composite client roles on
`rulegate-document-approval-api`:

```text
DOC.READ
DOC.CREATE
DOC.UPDATE
WFL.START
WFL.APPROVE
WFL.REJECT
```

Create these realm roles and make each one composite:

| Composite realm role | Included API client roles or realm roles                 |
| -------------------- | -------------------------------------------------------- |
| `VIEWER`             | `DOC.READ`                                               |
| `DOCUMENT_MANAGER`   | `DOC.READ`, `DOC.CREATE`, `DOC.UPDATE`, `WFL.START`      |
| `APPROVER`           | `DOC.READ`, `WFL.APPROVE`, `WFL.REJECT`                  |
| `ADMIN`              | realm roles `VIEWER`, `DOCUMENT_MANAGER`, and `APPROVER` |

RuleGate consumes the effective `APPROVER` realm role as
`keycloak:realm:APPROVER`. It consumes the API client roles through the
explicit permission claim described below.

## Access-token claims

Create a client scope named `rulegate-api-access` with two protocol mappers:

1. A User Client Role mapper for `rulegate-document-approval-api`:
   - token claim name: `permission`;
   - multivalued: enabled;
   - claim JSON type: `String`;
   - add to access token: enabled.
2. An Audience mapper:
   - included client audience: `rulegate-document-approval-api`;
   - add to access token: enabled.

Assign `rulegate-api-access` to `rulegate-document-approval-web` as a default
client scope. The resulting access token must contain the API client ID in
`aud`, the effective realm role in `realm_access.roles`, and effective API
permissions in the top-level `permission` array.

## Test identities

Create users without storing their passwords in this repository:

| Username                | Realm role         | Local sample organization |
| ----------------------- | ------------------ | ------------------------- |
| `sample-viewer`         | `VIEWER`           | `records`                 |
| `sample-manager`        | `DOCUMENT_MANAGER` | `records`                 |
| `sample-approver`       | `APPROVER`         | `records`                 |
| `sample-legal-approver` | `APPROVER`         | `legal`                   |

These four usernames are part of the deterministic SQLite seed data after the
dedicated realm replaces the temporary development realm.

## Token verification

Before switching application configuration, inspect one access token and
confirm:

- `iss` is the `rulegate-samples` realm issuer;
- `aud` includes `rulegate-document-approval-api`;
- `preferred_username` is present;
- `realm_access.roles` contains the assigned composite realm role;
- `permission` contains its effective API client roles.
