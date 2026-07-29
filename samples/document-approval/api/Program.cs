using System.Security.Claims;
using Fotbiler.RuleGate.AspNetCore.DependencyInjection;
using Fotbiler.RuleGate.AspNetCore.Endpoints;
using Fotbiler.RuleGate.Keycloak.DependencyInjection;
using Fotbiler.RuleGate.Manifest.Compilation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RuleGate.DocumentApproval.Api.Authorization;
using RuleGate.DocumentApproval.Api.Data;
using RuleGate.DocumentApproval.Api.Domain;

var builder = WebApplication.CreateBuilder(args);

var keycloak = builder.Configuration.GetRequiredSection("Keycloak");
var authority = keycloak["Authority"] ?? throw new InvalidOperationException("Keycloak:Authority is required.");
var audience = keycloak["Audience"] ?? throw new InvalidOperationException("Keycloak:Audience is required.");
var clientId = keycloak["ClientId"] ?? throw new InvalidOperationException("Keycloak:ClientId is required.");
var webOrigin = builder.Configuration["Web:Origin"] ?? "http://localhost:4200";
var connectionString = builder.Configuration.GetConnectionString("SampleDatabase") ??
    throw new InvalidOperationException("ConnectionStrings:SampleDatabase is required.");

var manifestPath = Path.Combine(builder.Environment.ContentRootPath, "rulegate.yaml");
var compilation = await new RuleGateManifestCompiler().CompileFromFileAsync(manifestPath);

if (!compilation.IsSuccess)
{
    throw new InvalidOperationException(
        "rulegate.yaml is invalid. Run 'rulegate validate' for structured diagnostics.");
}

builder.Services.AddProblemDetails();
builder.Services.AddCors(
    options => options.AddDefaultPolicy(
        policy => policy.WithOrigins(webOrigin).AllowAnyHeader().AllowAnyMethod()));
builder.Services.AddDbContext<SampleDbContext>(options => options.UseSqlite(connectionString));
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(
        options =>
        {
            options.Authority = authority;
            options.Audience = audience;
            options.MapInboundClaims = false;
            options.RequireHttpsMetadata = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                NameClaimType = "preferred_username",
            };
        });
builder.Services.AddAuthorization();
builder.Services
    .AddRuleGate()
    .UseKeycloakSubjectMapping(options => options.ClientIds.Add(clientId))
    .AddSubjectAttributeProvider<UserProfileAttributeProvider>()
    .AddResourceAttributeProvider<DocumentAttributeProvider>()
    .AddContextAttributeProvider<ApiRequestContextProvider>()
    .AddLoggingDiagnostics()
    .AddHttpAuthorizationResultMapping()
    .AddPolicies(compilation.Policies);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    await SampleData.InitializeAsync(scope.ServiceProvider.GetRequiredService<SampleDbContext>());
}

app.UseExceptionHandler();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/api/health", () => Results.Ok(new { status = "ready" }));
app.MapGet(
        "/api/me",
        async (ClaimsPrincipal principal, SampleDbContext database, CancellationToken cancellationToken) =>
        {
            var username = principal.FindFirstValue("preferred_username") ?? principal.Identity?.Name;
            var profile = username is null
                ? null
                : await database.UserProfiles.AsNoTracking()
                    .SingleOrDefaultAsync(item => item.Username == username, cancellationToken);

            return profile is null
                ? Results.NotFound(new { detail = "No local sample profile is mapped to this identity." })
                : Results.Ok(new
                {
                    profile.Username,
                    profile.DisplayName,
                    profile.OrganizationId,
                    profile.Clearance,
                });
        })
    .RequireAuthorization();

var documents = app.MapGroup("/api/documents");

documents.MapGet(
        "/",
        async (ClaimsPrincipal principal, SampleDbContext database, CancellationToken cancellationToken) =>
        {
            var username = principal.FindFirstValue("preferred_username") ?? principal.Identity?.Name;
            var profile = await database.UserProfiles.AsNoTracking()
                .SingleAsync(item => item.Username == username, cancellationToken);
            var items = await database.Documents.AsNoTracking()
                .Where(item => item.OrganizationId == profile.OrganizationId)
                .OrderByDescending(item => item.UpdatedAt)
                .ToListAsync(cancellationToken);
            return Results.Ok(items);
        })
    .RequireRuleGate("document", "list");

documents.MapGet(
        "/{id:int}",
        async (int id, SampleDbContext database, CancellationToken cancellationToken) =>
        {
            var document = await database.Documents.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
            return document is null ? Results.NotFound() : Results.Ok(document);
        })
    .RequireRuleGate("document", "read", "id");

documents.MapPost(
        "/",
        async (CreateDocumentRequest request, ClaimsPrincipal principal, SampleDbContext database,
            CancellationToken cancellationToken) =>
        {
            if (!IsValidDocumentInput(request.Title, request.Classification))
            {
                return Results.BadRequest(new { detail = "A title and a supported classification are required." });
            }

            var username = principal.FindFirstValue("preferred_username") ?? principal.Identity?.Name;
            var profile = await database.UserProfiles.SingleAsync(
                item => item.Username == username, cancellationToken);
            var document = new DocumentRecord
            {
                Title = request.Title.Trim(),
                Classification = request.Classification,
                OwnerUsername = profile.Username,
                OrganizationId = profile.OrganizationId,
                Status = DocumentStatuses.Draft,
                UpdatedAt = DateTimeOffset.UtcNow,
            };
            database.Documents.Add(document);
            await database.SaveChangesAsync(cancellationToken);
            return Results.Created($"/api/documents/{document.Id}", document);
        })
    .RequireRuleGate("document", "create");

documents.MapPut(
        "/{id:int}",
        async (int id, UpdateDocumentRequest request, SampleDbContext database,
            CancellationToken cancellationToken) =>
        {
            if (!IsValidDocumentInput(request.Title, request.Classification))
            {
                return Results.BadRequest(new { detail = "A title and a supported classification are required." });
            }

            var document = await database.Documents.SingleAsync(item => item.Id == id, cancellationToken);
            document.Title = request.Title.Trim();
            document.Classification = request.Classification;
            document.UpdatedAt = DateTimeOffset.UtcNow;
            await database.SaveChangesAsync(cancellationToken);
            return Results.Ok(document);
        })
    .RequireRuleGate("document", "update", "id");

documents.MapPost("/{id:int}/submit", Transition(DocumentStatuses.Submitted))
    .RequireRuleGate("document", "submit", "id");
documents.MapPost("/{id:int}/approve", Transition(DocumentStatuses.Approved))
    .RequireRuleGate("document", "approve", "id");
documents.MapPost("/{id:int}/reject", Transition(DocumentStatuses.Rejected))
    .RequireRuleGate("document", "reject", "id");

app.Run();

static Func<int, SampleDbContext, CancellationToken, Task<IResult>> Transition(string status)
{
    return async (id, database, cancellationToken) =>
    {
        var document = await database.Documents.SingleAsync(item => item.Id == id, cancellationToken);
        document.Status = status;
        document.UpdatedAt = DateTimeOffset.UtcNow;
        await database.SaveChangesAsync(cancellationToken);
        return Results.Ok(document);
    };
}

static bool IsValidDocumentInput(string title, string classification)
{
    return !string.IsNullOrWhiteSpace(title) &&
           title.Length <= 200 &&
           classification is "public" or "internal" or "confidential";
}

public sealed record CreateDocumentRequest(string Title, string Classification);

public sealed record UpdateDocumentRequest(string Title, string Classification);

public partial class Program;
