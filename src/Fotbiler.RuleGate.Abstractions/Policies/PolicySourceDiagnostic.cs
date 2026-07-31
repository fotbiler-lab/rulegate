namespace Fotbiler.RuleGate.Abstractions.Policies;

public sealed record PolicySourceDiagnostic
{
    public PolicySourceDiagnostic(
        string code,
        string message,
        string? path = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        if (path is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(path);
        }

        Code = code;
        Message = message;
        Path = path;
    }

    public string Code { get; }

    public string Message { get; }

    public string? Path { get; }
}
