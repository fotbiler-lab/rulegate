using System.Text.Json;
using Fotbiler.RuleGate.Cli.Validation;

namespace Fotbiler.RuleGate.Cli.Output;

internal sealed class JsonValidationReporter :
    IValidationReporter
{
    private static readonly JsonSerializerOptions
        SerializerOptions =
            new(JsonSerializerDefaults.Web)
            {
                WriteIndented = true
            };

    public void Write(
        ManifestValidationReport report,
        TextWriter output,
        TextWriter error)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(output);
        ArgumentNullException.ThrowIfNull(error);

        output.WriteLine(
            JsonSerializer.Serialize(
                report,
                SerializerOptions));
    }
}
