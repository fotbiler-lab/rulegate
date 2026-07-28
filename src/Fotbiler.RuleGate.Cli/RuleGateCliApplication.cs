using System.CommandLine;
using System.CommandLine.Help;
using Fotbiler.RuleGate.Cli.Commands;
using Fotbiler.RuleGate.Cli.ExitCodes;

namespace Fotbiler.RuleGate.Cli;

internal static class RuleGateCliApplication
{
    public static async Task<int> RunAsync(
        IReadOnlyList<string> arguments,
        TextWriter? output = null,
        TextWriter? error = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        output ??= Console.Out;
        error ??= Console.Error;

        var rootCommand =
            CreateRootCommand();

        IReadOnlyList<string> effectiveArguments =
            arguments.Count == 0
                ? ["--help"]
                : arguments;

        var parseResult =
            rootCommand.Parse(
                effectiveArguments);

        if (parseResult.Errors.Count != 0)
        {
            foreach (var parseError in
                     parseResult.Errors)
            {
                await error.WriteLineAsync(
                    $"error: {parseError.Message}");
            }

            await error.WriteLineAsync(
                "Run 'rulegate --help' " +
                "for usage information.");

            return RuleGateExitCodes.UsageError;
        }

        var invocationConfiguration =
            new InvocationConfiguration
            {
                Output = output,
                Error = error,
                EnableDefaultExceptionHandler = false
            };

        try
        {
            return await parseResult.InvokeAsync(
                invocationConfiguration,
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            await error.WriteLineAsync(
                "Operation canceled.");

            return RuleGateExitCodes.Canceled;
        }
        catch (Exception)
        {
            await error.WriteLineAsync(
                "An unexpected RuleGate CLI error occurred.");

            return RuleGateExitCodes.InternalError;
        }
    }

    private static Command CreateRootCommand()
    {
        var rootCommand =
            new Command(
                "rulegate",
                "Validate, generate, and manage Fotbiler RuleGate policies.");

        rootCommand.Options.Add(
            new HelpOption());

        rootCommand.Options.Add(
            new VersionOption());

        rootCommand.Subcommands.Add(
            ValidateCommand.Create());

        rootCommand.Subcommands.Add(
            GenerateCommand.Create());

        rootCommand.Subcommands.Add(
            InfoCommand.Create());

        return rootCommand;
    }
}
