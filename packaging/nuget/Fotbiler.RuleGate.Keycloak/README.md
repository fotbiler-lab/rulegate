# RuleGate Keycloak Integration

Optional Keycloak claim normalization and RuleGate subject mapping for ASP.NET
Core applications.

This package does not configure authentication, contact Keycloak, or depend on
a Keycloak Admin SDK. The application remains responsible for validating
bearer tokens before RuleGate maps the authenticated `ClaimsPrincipal`.

## Installation

```bash
dotnet add package Fotbiler.RuleGate.Keycloak --version 0.7.0-preview.1
```

## Register the integration

```csharp
services
    .AddRuleGate()
    .UseKeycloakSubjectMapping(options =>
    {
        options.ClientIds.Add("rulegate-api");
    });
```

See the [Keycloak integration guide](https://github.com/fotbiler-lab/rulegate/blob/main/docs/keycloak.md)
for role naming, client-role selection, and security boundaries.

## RuleGate packages

| Package                                                                                         | Purpose                                                        |
| ----------------------------------------------------------------------------------------------- | -------------------------------------------------------------- |
| [Fotbiler.RuleGate.Abstractions](https://www.nuget.org/packages/Fotbiler.RuleGate.Abstractions) | Public authorization contracts and extension abstractions      |
| [Fotbiler.RuleGate.Core](https://www.nuget.org/packages/Fotbiler.RuleGate.Core)                 | Local fail-closed authorization engine and built-in evaluators |
| [Fotbiler.RuleGate.Manifest](https://www.nuget.org/packages/Fotbiler.RuleGate.Manifest)         | YAML manifest loading, validation, and compilation             |
| [Fotbiler.RuleGate.AspNetCore](https://www.nuget.org/packages/Fotbiler.RuleGate.AspNetCore)     | ASP.NET Core integration and attribute enrichment              |
| [Fotbiler.RuleGate.Cli](https://www.nuget.org/packages/Fotbiler.RuleGate.Cli)                   | .NET tool for deterministic manifest validation and CI usage   |
| [Fotbiler.RuleGate.Keycloak](https://www.nuget.org/packages/Fotbiler.RuleGate.Keycloak)         | Optional Keycloak claim normalization and subject mapping      |

## Security

Authentication and token validation remain application responsibilities. Map
only explicitly trusted realm roles, client roles, and permission claims into
RuleGate subjects.

Report suspected vulnerabilities through the
[private security reporting process](https://github.com/fotbiler-lab/rulegate/security/policy).

## License

RuleGate is licensed under the
[Apache License 2.0](https://github.com/fotbiler-lab/rulegate/blob/main/LICENSE).
