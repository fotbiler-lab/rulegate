using System.CommandLine;

namespace Fotbiler.RuleGate.Cli.Commands;

internal static class GenerateCommand
{
    public static Command Create()
    {
        var command =
            new Command(
                "generate",
                "Generate source code from a validated RuleGate manifest.");

        command.Subcommands.Add(
            GenerateCSharpCommand.Create());

        return command;
    }
}
