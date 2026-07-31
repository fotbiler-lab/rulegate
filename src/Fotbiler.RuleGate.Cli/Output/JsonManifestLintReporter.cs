using System.Text.Json;
using Fotbiler.RuleGate.Cli.Linting;

namespace Fotbiler.RuleGate.Cli.Output;

internal sealed class JsonManifestLintReporter :
    IManifestLintReporter
{
    private static readonly JsonSerializerOptions
        SerializerOptions =
            new(JsonSerializerDefaults.Web)
            {
                WriteIndented = true
            };

    public void Write(
        ManifestLintReport report,
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
