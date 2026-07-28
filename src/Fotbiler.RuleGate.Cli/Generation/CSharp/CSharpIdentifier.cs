using System.Text;

namespace Fotbiler.RuleGate.Cli.Generation.CSharp;

internal static class CSharpIdentifier
{
    public static string? TryCreate(
        string value)
    {
        ArgumentNullException.ThrowIfNull(
            value);

        var builder =
            new StringBuilder(
                value.Length);

        var capitalizeNext =
            true;

        foreach (var character in value)
        {
            if (!char.IsLetterOrDigit(character))
            {
                capitalizeNext = true;

                continue;
            }

            if (builder.Length == 0
                && char.IsDigit(character))
            {
                builder.Append('_');
            }

            builder.Append(
                capitalizeNext
                    ? char.ToUpperInvariant(character)
                    : character);

            capitalizeNext = false;
        }

        return builder.Length == 0
            ? null
            : builder.ToString();
    }
}
