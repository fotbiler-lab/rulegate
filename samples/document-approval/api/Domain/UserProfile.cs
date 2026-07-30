namespace RuleGate.DocumentApproval.Api.Domain;

public sealed class UserProfile
{
    public required string Username { get; set; }

    public required string DisplayName { get; set; }

    public required string OrganizationId { get; set; }

    public required string Clearance { get; set; }
}
