using System.Reflection;

namespace Fotbiler.RuleGate.Cli.Versioning;

internal static class CliVersionProvider
{
    public static string GetVersion()
    {
        var informationalVersion =
            typeof(CliVersionProvider)
                .Assembly
                .GetCustomAttribute<
                    AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion;

        if (!string.IsNullOrWhiteSpace(
                informationalVersion))
        {
            var metadataIndex =
                informationalVersion.IndexOf(
                    '+',
                    StringComparison.Ordinal);

            return metadataIndex >= 0
                ? informationalVersion[..metadataIndex]
                : informationalVersion;
        }

        return typeof(CliVersionProvider)
                .Assembly
                .GetName()
                .Version
                ?.ToString(3)
            ?? "unknown";
    }
}
