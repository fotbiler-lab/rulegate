namespace RuleGate.DocumentApproval.Api.Domain;

public sealed class DocumentRecord
{
    public int Id { get; set; }

    public required string Title { get; set; }

    public required string OwnerUsername { get; set; }

    public required string OrganizationId { get; set; }

    public required string Classification { get; set; }

    public required string Status { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}

public static class DocumentClassifications
{
    public const string Public = "public";
    public const string Internal = "internal";
    public const string Confidential = "confidential";

    public static bool TryGetLevel(string? classification, out long level)
    {
        level = classification switch
        {
            Public => 1,
            Internal => 2,
            Confidential => 3,
            _ => 0,
        };

        return level > 0;
    }
}
