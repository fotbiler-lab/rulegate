using System.CommandLine;
using System.Runtime.InteropServices;
using Fotbiler.RuleGate.Cli.ExitCodes;
using Fotbiler.RuleGate.Cli.Versioning;
using Fotbiler.RuleGate.Manifest.Configuration;

namespace Fotbiler.RuleGate.Cli.Commands;

internal static class InfoCommand
{
    public static Command Create()
    {
        var command =
            new Command(
                "info",
                "Show RuleGate CLI and runtime information.");

        command.SetAction(
            parseResult =>
            {
                var output =
                    parseResult
                        .InvocationConfiguration
                        .Output;

                output.WriteLine(
                    "Fotbiler RuleGate CLI");
                output.WriteLine(
                    $"Version: {CliVersionProvider.GetVersion()}");
                output.WriteLine(
                    $"Runtime: {RuntimeInformation.FrameworkDescription}");
                output.WriteLine(
                    $"Operating system: {RuntimeInformation.OSDescription}");
                output.WriteLine(
                    "Default manifest: " +
                    RuleGateManifestDefaults.FileName);
                output.WriteLine(
                    "Supported schema version: " +
                    RuleGateManifestDefaults
                        .SupportedSchemaVersion);

                return RuleGateExitCodes.Success;
            });

        return command;
    }
}
