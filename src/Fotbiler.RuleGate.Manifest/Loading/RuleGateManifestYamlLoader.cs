using System.Text;
using Fotbiler.RuleGate.Manifest.Configuration;
using Fotbiler.RuleGate.Manifest.Models;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Fotbiler.RuleGate.Manifest.Loading;

public sealed class RuleGateManifestYamlLoader
{
    private const int MaximumRecursion = 64;

    private const int FileReadBufferSize = 4_096;

    private static readonly Encoding StrictUtf8 =
        new UTF8Encoding(
            encoderShouldEmitUTF8Identifier: false,
            throwOnInvalidBytes: true);

    private static readonly byte[] Utf8Preamble =
        Encoding.UTF8.GetPreamble();

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

        int utf8ByteCount;

        try
        {
            utf8ByteCount =
                StrictUtf8.GetByteCount(yaml);
        }
        catch (EncoderFallbackException)
        {
            return CreateInvalidEncodingResult(
                "Manifest YAML text contains invalid Unicode data.");
        }

        if (utf8ByteCount >
            RuleGateManifestResourceLimits
                .MaximumManifestContentByteCount)
        {
            return CreateContentTooLargeResult();
        }

        if (string.IsNullOrWhiteSpace(yaml))
        {
            return ManifestLoadResult.Failure(
                new ManifestLoadError(
                    ManifestLoadCodes.EmptyContent,
                    "Manifest YAML content is empty."));
        }

        try
        {
            var securityError =
                ValidateYamlSecurityProfile(yaml);

            if (securityError is not null)
            {
                return ManifestLoadResult.Failure(
                    securityError);
            }

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
            var fileReadResult =
                await ReadAllTextAsync(
                    path,
                    cancellationToken);

            if (fileReadResult.Status ==
                ManifestFileReadStatus.TooLarge)
            {
                return CreateContentTooLargeResult();
            }

            if (fileReadResult.Status ==
                ManifestFileReadStatus.InvalidEncoding)
            {
                return CreateInvalidEncodingResult(
                    "Manifest files must contain valid UTF-8 data.");
            }

            return LoadFromText(
                fileReadResult.Content!);
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

    private static ManifestLoadError?
        ValidateYamlSecurityProfile(
            string yaml)
    {
        using var reader =
            new StringReader(yaml);

        var parser =
            new Parser(reader);

        var documentCount = 0;

        while (parser.MoveNext())
        {
            var current =
                parser.Current ??
                throw new YamlException(
                    "The YAML parser returned an empty event.");

            if (current is DocumentStart)
            {
                documentCount++;

                if (documentCount > 1)
                {
                    return CreateSecurityProfileError(
                        "A RuleGate manifest must contain exactly one YAML document.",
                        current);
                }

                continue;
            }

            if (current is AnchorAlias)
            {
                return CreateSecurityProfileError(
                    "YAML aliases are not supported in RuleGate manifests.",
                    current);
            }

            if (current is not NodeEvent node)
            {
                continue;
            }

            if (!node.Anchor.IsEmpty)
            {
                return CreateSecurityProfileError(
                    "YAML anchors are not supported in RuleGate manifests.",
                    node);
            }

            if (node.Tag.IsLocal ||
                node.Tag.IsGlobal)
            {
                return CreateSecurityProfileError(
                    "Explicit YAML tags are not supported in RuleGate manifests.",
                    node);
            }
        }

        return null;
    }

    private static ManifestLoadError
        CreateSecurityProfileError(
            string message,
            ParsingEvent parsingEvent)
    {
        return new ManifestLoadError(
            ManifestLoadCodes.InvalidYaml,
            message,
            GetPosition(parsingEvent.Start.Line),
            GetPosition(parsingEvent.Start.Column));
    }

    private static async Task<ManifestFileReadResult>
        ReadAllTextAsync(
            string path,
            CancellationToken cancellationToken)
    {
        using var stream =
            new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                FileReadBufferSize,
                useAsync: true);

        if (stream.Length >
            RuleGateManifestResourceLimits
                .MaximumManifestContentByteCount)
        {
            return ManifestFileReadResult.TooLarge();
        }

        using var content =
            new MemoryStream(
                capacity: (int)stream.Length);

        var buffer =
            new byte[FileReadBufferSize];

        while (true)
        {
            cancellationToken
                .ThrowIfCancellationRequested();

            var read =
                await stream.ReadAsync(
                    buffer,
                    0,
                    buffer.Length,
                    cancellationToken);

            if (read == 0)
            {
                break;
            }

            if (content.Length + read >
                RuleGateManifestResourceLimits
                    .MaximumManifestContentByteCount)
            {
                return ManifestFileReadResult.TooLarge();
            }

            content.Write(
                buffer,
                0,
                read);
        }

        cancellationToken
            .ThrowIfCancellationRequested();

        var bytes =
            content.ToArray();

        var preambleLength =
            HasUtf8Preamble(bytes)
                ? Utf8Preamble.Length
                : 0;

        try
        {
            var text =
                StrictUtf8.GetString(
                    bytes,
                    preambleLength,
                    bytes.Length - preambleLength);

            return ManifestFileReadResult.Success(
                text);
        }
        catch (DecoderFallbackException)
        {
            return ManifestFileReadResult
                .InvalidEncoding();
        }
    }

    private static bool HasUtf8Preamble(
        IReadOnlyList<byte> bytes)
    {
        if (bytes.Count <
            Utf8Preamble.Length)
        {
            return false;
        }

        for (var index = 0;
             index < Utf8Preamble.Length;
             index++)
        {
            if (bytes[index] !=
                Utf8Preamble[index])
            {
                return false;
            }
        }

        return true;
    }

    private static ManifestLoadResult
        CreateContentTooLargeResult()
    {
        return ManifestLoadResult.Failure(
            new ManifestLoadError(
                ManifestLoadCodes.ContentTooLarge,
                $"Manifest content cannot exceed {RuleGateManifestResourceLimits.MaximumManifestContentByteCount} bytes."));
    }

    private static ManifestLoadResult
        CreateInvalidEncodingResult(
            string message)
    {
        return ManifestLoadResult.Failure(
            new ManifestLoadError(
                ManifestLoadCodes.InvalidYaml,
                message));
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

    private enum ManifestFileReadStatus
    {
        Success = 0,
        TooLarge = 1,
        InvalidEncoding = 2
    }

    private sealed class ManifestFileReadResult
    {
        private ManifestFileReadResult(
            string? content,
            ManifestFileReadStatus status)
        {
            Content = content;
            Status = status;
        }

        internal string? Content { get; }

        internal ManifestFileReadStatus Status { get; }

        internal static ManifestFileReadResult Success(
            string content)
        {
            return new ManifestFileReadResult(
                content,
                ManifestFileReadStatus.Success);
        }

        internal static ManifestFileReadResult TooLarge()
        {
            return new ManifestFileReadResult(
                content: null,
                ManifestFileReadStatus.TooLarge);
        }

        internal static ManifestFileReadResult
            InvalidEncoding()
        {
            return new ManifestFileReadResult(
                content: null,
                ManifestFileReadStatus.InvalidEncoding);
        }
    }
}
