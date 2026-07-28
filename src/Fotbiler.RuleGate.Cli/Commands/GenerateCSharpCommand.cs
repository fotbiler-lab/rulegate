using System.CommandLine;

using Fotbiler.RuleGate.Cli.Generation.CSharp;
using Fotbiler.RuleGate.Manifest.Compilation;
using Fotbiler.RuleGate.Manifest.Configuration;

namespace Fotbiler.RuleGate.Cli.Commands;

internal static class GenerateCSharpCommand
{
    public static Command Create()
    {
        var fileArgument =
            new Argument<string?>(
                "file")
            {
                Description =
                    "Manifest path. Defaults to " +
                    RuleGateManifestDefaults.FileName +
                    ".",
                DefaultValueFactory =
                    static _ => null,
                HelpName = "file"
            };

        var namespaceOption =
            new Option<string>(
                "--namespace",
                "-n")
            {
                Description =
                    "C# namespace for the generated constants.",
                HelpName = "namespace",
                Required = true
            };

        var outputOption =
            new Option<string?>(
                "--output",
                "-o")
            {
                Description =
                    "Output file. Writes source to standard output when omitted.",
                DefaultValueFactory =
                    static _ => null,
                HelpName = "file"
            };

        var checkOption =
            new Option<bool>(
                "--check")
            {
                Description =
                    "Verify that --output is current without modifying it."
            };

        var runner =
            new CSharpGenerationCommandRunner(
                new ManifestCSharpGenerationRunner(
                    new RuleGateManifestCompiler(),
                    new CSharpCodeGenerator()));

        var command =
            new Command(
                "csharp",
                "Generate deterministic C# constants from a RuleGate manifest.");

        command.Arguments.Add(
            fileArgument);

        command.Options.Add(
            namespaceOption);

        command.Options.Add(
            outputOption);

        command.Options.Add(
            checkOption);

        command.SetAction(
            async (
                parseResult,
                cancellationToken) =>
            {
                var path =
                    parseResult.GetValue(
                        fileArgument);

                var namespaceName =
                    parseResult.GetValue(
                        namespaceOption)
                    ?? string.Empty;

                var outputPath =
                    parseResult.GetValue(
                        outputOption);

                var check =
                    parseResult.GetValue(
                        checkOption);

                return await runner.RunAsync(
                    path,
                    namespaceName,
                    outputPath,
                    check,
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
