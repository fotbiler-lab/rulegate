using System.CommandLine;
using Fotbiler.RuleGate.Cli.Output;
using Fotbiler.RuleGate.Cli.Testing;
using Fotbiler.RuleGate.Manifest.Compilation;

namespace Fotbiler.RuleGate.Cli.Commands;

internal static class TestCommand
{
    public static Command Create()
    {
        var fileArgument =
            new Argument<string?>("file")
            {
                Description =
                    "Policy test fixture path. Defaults to " +
                    PolicyTestDefaults.FileName + ".",

                DefaultValueFactory =
                    static _ => null,

                HelpName = "file"
            };

        var filterOption =
            new Option<string?>("--filter")
            {
                Description =
                    "Run tests whose identifiers contain this text.",

                HelpName = "text"
            };

        var formatOption =
            new Option<string>(
                "--format",
                "-f")
            {
                Description =
                    "Output format: text or json.",

                DefaultValueFactory =
                    static _ => "text",

                HelpName = "text|json"
            };

        formatOption.AcceptOnlyFromAmong(
            "text",
            "json");

        var runner =
            new PolicyTestRunner(
                new PolicyTestFixtureCompiler(),
                new RuleGateManifestCompiler());

        var command =
            new Command(
                "test",
                "Evaluate authorization policy test fixtures deterministically.");

        command.Arguments.Add(
            fileArgument);

        command.Options.Add(
            filterOption);

        command.Options.Add(
            formatOption);

        command.SetAction(
            async (
                parseResult,
                cancellationToken) =>
            {
                var path =
                    parseResult.GetValue(
                        fileArgument);

                var filter =
                    parseResult.GetValue(
                        filterOption);

                var requestedFormat =
                    parseResult.GetValue(
                        formatOption)
                    ?? "text";

                var outputFormat =
                    string.Equals(
                        requestedFormat,
                        "json",
                        StringComparison.Ordinal)
                        ? ValidationOutputFormat.Json
                        : ValidationOutputFormat.Text;

                return await runner.RunAsync(
                    path,
                    filter,
                    outputFormat,
                    parseResult
                        .InvocationConfiguration
                        .Output,
                    parseResult
                        .InvocationConfiguration
                        .Error,
                    cancellationToken);
            });

        return command;
    }
}
