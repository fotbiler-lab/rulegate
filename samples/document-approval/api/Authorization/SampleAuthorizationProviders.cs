using System.Security.Claims;
using Fotbiler.RuleGate.Abstractions.Attributes;
using Fotbiler.RuleGate.AspNetCore.Enrichment;
using Microsoft.EntityFrameworkCore;
using RuleGate.DocumentApproval.Api.Data;
using RuleGate.DocumentApproval.Api.Domain;

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

        if (!DocumentClassifications.TryGetLevel(profile.Clearance, out var clearanceLevel))
        {
            return RuleGateAttributeProviderResult.MissingRequiredData();
        }

        return RuleGateAttributeProviderResult.Success(
            new AuthorizationAttributes(
            [
                new("username", profile.Username),
                new("organizationId", profile.OrganizationId),
                new("clearance", profile.Clearance),
                new("clearanceLevel", clearanceLevel),
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
            (context.Action == "list" || context.Action == "create" || context.Action == "classify"))
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

        if (!DocumentClassifications.TryGetLevel(
                document.Classification,
                out var classificationLevel))
        {
            return RuleGateAttributeProviderResult.MissingRequiredData();
        }

        return RuleGateAttributeProviderResult.Success(
            new AuthorizationAttributes(
            [
                new("ownerUsername", document.OwnerUsername),
                new("organizationId", document.OrganizationId),
                new("classification", document.Classification),
                new("classificationLevel", classificationLevel),
                new("status", document.Status),
            ]));
    }
}

public sealed class ApiRequestContextProvider(SampleDbContext database)
    : IRuleGateContextAttributeProvider
{
    public async ValueTask<RuleGateAttributeProviderResult> ProvideAttributesAsync(
        RuleGateAttributeProviderContext context,
        CancellationToken cancellationToken = default)
    {
        var attributes = new List<KeyValuePair<string, object?>>
        {
            new("requestChannel", "api"),
        };

        if (!string.Equals(context.Action, "read", StringComparison.Ordinal))
        {
            return RuleGateAttributeProviderResult.Success(
                new AuthorizationAttributes(attributes));
        }

        var username =
            context.Principal.FindFirstValue("preferred_username") ??
            context.Principal.Identity?.Name;

        if (string.IsNullOrWhiteSpace(username))
        {
            return RuleGateAttributeProviderResult.MissingRequiredData();
        }

        var schedule = await (
                from profile in database.UserProfiles.AsNoTracking()
                join item in database.OrganizationSchedules.AsNoTracking()
                    on profile.OrganizationId equals item.OrganizationId
                where profile.Username == username
                select item)
            .SingleOrDefaultAsync(cancellationToken);

        if (schedule is null ||
            !schedule.TryIsOpen(context.Context.EvaluationTime, out var businessHoursOpen))
        {
            return RuleGateAttributeProviderResult.MissingRequiredData();
        }

        attributes.Add(new("organizationBusinessHoursOpen", businessHoursOpen));
        attributes.Add(new("organizationScheduleTimeZone", schedule.TimeZoneId));

        return RuleGateAttributeProviderResult.Success(
            new AuthorizationAttributes(attributes));
    }
}
