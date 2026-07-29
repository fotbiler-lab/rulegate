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
