namespace Fotbiler.RuleGate.Abstractions.Authorization;

public sealed record AuthorizationFailure
{
    public AuthorizationFailure(
        string code,
        string? requirementId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        if (requirementId is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(
                requirementId);
        }

        Code = code;
        RequirementId = requirementId;
    }

    public string Code { get; }

    public string? RequirementId { get; }
}
