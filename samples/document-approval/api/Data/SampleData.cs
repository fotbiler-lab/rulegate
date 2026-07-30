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
        await EnsureOrganizationSchedulesTableAsync(database, cancellationToken);

        var weekdaySchedule =
            OrganizationWorkingDays.Monday |
            OrganizationWorkingDays.Tuesday |
            OrganizationWorkingDays.Wednesday |
            OrganizationWorkingDays.Thursday |
            OrganizationWorkingDays.Friday;
        var schedules = new[]
        {
            new OrganizationSchedule
            {
                OrganizationId = "records",
                TimeZoneId = "Europe/Istanbul",
                WorkingDays = weekdaySchedule,
                StartMinute = 8 * 60,
                EndMinute = 18 * 60,
            },
            new OrganizationSchedule
            {
                OrganizationId = "legal",
                TimeZoneId = "Europe/Istanbul",
                WorkingDays = weekdaySchedule,
                StartMinute = 6 * 60,
                EndMinute = 20 * 60,
            },
        };
        var scheduledOrganizationIds = await database.OrganizationSchedules
            .Select(schedule => schedule.OrganizationId)
            .ToHashSetAsync(cancellationToken);

        database.OrganizationSchedules.AddRange(
            schedules.Where(schedule => !scheduledOrganizationIds.Contains(schedule.OrganizationId)));

        var profiles = new[]
        {
            new UserProfile
            {
                Username = "sample-viewer",
                DisplayName = "Sample Viewer",
                OrganizationId = "records",
                Clearance = "public",
            },
            new UserProfile
            {
                Username = "sample-manager",
                DisplayName = "Sample Document Manager",
                OrganizationId = "records",
                Clearance = "internal",
            },
            new UserProfile
            {
                Username = "sample-approver",
                DisplayName = "Sample Approver",
                OrganizationId = "records",
                Clearance = "confidential",
            },
            new UserProfile
            {
                Username = "sample-legal-approver",
                DisplayName = "Sample Legal Approver",
                OrganizationId = "legal",
                Clearance = "confidential",
            },
            new UserProfile
            {
                Username = "sample-admin",
                DisplayName = "Sample Administrator",
                OrganizationId = "records",
                Clearance = "confidential",
            },
        };
        var existingUsernames = await database.UserProfiles
            .Select(profile => profile.Username)
            .ToHashSetAsync(cancellationToken);

        database.UserProfiles.AddRange(
            profiles.Where(profile => !existingUsernames.Contains(profile.Username)));

        var legacyOwnerMappings = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["qa-manager"] = "sample-manager",
            ["qa-provision-approver"] = "sample-legal-approver",
        };
        var legacyDocuments = await database.Documents
            .Where(document => legacyOwnerMappings.Keys.Contains(document.OwnerUsername))
            .ToListAsync(cancellationToken);

        foreach (var document in legacyDocuments)
        {
            document.OwnerUsername = legacyOwnerMappings[document.OwnerUsername];
        }

        var seededDocuments = await database.Documents
            .Where(document =>
                document.Title == "Records retention schedule" ||
                document.Title == "Supplier approval form" ||
                document.Title == "Legal review checklist" ||
                document.Title == "Confidential legal opinion" ||
                document.Title == "Public filing guide" ||
                document.Title == "Confidential board minutes")
            .ToListAsync(cancellationToken);
        var documentsByTitle = seededDocuments
            .GroupBy(document => document.Title, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.First(),
                StringComparer.Ordinal);

        AddDocumentIfMissing(
            database,
            documentsByTitle,
            new DocumentRecord
            {
                Title = "Records retention schedule",
                OwnerUsername = "sample-manager",
                OrganizationId = "records",
                Classification = DocumentClassifications.Internal,
                Status = DocumentStatuses.Draft,
                UpdatedAt = DateTimeOffset.UtcNow,
            });
        AddDocumentIfMissing(
            database,
            documentsByTitle,
            new DocumentRecord
            {
                Title = "Confidential legal opinion",
                OwnerUsername = "sample-legal-approver",
                OrganizationId = "legal",
                Classification = DocumentClassifications.Confidential,
                Status = DocumentStatuses.Submitted,
                UpdatedAt = DateTimeOffset.UtcNow,
            });
        AddDocumentIfMissing(
            database,
            documentsByTitle,
            new DocumentRecord
            {
                Title = "Supplier approval form",
                OwnerUsername = "sample-manager",
                OrganizationId = "records",
                Classification = DocumentClassifications.Internal,
                Status = DocumentStatuses.Submitted,
                UpdatedAt = DateTimeOffset.UtcNow,
            });
        AddDocumentIfMissing(
            database,
            documentsByTitle,
            new DocumentRecord
            {
                Title = "Legal review checklist",
                OwnerUsername = "sample-legal-approver",
                OrganizationId = "legal",
                Classification = DocumentClassifications.Internal,
                Status = DocumentStatuses.Submitted,
                UpdatedAt = DateTimeOffset.UtcNow,
            });
        AddDocumentIfMissing(
            database,
            documentsByTitle,
            new DocumentRecord
            {
                Title = "Public filing guide",
                OwnerUsername = "sample-manager",
                OrganizationId = "records",
                Classification = DocumentClassifications.Public,
                Status = DocumentStatuses.Draft,
                UpdatedAt = DateTimeOffset.UtcNow,
            });
        AddDocumentIfMissing(
            database,
            documentsByTitle,
            new DocumentRecord
            {
                Title = "Confidential board minutes",
                OwnerUsername = "sample-admin",
                OrganizationId = "records",
                Classification = DocumentClassifications.Confidential,
                Status = DocumentStatuses.Submitted,
                UpdatedAt = DateTimeOffset.UtcNow,
            });

        if (documentsByTitle.TryGetValue("Supplier approval form", out var supplierDocument) &&
            supplierDocument.OwnerUsername == "sample-manager")
        {
            supplierDocument.Classification = DocumentClassifications.Internal;
        }

        await database.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureOrganizationSchedulesTableAsync(
        SampleDbContext database,
        CancellationToken cancellationToken)
    {
        await database.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS "OrganizationSchedules" (
                "OrganizationId" TEXT NOT NULL CONSTRAINT "PK_OrganizationSchedules" PRIMARY KEY,
                "TimeZoneId" TEXT NOT NULL,
                "WorkingDays" INTEGER NOT NULL,
                "StartMinute" INTEGER NOT NULL,
                "EndMinute" INTEGER NOT NULL
            );
            """,
            cancellationToken);
    }

    private static void AddDocumentIfMissing(
        SampleDbContext database,
        IDictionary<string, DocumentRecord> documentsByTitle,
        DocumentRecord document)
    {
        if (documentsByTitle.ContainsKey(document.Title))
        {
            return;
        }

        database.Documents.Add(document);
        documentsByTitle.Add(document.Title, document);
    }
}

public static class DocumentStatuses
{
    public const string Draft = "draft";
    public const string Submitted = "submitted";
    public const string Approved = "approved";
    public const string Rejected = "rejected";
}
