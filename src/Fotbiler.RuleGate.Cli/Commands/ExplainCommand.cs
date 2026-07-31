using System.CommandLine;
using Fotbiler.RuleGate.Cli.Explanation;
using Fotbiler.RuleGate.Cli.Output;
using Fotbiler.RuleGate.Cli.Testing;
using Fotbiler.RuleGate.Manifest.Compilation;

namespace Fotbiler.RuleGate.Cli.Commands;

internal static class ExplainCommand
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

        var testOption =
            new Option<string>("--test")
            {
                Description =
                    "Exact policy test identifier to explain.",

                HelpName = "id",
                Required = true
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
            new PolicyExplanationRunner(
                new PolicyTestFixtureCompiler(),
                new RuleGateManifestCompiler());

        var command =
            new Command(
                "explain",
                "Explain one policy decision without exposing request values.");

        command.Arguments.Add(fileArgument);
        command.Options.Add(testOption);
        command.Options.Add(formatOption);

        command.SetAction(
            async (
                parseResult,
                cancellationToken) =>
            {
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
                    parseResult.GetValue(
                        fileArgument),
                    parseResult.GetValue(
                        testOption)!,
                    outputFormat,
                    parseResult.InvocationConfiguration
                        .Output,
                    parseResult.InvocationConfiguration
                        .Error,
                    cancellationToken);
            });

        return command;
    }
}
