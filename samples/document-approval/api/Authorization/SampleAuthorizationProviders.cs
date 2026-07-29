using System.Security.Claims;
using Fotbiler.RuleGate.Abstractions.Attributes;
using Fotbiler.RuleGate.AspNetCore.Enrichment;
using Microsoft.EntityFrameworkCore;
using RuleGate.DocumentApproval.Api.Data;

namespace RuleGate.DocumentApproval.Api.Authorization;

public sealed class UserProfileAttributeProvider(SampleDbContext database)
    : IRuleGateSubjectAttributeProvider
{
    public async ValueTask<RuleGateAttributeProviderResult> ProvideAttributesAsync(
        RuleGateAttributeProviderContext context,
        CancellationToken cancellationToken = default)
    {
        var username =
            context.Principal.FindFirstValue("preferred_username") ??
            context.Principal.Identity?.Name;

        if (string.IsNullOrWhiteSpace(username))
        {
            return RuleGateAttributeProviderResult.MissingRequiredData();
        }

        var profile = await database.UserProfiles
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Username == username, cancellationToken);

        if (profile is null)
        {
            return RuleGateAttributeProviderResult.MissingRequiredData();
        }

        return RuleGateAttributeProviderResult.Success(
            new AuthorizationAttributes(
            [
                new("username", profile.Username),
                new("organizationId", profile.OrganizationId),
                new("clearance", profile.Clearance),
            ]));
    }
}

public sealed class DocumentAttributeProvider(SampleDbContext database)
    : IRuleGateResourceAttributeProvider
{
    public async ValueTask<RuleGateAttributeProviderResult> ProvideAttributesAsync(
        RuleGateAttributeProviderContext context,
        CancellationToken cancellationToken = default)
    {
        if (!string.Equals(context.Resource.Type, "document", StringComparison.Ordinal))
        {
            return RuleGateAttributeProviderResult.Success();
        }

        if (context.Resource.Id is null &&
            (context.Action == "list" || context.Action == "create"))
        {
            return RuleGateAttributeProviderResult.Success();
        }

        if (!int.TryParse(context.Resource.Id, out var documentId))
        {
            return RuleGateAttributeProviderResult.MissingRequiredData();
        }

        var document = await database.Documents
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == documentId, cancellationToken);

        if (document is null)
        {
            return RuleGateAttributeProviderResult.MissingRequiredData();
        }

        return RuleGateAttributeProviderResult.Success(
            new AuthorizationAttributes(
            [
                new("ownerUsername", document.OwnerUsername),
                new("organizationId", document.OrganizationId),
                new("classification", document.Classification),
                new("status", document.Status),
            ]));
    }
}

public sealed class ApiRequestContextProvider : IRuleGateContextAttributeProvider
{
    public ValueTask<RuleGateAttributeProviderResult> ProvideAttributesAsync(
        RuleGateAttributeProviderContext context,
        CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(
            RuleGateAttributeProviderResult.Success(
                new AuthorizationAttributes([new("requestChannel", "api")])));
    }
}
