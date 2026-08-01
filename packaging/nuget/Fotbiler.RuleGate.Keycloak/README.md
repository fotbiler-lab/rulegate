# RuleGate Keycloak Integration

Optional Keycloak claim normalization and RuleGate subject mapping for ASP.NET
Core applications.

This package does not configure authentication, contact Keycloak, or depend on
a Keycloak Admin SDK. The application remains responsible for validating
bearer tokens before RuleGate maps the authenticated `ClaimsPrincipal`.

## Installation

```bash
dotnet add package Fotbiler.RuleGate.Keycloak --version 1.0.0
```

## Compatibility

This package targets .NET Core 3.1 and every .NET release
from 5 through 10. Legacy targets are package-tested for migration support but
remain outside Microsoft security support.

## Register the integration

```csharp
services
    .AddRuleGate()
    .UseKeycloakSubjectMapping(options =>
    {
        options.ClientIds.Add("rulegate-api");
    });
```

The mapping keeps identity-provider details outside the policy engine:

| Validated token input                   | RuleGate subject output              |
| --------------------------------------- | ------------------------------------ |
| `sub`                                   | Subject identifier                   |
| `realm_access.roles`                    | `keycloak:realm:<role>`              |
| Selected `resource_access` client roles | `keycloak:client:<client-id>:<role>` |
| Explicit `permission` claims            | Provider-independent permissions     |

Only selected client IDs are mapped. RuleGate does not contact Keycloak to
expand roles or infer permissions absent from the validated token.

Policy sources and atomic reload remain provider-independent; they do not
contact Keycloak or change token-validation behavior. See the
[policy-source guide](https://github.com/fotbiler-lab/rulegate/blob/main/docs/policy-sources.md).

Authorization telemetry remains provider-independent and does not emit raw
Keycloak roles, claims, subject identifiers, or token values. See the
[telemetry, performance, and concurrency guide](https://github.com/fotbiler-lab/rulegate/blob/main/docs/telemetry-performance-concurrency.md).

See the [Keycloak integration guide](https://github.com/fotbiler-lab/rulegate/blob/main/docs/keycloak.md)
for role naming, client-role selection, and security boundaries.

Start with the [complete RuleGate guide](https://github.com/fotbiler-lab/rulegate/blob/main/docs/guide/README.md)
and continue with the [Identity and Keycloak chapter](https://github.com/fotbiler-lab/rulegate/blob/main/docs/guide/07-Identity-and-Keycloak.md)
for the connected authentication, backend, frontend, and trust-boundary flow.

See the
[document-approval reference application](https://github.com/fotbiler-lab/rulegate/tree/main/samples/document-approval)
for the complete ASP.NET Core, Angular, Keycloak, SQLite, and YAML composition.

## RuleGate packages

| Package                                                                                         | Purpose                                                        |
| ----------------------------------------------------------------------------------------------- | -------------------------------------------------------------- |
| [Fotbiler.RuleGate.Abstractions](https://www.nuget.org/packages/Fotbiler.RuleGate.Abstractions) | Public authorization contracts and extension abstractions      |
| [Fotbiler.RuleGate.Core](https://www.nuget.org/packages/Fotbiler.RuleGate.Core)                 | Local fail-closed authorization engine and built-in evaluators |
| [Fotbiler.RuleGate.Manifest](https://www.nuget.org/packages/Fotbiler.RuleGate.Manifest)         | YAML manifest loading, validation, and compilation             |
| [Fotbiler.RuleGate.AspNetCore](https://www.nuget.org/packages/Fotbiler.RuleGate.AspNetCore)     | ASP.NET Core integration and attribute enrichment              |
| [Fotbiler.RuleGate.Cli](https://www.nuget.org/packages/Fotbiler.RuleGate.Cli)                   | Manifest validation, policy testing, generation, and CI usage  |
| [Fotbiler.RuleGate.Keycloak](https://www.nuget.org/packages/Fotbiler.RuleGate.Keycloak)         | Optional Keycloak claim normalization and subject mapping      |

## Security

Authentication and token validation remain application responsibilities. Map
only explicitly trusted realm roles, client roles, and permission claims into
RuleGate subjects.

Report suspected vulnerabilities through the
[private security reporting process](https://github.com/fotbiler-lab/rulegate/security/policy).

## License

RuleGate is licensed under the
[MIT License](https://github.com/fotbiler-lab/rulegate/blob/main/LICENSE).
