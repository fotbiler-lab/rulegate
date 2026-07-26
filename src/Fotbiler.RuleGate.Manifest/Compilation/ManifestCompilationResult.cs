using Fotbiler.RuleGate.Abstractions.Policies;
using Fotbiler.RuleGate.Manifest.Loading;
using Fotbiler.RuleGate.Manifest.Validation;

namespace Fotbiler.RuleGate.Manifest.Compilation;

public sealed class ManifestCompilationResult
{
    private ManifestCompilationResult(
        IReadOnlyList<PolicyDefinition> policies,
        IReadOnlyList<ManifestLoadError> loadErrors,
        IReadOnlyList<ManifestValidationError> validationErrors)
    {
        Policies = policies;
        LoadErrors = loadErrors;
        ValidationErrors = validationErrors;
    }

    public bool IsSuccess =>
        LoadErrors.Count == 0 &&
        ValidationErrors.Count == 0;

    public IReadOnlyList<PolicyDefinition> Policies
    {
        get;
    }

    public IReadOnlyList<ManifestLoadError> LoadErrors
    {
        get;
    }

    public IReadOnlyList<ManifestValidationError>
        ValidationErrors
    {
        get;
    }

    public static ManifestCompilationResult Success(
        IEnumerable<PolicyDefinition> policies)
    {
        ArgumentNullException.ThrowIfNull(policies);

        var items = policies.ToArray();

        if (items.Any(static policy => policy is null))
        {
            throw new ArgumentException(
                "Compiled policies cannot contain null values.",
                nameof(policies));
        }

        return new ManifestCompilationResult(
            policies: Array.AsReadOnly(items),
            loadErrors:
                Array.Empty<ManifestLoadError>(),
            validationErrors:
                Array.Empty<ManifestValidationError>());
    }

    public static ManifestCompilationResult LoadFailure(
        IEnumerable<ManifestLoadError> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);

        var items = errors.ToArray();

        if (items.Length == 0)
        {
            throw new ArgumentException(
                "A load failure must contain at least one error.",
                nameof(errors));
        }

        if (items.Any(static error => error is null))
        {
            throw new ArgumentException(
                "Load errors cannot contain null values.",
                nameof(errors));
        }

        return new ManifestCompilationResult(
            policies:
                Array.Empty<PolicyDefinition>(),
            loadErrors:
                Array.AsReadOnly(items),
            validationErrors:
                Array.Empty<ManifestValidationError>());
    }

    public static ManifestCompilationResult
        ValidationFailure(
            IEnumerable<ManifestValidationError> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);

        var items = errors.ToArray();

        if (items.Length == 0)
        {
            throw new ArgumentException(
                "A validation failure must contain at least one error.",
                nameof(errors));
        }

        if (items.Any(static error => error is null))
        {
            throw new ArgumentException(
                "Validation errors cannot contain null values.",
                nameof(errors));
        }

        return new ManifestCompilationResult(
            policies:
                Array.Empty<PolicyDefinition>(),
            loadErrors:
                Array.Empty<ManifestLoadError>(),
            validationErrors:
                Array.AsReadOnly(items));
    }
}
