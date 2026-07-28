using System.CommandLine;
using Fotbiler.RuleGate.Cli.Output;
using Fotbiler.RuleGate.Cli.Validation;
using Fotbiler.RuleGate.Manifest.Compilation;
using Fotbiler.RuleGate.Manifest.Configuration;

namespace Fotbiler.RuleGate.Cli.Commands;

internal static class ValidateCommand
{
    public static Command Create()
    {
        var fileArgument =
            new Argument<string?>("file")
            {
                Description =
                    "Manifest path. Defaults to " +
                    RuleGateManifestDefaults.FileName + ".",

                DefaultValueFactory =
                    static _ => null,

                HelpName = "file"
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
            new ManifestValidationRunner(
                new RuleGateManifestCompiler());

        var command =
            new Command(
                "validate",
                "Validate and compile a RuleGate manifest.");

        command.Arguments.Add(
            fileArgument);

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
