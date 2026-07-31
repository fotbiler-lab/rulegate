namespace Fotbiler.RuleGate.Abstractions.Policies;

public sealed record PolicyReloadDiagnostic
{
    public PolicyReloadDiagnostic(
        string sourceName,
        string code,
        string message,
        string? path = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceName);
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        if (path is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(path);
        }

        SourceName = sourceName;
        Code = code;
        Message = message;
        Path = path;
    }

    public string SourceName { get; }

    public string Code { get; }

    public string Message { get; }

    public string? Path { get; }
}
