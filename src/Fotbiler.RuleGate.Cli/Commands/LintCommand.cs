using System.CommandLine;
using Fotbiler.RuleGate.Cli.Linting;
using Fotbiler.RuleGate.Cli.Output;
using Fotbiler.RuleGate.Manifest.Configuration;
using Fotbiler.RuleGate.Manifest.Loading;
using Fotbiler.RuleGate.Manifest.Validation;

namespace Fotbiler.RuleGate.Cli.Commands;

internal static class LintCommand
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
            new ManifestLintRunner(
                new RuleGateManifestYamlLoader(),
                new RuleGateManifestValidator(),
                new ManifestLinter());

        var command =
            new Command(
                "lint",
                "Find risky or unnecessarily complex manifest structures.");

        command.Arguments.Add(fileArgument);
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
