using Microsoft.EntityFrameworkCore;
using RuleGate.DocumentApproval.Api.Domain;

namespace RuleGate.DocumentApproval.Api.Data;

public static class SampleData
{
    public static async Task InitializeAsync(
        SampleDbContext database,
        CancellationToken cancellationToken = default)
    {
        await database.Database.EnsureCreatedAsync(cancellationToken);

        if (!await database.UserProfiles.AnyAsync(cancellationToken))
        {
            database.UserProfiles.AddRange(
                new UserProfile
                {
                    Username = "qa-viewer",
                    DisplayName = "QA Viewer",
                    OrganizationId = "records",
                    Clearance = "public",
                },
                new UserProfile
                {
                    Username = "qa-manager",
                    DisplayName = "QA Document Manager",
                    OrganizationId = "records",
                    Clearance = "internal",
                },
                new UserProfile
                {
                    Username = "qa-approver",
                    DisplayName = "QA Approver",
                    OrganizationId = "records",
                    Clearance = "confidential",
                },
                new UserProfile
                {
                    Username = "qa-provision-approver",
                    DisplayName = "QA Provision Approver",
                    OrganizationId = "legal",
                    Clearance = "confidential",
                });
        }

        if (!await database.Documents.AnyAsync(cancellationToken))
        {
            database.Documents.AddRange(
                new DocumentRecord
                {
                    Title = "Records retention schedule",
                    OwnerUsername = "qa-manager",
                    OrganizationId = "records",
                    Classification = "internal",
                    Status = DocumentStatuses.Draft,
                    UpdatedAt = DateTimeOffset.UtcNow,
                },
                new DocumentRecord
                {
                    Title = "Supplier approval form",
                    OwnerUsername = "qa-manager",
                    OrganizationId = "records",
                    Classification = "confidential",
                    Status = DocumentStatuses.Submitted,
                    UpdatedAt = DateTimeOffset.UtcNow,
                },
                new DocumentRecord
                {
                    Title = "Legal review checklist",
                    OwnerUsername = "qa-provision-approver",
                    OrganizationId = "legal",
                    Classification = "internal",
                    Status = DocumentStatuses.Submitted,
                    UpdatedAt = DateTimeOffset.UtcNow,
                });
        }

        await database.SaveChangesAsync(cancellationToken);
    }
}

public static class DocumentStatuses
{
    public const string Draft = "draft";
    public const string Submitted = "submitted";
    public const string Approved = "approved";
    public const string Rejected = "rejected";
}
