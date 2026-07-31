using Fotbiler.RuleGate.Cli.Testing;

namespace Fotbiler.RuleGate.Cli.Output;

internal interface IPolicyTestReporter
{
    void Write(
        PolicyTestReport report,
        TextWriter output,
        TextWriter error);
}
