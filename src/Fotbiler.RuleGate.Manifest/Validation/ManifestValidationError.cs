namespace Fotbiler.RuleGate.Manifest.Validation;

public sealed record ManifestValidationError
{
    public ManifestValidationError(
        string code,
        string path,
        string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        Code = code;
        Path = path;
        Message = message;
    }

    public string Code { get; }

    public string Path { get; }

    public string Message { get; }
}
