using System.CommandLine;
using System.Runtime.InteropServices;
using Fotbiler.RuleGate.Cli.ExitCodes;
using Fotbiler.RuleGate.Cli.Testing;
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
                    "RuleGate CLI");
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
                output.WriteLine(
                    "Default policy tests: " +
                    PolicyTestDefaults.FileName);
                output.WriteLine(
                    "Supported policy test schema version: " +
                    PolicyTestDefaults
                        .SupportedSchemaVersion);

                return RuleGateExitCodes.Success;
            });

        return command;
    }
}
