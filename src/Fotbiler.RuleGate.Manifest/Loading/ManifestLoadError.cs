namespace Fotbiler.RuleGate.Manifest.Loading;

public sealed record ManifestLoadError
{
    public ManifestLoadError(
        string code,
        string message,
        long? line = null,
        long? column = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        if (line is <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(line),
                line,
                "Line must be greater than zero.");
        }

        if (column is <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(column),
                column,
                "Column must be greater than zero.");
        }

        Code = code;
        Message = message;
        Line = line;
        Column = column;
    }

    public string Code { get; }

    public string Message { get; }

    public long? Line { get; }

    public long? Column { get; }
}
