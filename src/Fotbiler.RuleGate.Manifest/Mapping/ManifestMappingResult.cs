using Fotbiler.RuleGate.Abstractions.Policies;
using Fotbiler.RuleGate.Manifest.Validation;

namespace Fotbiler.RuleGate.Manifest.Mapping;

public sealed class ManifestMappingResult
{
    private ManifestMappingResult(
        IReadOnlyList<PolicyDefinition> policies,
        IReadOnlyList<ManifestValidationError> errors)
    {
        Policies = policies;
        Errors = errors;
    }

    public bool IsSuccess => Errors.Count == 0;

    public IReadOnlyList<PolicyDefinition> Policies
    {
        get;
    }

    public IReadOnlyList<ManifestValidationError> Errors
    {
        get;
    }

    public static ManifestMappingResult Success(
        IEnumerable<PolicyDefinition> policies)
    {
        ArgumentNullException.ThrowIfNull(policies);

        var items = policies.ToArray();

        if (items.Any(static policy => policy is null))
        {
            throw new ArgumentException(
                "Mapped policies cannot contain null values.",
                nameof(policies));
        }

        return new ManifestMappingResult(
            policies: Array.AsReadOnly(items),
            errors:
                Array.Empty<ManifestValidationError>());
    }

    public static ManifestMappingResult Failure(
        IEnumerable<ManifestValidationError> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);

        var items = errors.ToArray();

        if (items.Length == 0)
        {
            throw new ArgumentException(
                "A failed manifest mapping must contain at least one validation error.",
                nameof(errors));
        }

        if (items.Any(static error => error is null))
        {
            throw new ArgumentException(
                "Manifest mapping errors cannot contain null values.",
                nameof(errors));
        }

        return new ManifestMappingResult(
            policies: Array.Empty<PolicyDefinition>(),
            errors: Array.AsReadOnly(items));
    }
}
