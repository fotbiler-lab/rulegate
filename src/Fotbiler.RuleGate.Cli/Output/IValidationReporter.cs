using Fotbiler.RuleGate.Cli.Validation;

namespace Fotbiler.RuleGate.Cli.Output;

internal interface IValidationReporter
{
    void Write(
        ManifestValidationReport report,
        TextWriter output,
        TextWriter error);
}
