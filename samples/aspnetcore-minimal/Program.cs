using System.Security.Claims;
using System.Text.Encodings.Web;
using Fotbiler.RuleGate.AspNetCore.DependencyInjection;
using Fotbiler.RuleGate.AspNetCore.Endpoints;
using Fotbiler.RuleGate.Manifest.Compilation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

var manifestPath =
    Path.Combine(builder.Environment.ContentRootPath, "rulegate.yaml");
var compilation =
    await new RuleGateManifestCompiler().CompileFromFileAsync(manifestPath);

if (!compilation.IsSuccess)
{
    throw new InvalidOperationException(
        "rulegate.yaml is invalid. Run 'rulegate validate' for details.");
}

builder.Services
    .AddAuthentication("Demo")
    .AddScheme<AuthenticationSchemeOptions, DemoAuthenticationHandler>("Demo", null);
builder.Services.AddAuthorization();
builder.Services
    .AddRuleGate()
    .AddHttpAuthorizationResultMapping()
    .AddPolicies(compilation.Policies);

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Ok(new { sample = "RuleGate Minimal API" }));

app.MapGet(
        "/documents/{id}",
        (string id) => Results.Ok(new { id, title = "Reference document" }))
    .RequireRuleGate("document", "read", "id");

app.Run();

public partial class Program;

// This header identity is intentionally sample-only. Production applications
// must use a real authentication handler and validated identity tokens.
public sealed class DemoAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
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

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name));
        return Task.FromResult(
            AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name)));
    }
}
