namespace Fotbiler.RuleGate.Manifest.Validation;

public sealed class ManifestValidationResult
{
    public ManifestValidationResult(
        IEnumerable<ManifestValidationError> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);

        var items = errors.ToArray();

        if (items.Any(static error => error is null))
        {
            throw new ArgumentException(
                "Manifest validation errors cannot contain null values.",
                nameof(errors));
        }

        Errors = Array.AsReadOnly(items);
    }

    public bool IsValid => Errors.Count == 0;

    public IReadOnlyList<ManifestValidationError> Errors
    {
        get;
    }
}
