# RuleGate Keycloak Integration

Optional Keycloak claim normalization and RuleGate subject mapping for ASP.NET Core applications.

This package does not configure authentication, contact Keycloak, or depend on a Keycloak Admin SDK. The application remains responsible for validating bearer tokens before RuleGate maps the authenticated `ClaimsPrincipal`.

```csharp
services
    .AddRuleGate()
    .UseKeycloakSubjectMapping(options =>
    {
        options.ClientIds.Add("rulegate-api");
    });
```

See the [Keycloak integration guide](https://github.com/fotbiler-lab/rulegate/blob/main/docs/keycloak.md) for role naming, client-role selection, and security boundaries.
