using Fotbiler.RuleGate.Manifest.Models;

namespace Fotbiler.RuleGate.Manifest.Loading;

public sealed class ManifestLoadResult
{
    private ManifestLoadResult(
        RuleGateManifest? manifest,
        IReadOnlyList<ManifestLoadError> errors)
    {
        Manifest = manifest;
        Errors = errors;
    }

    public bool IsSuccess =>
        Manifest is not null &&
        Errors.Count == 0;

    public RuleGateManifest? Manifest { get; }

    public IReadOnlyList<ManifestLoadError> Errors
    {
        get;
    }

    public static ManifestLoadResult Success(
        RuleGateManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        return new ManifestLoadResult(
            manifest,
            Array.Empty<ManifestLoadError>());
    }

    public static ManifestLoadResult Failure(
        params ManifestLoadError[] errors)
    {
        ArgumentNullException.ThrowIfNull(errors);

        if (errors.Length == 0)
        {
            throw new ArgumentException(
                "A failed manifest load result must contain at least one error.",
                nameof(errors));
        }

        if (errors.Any(static error => error is null))
        {
            throw new ArgumentException(
                "Manifest load errors cannot contain null values.",
                nameof(errors));
        }

        return new ManifestLoadResult(
            manifest: null,
            errors: Array.AsReadOnly(
                errors.ToArray()));
    }
}
