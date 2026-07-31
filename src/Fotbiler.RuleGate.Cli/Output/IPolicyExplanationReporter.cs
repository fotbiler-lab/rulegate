using Fotbiler.RuleGate.Cli.Explanation;

namespace Fotbiler.RuleGate.Cli.Output;

internal interface IPolicyExplanationReporter
{
    void Write(
        PolicyExplanationReport report,
        TextWriter output,
        TextWriter error);
}
