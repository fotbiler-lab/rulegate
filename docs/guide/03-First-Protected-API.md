# 3. First Protected API

This chapter builds the smallest complete ASP.NET Core flow:

1. authenticate a caller;
2. compile `rulegate.yaml`;
3. register RuleGate;
4. map one endpoint to `document/read`;
5. observe `401`, `403`, and `200` outcomes.

The sample authentication handler uses headers only to keep the chapter
focused. Never use caller-controlled identity headers in production.

## 1. Create the project

```bash
dotnet new web -n RuleGateQuickstart
cd RuleGateQuickstart
dotnet add package Fotbiler.RuleGate.AspNetCore --version 1.0.0
dotnet tool install Fotbiler.RuleGate.Cli --version 1.0.0 \
  --create-manifest-if-needed
```

## 2. Define a policy

Create `rulegate.yaml`:

```yaml
schemaVersion: 1

application:
  id: rulegate-quickstart
  name: RuleGate Quickstart

policies:
  - id: document-read
    resourceType: document
    action: read
    requirement:
      all:
        - any:
            - permission: DOC.READ
            - role: DOCUMENT.READER
        - not:
            role: DOCUMENT.BLOCKED
```

Read it as:

> The caller may read a document when the caller has either `DOC.READ` or the
> `DOCUMENT.READER` role, and does not have `DOCUMENT.BLOCKED`.

Validate before starting the host:

```bash
dotnet tool run rulegate validate rulegate.yaml
```

Fix validation errors rather than starting with a partial policy set.

## 3. Publish the manifest

Add this to the project file:

```xml
<ItemGroup>
  <Content Include="rulegate.yaml">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    <CopyToPublishDirectory>PreserveNewest</CopyToPublishDirectory>
  </Content>
</ItemGroup>
```

## 4. Compile and register RuleGate

Replace `Program.cs` with the following. The authentication handler appears
after the application code.

```csharp
using System.Security.Claims;
using System.Text.Encodings.Web;
using Fotbiler.RuleGate.AspNetCore.DependencyInjection;
using Fotbiler.RuleGate.AspNetCore.Endpoints;
using Fotbiler.RuleGate.Manifest.Compilation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

var manifestPath = Path.Combine(
    builder.Environment.ContentRootPath,
    "rulegate.yaml");

var compilation = await new RuleGateManifestCompiler()
    .CompileFromFileAsync(manifestPath);

if (!compilation.IsSuccess)
{
    throw new InvalidOperationException(
        "rulegate.yaml is invalid. Run 'rulegate validate' for details.");
}

builder.Services
    .AddAuthentication("Demo")
    .AddScheme<AuthenticationSchemeOptions, DemoAuthenticationHandler>(
        "Demo",
        configureOptions: null);

builder.Services.AddAuthorization();

builder.Services
    .AddRuleGate()
    .AddHttpAuthorizationResultMapping()
    .AddPolicies(compilation.Policies);

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet(
        "/documents/{id}",
        (string id) => Results.Ok(new
        {
            id,
            title = "Reference document",
        }))
    .RequireRuleGate("document", "read", "id");

app.Run();
```

Registration order matters:

- authentication establishes `HttpContext.User`;
- ASP.NET Core authorization hosts RuleGate's handler;
- `AddRuleGate()` registers the engine and built-in evaluators;
- policies are immutable definitions used by the engine;
- authentication middleware runs before authorization middleware.

## 5. Add the sample authentication handler

Append this class to `Program.cs`:

```csharp
public sealed class DemoAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(
        options,
        logger,
        encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-Demo-User", out var userId))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
        };

        foreach (var permission in Request.Headers["X-Demo-Permissions"]
                     .ToString()
                     .Split(',', StringSplitOptions.RemoveEmptyEntries |
                                 StringSplitOptions.TrimEntries))
        {
            claims.Add(new Claim("permission", permission));
        }

        foreach (var role in Request.Headers["X-Demo-Roles"]
                     .ToString()
                     .Split(',', StringSplitOptions.RemoveEmptyEntries |
                                 StringSplitOptions.TrimEntries))
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
```

Production authentication must validate issuer, audience, signature,
expiration, and the application-specific token requirements. RuleGate starts
after that validation.

## 6. Run and observe the boundary

```bash
dotnet run
```

Use the URL printed by ASP.NET Core.

Anonymous request:

```bash
curl -i http://localhost:5000/documents/doc-1
```

Expected: `401`. Authentication did not establish a subject.

Authenticated without a grant:

```bash
curl -i http://localhost:5000/documents/doc-1 \
  -H 'X-Demo-User: alice'
```

Expected: `403`. The subject exists but the policy is not satisfied.

Permission path:

```bash
curl -i http://localhost:5000/documents/doc-1 \
  -H 'X-Demo-User: alice' \
  -H 'X-Demo-Permissions: DOC.READ'
```

Expected: `200`.

Role path:

```bash
curl -i http://localhost:5000/documents/doc-1 \
  -H 'X-Demo-User: alice' \
  -H 'X-Demo-Roles: DOCUMENT.READER'
```

Expected: `200`.

Explicit block:

```bash
curl -i http://localhost:5000/documents/doc-1 \
  -H 'X-Demo-User: alice' \
  -H 'X-Demo-Permissions: DOC.READ' \
  -H 'X-Demo-Roles: DOCUMENT.BLOCKED'
```

Expected: `403` because the `not` requirement is not satisfied.

## 7. Understand the request

For `/documents/doc-1`, the integration constructs approximately:

```text
subject.id       = alice
subject.roles    = values mapped from ClaimTypes.Role
subject.permissions = values mapped from permission claims
resource.type    = document
resource.id      = doc-1
action           = read
context.evaluationTime = trusted server clock
```

The default resource mapper does not query a database. Later chapters add a
resource provider that loads owner, organization, status, and classification.

## 8. Keep the full sample nearby

The repository's
[Minimal ASP.NET Core sample](../../samples/aspnetcore-minimal/README.md)
contains a broad manifest catalog and deterministic policy fixtures. The
[document-approval application](../../samples/document-approval/README.md)
shows real Keycloak authentication, SQLite-backed resource data, Angular, and
the complete authorization workflow.

## Common first-run mistakes

- **401 instead of 403:** authentication did not create an authenticated
  principal, or authentication middleware is missing.
- **403 for every user:** the claim type or exact permission/role casing does
  not match.
- **Manifest not found:** copy it to output/publish or use an absolute path
  built from the content root.
- **Policy not found:** `resourceType` and `action` do not exactly match the
  endpoint metadata.
- **Unexpected allow assumption:** a valid token authenticates; it does not
  automatically grant a RuleGate policy.

## Further reference

- [Getting started](../getting-started.md)
- [ASP.NET Core reference](../aspnetcore.md)
- [HTTP result mapping](../aspnetcore.md#http-authorization-results)

---

Previous: [Packages and installation](02-Packages-and-Installation.md) · Next:
[Policy language](04-Policy-Language.md)
