using Fotbiler.RuleGate.Cli.Linting;

namespace Fotbiler.RuleGate.Cli.Output;

internal interface IManifestLintReporter
{
    void Write(
        ManifestLintReport report,
        TextWriter output,
        TextWriter error);
}
