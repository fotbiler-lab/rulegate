using Fotbiler.RuleGate.Manifest.Models;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Fotbiler.RuleGate.Manifest.Loading;

public sealed class RuleGateManifestYamlLoader
{
    private const int MaximumRecursion = 64;

    private readonly IDeserializer _deserializer;

    public RuleGateManifestYamlLoader()
    {
        _deserializer =
            new DeserializerBuilder()
                .WithNamingConvention(
                    CamelCaseNamingConvention.Instance)
                .WithDuplicateKeyChecking()
                .WithMaximumRecursion(
                    MaximumRecursion)
                .Build();
    }

    public ManifestLoadResult LoadFromText(
        string yaml)
    {
        ArgumentNullException.ThrowIfNull(yaml);

        if (string.IsNullOrWhiteSpace(yaml))
        {
            return ManifestLoadResult.Failure(
                new ManifestLoadError(
                    ManifestLoadCodes.EmptyContent,
                    "Manifest YAML content is empty."));
        }

        try
        {
            using var reader =
                new StringReader(yaml);

            var manifest =
                _deserializer.Deserialize<
                    RuleGateManifest>(reader);

            if (manifest is null)
            {
                return ManifestLoadResult.Failure(
                    new ManifestLoadError(
                        ManifestLoadCodes.RootRequired,
                        "Manifest YAML must contain a root object."));
            }

            return ManifestLoadResult.Success(
                manifest);
        }
        catch (YamlException exception)
        {
            return ManifestLoadResult.Failure(
                new ManifestLoadError(
                    ManifestLoadCodes.InvalidYaml,
                    GetErrorMessage(exception),
                    GetPosition(exception.Start.Line),
                    GetPosition(exception.Start.Column)));
        }
    }

    public async ValueTask<ManifestLoadResult>
        LoadFromFileAsync(
            string path,
            CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var yaml = await ReadAllTextAsync(
                path,
                cancellationToken);

            return LoadFromText(yaml);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (FileNotFoundException exception)
        {
            return CreateFileError(
                ManifestLoadCodes.FileNotFound,
                exception);
        }
        catch (DirectoryNotFoundException exception)
        {
            return CreateFileError(
                ManifestLoadCodes.FileNotFound,
                exception);
        }
        catch (UnauthorizedAccessException exception)
        {
            return CreateFileError(
                ManifestLoadCodes.FileReadFailed,
                exception);
        }
        catch (IOException exception)
        {
            return CreateFileError(
                ManifestLoadCodes.FileReadFailed,
                exception);
        }
    }

    private static async Task<string> ReadAllTextAsync(
        string path,
        CancellationToken cancellationToken)
    {
#if NETSTANDARD2_0
        using var reader = File.OpenText(path);

        var content = await reader.ReadToEndAsync();

        cancellationToken.ThrowIfCancellationRequested();

        return content;
#else
        return await File.ReadAllTextAsync(
            path,
            cancellationToken);
#endif
    }

    private static ManifestLoadResult CreateFileError(
        string code,
        Exception exception)
    {
        return ManifestLoadResult.Failure(
            new ManifestLoadError(
                code,
                exception.Message));
    }

    private static string GetErrorMessage(
        YamlException exception)
    {
        return exception.InnerException?.Message ??
               exception.Message;
    }

    private static long? GetPosition(long value)
    {
        return value > 0
            ? value
            : null;
    }
}
