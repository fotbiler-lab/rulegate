using Fotbiler.RuleGate.Cli.Testing;

namespace Fotbiler.RuleGate.Cli.Explanation;

internal static class PolicyExplanationDiagnosticCodes
{
    public const string TestNotFound =
        "RGEXPLAIN_TEST_NOT_FOUND";
}

internal sealed record PolicyExplanationRequirement(
    string Kind,
    string Outcome,
    string? RequirementId,
    IReadOnlyList<string> FailureCodes,
    string? AttributeSource,
    string? AttributeName,
    string? ComparedAttributeSource,
    string? ComparedAttributeName,
    IReadOnlyList<PolicyExplanationRequirement> Children);

internal sealed record PolicyExplanationReport(
    bool IsValid,
    string Fixture,
    string? Manifest,
    string? TestId,
    string? Outcome,
    string? PolicyId,
    IReadOnlyList<string> FailureCodes,
    bool SensitiveValuesRedacted,
    IReadOnlyList<PolicyTestDiagnostic> Errors,
    IReadOnlyList<PolicyExplanationRequirement> Requirements);
