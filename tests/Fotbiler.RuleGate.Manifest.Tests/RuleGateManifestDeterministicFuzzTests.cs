using System.Text;
using Fotbiler.RuleGate.Manifest.Compilation;
using Fotbiler.RuleGate.Manifest.Loading;
using Xunit.Sdk;

namespace Fotbiler.RuleGate.Manifest.Tests;

public sealed class
    RuleGateManifestDeterministicFuzzTests
{
    private readonly RuleGateManifestYamlLoader _loader =
        new();

    private readonly RuleGateManifestCompiler _compiler =
        new();

    [Theory]
    [InlineData(24_301)]
    [InlineData(12_648_430)]
    [InlineData(195_936_478)]
    [InlineData(1_597_463_007)]
    public void
        RandomText_IsDeterministicAndFailClosed(
            int seed)
    {
        var random =
            new Random(seed);

        for (var caseIndex = 0;
             caseIndex < 128;
             caseIndex++)
        {
            var yaml =
                CreateRandomText(
                    random);

            AssertTextCase(
                yaml,
                seed,
                caseIndex);
        }
    }

    [Theory]
    [InlineData(24_301)]
    [InlineData(12_648_430)]
    [InlineData(195_936_478)]
    [InlineData(1_597_463_007)]
    public void
        StructuredManifestMutations_AreDeterministicAndFailClosed(
            int seed)
    {
        var random =
            new Random(seed);

        for (var caseIndex = 0;
             caseIndex < 96;
             caseIndex++)
        {
            var yaml =
                CreateStructuredManifest(
                    random,
                    caseIndex);

            AssertTextCase(
                yaml,
                seed,
                caseIndex);
        }
    }

    [Theory]
    [InlineData(24_301)]
    [InlineData(12_648_430)]
    [InlineData(195_936_478)]
    [InlineData(1_597_463_007)]
    public async Task
        RandomFileBytes_AreDeterministicAndFailClosed(
            int seed)
    {
        var random =
            new Random(seed);

        var path =
            Path.Combine(
                Path.GetTempPath(),
                $"rulegate-fuzz-{seed}-{Guid.NewGuid():N}.yaml");

        try
        {
            for (var caseIndex = 0;
                 caseIndex < 48;
                 caseIndex++)
            {
                var bytes =
                    new byte[
                        random.Next(
                            minValue: 0,
                            maxValue: 4_097)];

                random.NextBytes(bytes);

                await File.WriteAllBytesAsync(
                    path,
                    bytes);

                try
                {
                    var firstLoad =
                        await _loader
                            .LoadFromFileAsync(
                                path);

                    var firstCompilation =
                        await _compiler
                            .CompileFromFileAsync(
                                path);

                    var secondLoad =
                        await _loader
                            .LoadFromFileAsync(
                                path);

                    var secondCompilation =
                        await _compiler
                            .CompileFromFileAsync(
                                path);

                    AssertLoadResultsEqual(
                        firstLoad,
                        secondLoad);

                    AssertCompilationResultsEqual(
                        firstCompilation,
                        secondCompilation);

                    AssertFailClosed(
                        firstLoad,
                        firstCompilation);
                }
                catch (Exception exception)
                {
                    throw CreateFuzzFailure(
                        seed,
                        caseIndex,
                        "random-file-bytes",
                        exception);
                }
            }
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    private void AssertTextCase(
        string yaml,
        int seed,
        int caseIndex)
    {
        try
        {
            var firstLoad =
                _loader.LoadFromText(
                    yaml);

            var firstCompilation =
                _compiler.CompileFromText(
                    yaml);

            var secondLoad =
                _loader.LoadFromText(
                    yaml);

            var secondCompilation =
                _compiler.CompileFromText(
                    yaml);

            AssertLoadResultsEqual(
                firstLoad,
                secondLoad);

            AssertCompilationResultsEqual(
                firstCompilation,
                secondCompilation);

            AssertFailClosed(
                firstLoad,
                firstCompilation);
        }
        catch (Exception exception)
        {
            throw CreateFuzzFailure(
                seed,
                caseIndex,
                "text",
                exception);
        }
    }

    private static void AssertLoadResultsEqual(
        ManifestLoadResult first,
        ManifestLoadResult second)
    {
        Assert.Equal(
            first.IsSuccess,
            second.IsSuccess);

        Assert.Equal(
            first.Errors
                .Select(
                    static error =>
                        error.Code)
                .ToArray(),
            second.Errors
                .Select(
                    static error =>
                        error.Code)
                .ToArray());
    }

    private static void
        AssertCompilationResultsEqual(
            ManifestCompilationResult first,
            ManifestCompilationResult second)
    {
        Assert.Equal(
            first.IsSuccess,
            second.IsSuccess);

        Assert.Equal(
            first.LoadErrors
                .Select(
                    static error =>
                        error.Code)
                .ToArray(),
            second.LoadErrors
                .Select(
                    static error =>
                        error.Code)
                .ToArray());

        Assert.Equal(
            first.ValidationErrors
                .Select(
                    static error =>
                        error.Code)
                .ToArray(),
            second.ValidationErrors
                .Select(
                    static error =>
                        error.Code)
                .ToArray());

        Assert.Equal(
            first.Policies.Count,
            second.Policies.Count);
    }

    private static void AssertFailClosed(
        ManifestLoadResult load,
        ManifestCompilationResult compilation)
    {
        if (!load.IsSuccess)
        {
            Assert.False(
                compilation.IsSuccess);

            Assert.Empty(
                compilation.Policies);

            Assert.NotEmpty(
                compilation.LoadErrors);

            return;
        }

        if (!compilation.IsSuccess)
        {
            Assert.Empty(
                compilation.Policies);
        }
    }

    private static string CreateRandomText(
        Random random)
    {
        const string yamlCharacters =
            "-:#[]{}&*!?|>%@`,.'\"";

        const string whitespace =
            " \t\r\n";

        var length =
            random.Next(
                minValue: 0,
                maxValue: 2_049);

        var builder =
            new StringBuilder(length);

        for (var index = 0;
             index < length;
             index++)
        {
            var value =
                random.Next(
                    minValue: 0,
                    maxValue: 10);

            switch (value)
            {
                case 0:
                    builder.Append(
                        yamlCharacters[
                            random.Next(
                                yamlCharacters.Length)]);
                    break;

                case 1:
                    builder.Append(
                        whitespace[
                            random.Next(
                                whitespace.Length)]);
                    break;

                case 2:
                    builder.Append(
                        (char)random.Next(
                            0x20,
                            0x7F));
                    break;

                case 3:
                    builder.Append(
                        (char)random.Next(
                            0x80,
                            0xD800));
                    break;

                case 4:
                    builder.Append(
                        (char)random.Next(
                            0xE000,
                            0xFFFE));
                    break;

                case 5:
                    builder.Append(
                        (char)random.Next(
                            0xD800,
                            0xDC00));
                    break;

                case 6:
                    builder.Append(
                        (char)random.Next(
                            0xDC00,
                            0xE000));
                    break;

                case 7:
                    builder.Append('\0');
                    break;

                case 8:
                    builder.Append(
                        random.Next(2) == 0
                            ? "---"
                            : "...");
                    break;

                default:
                    builder.Append(
                        random.Next(2) == 0
                            ? "&anchor"
                            : "*alias");
                    break;
            }
        }

        return builder.ToString();
    }

    private static string CreateStructuredManifest(
        Random random,
        int caseIndex)
    {
        var token =
            $"item-{caseIndex}-{random.Next(1_000_000)}";

        return random.Next(
            minValue: 0,
            maxValue: 10) switch
        {
            0 => CreateValidPermissionManifest(
                token),

            1 => CreateDuplicateIdentifierManifest(
                token),

            2 => CreateDuplicateRouteManifest(
                token),

            3 => CreateUnknownPropertyManifest(
                token),

            4 => CreateMultipleRequirementKindManifest(
                token),

            5 => CreateNestedNotManifest(
                token,
                random.Next(
                    minValue: 1,
                    maxValue: 71)),

            6 => CreateMultipleDocumentManifest(
                token),

            7 => CreateAnchorManifest(
                token),

            8 => CreateTaggedManifest(
                token),

            _ => CreateMalformedManifest(
                token)
        };
    }

    private static string
        CreateValidPermissionManifest(
            string token)
    {
        return $$"""
            schemaVersion: 1
            application:
              id: {{token}}
              name: {{token}}
            policies:
              - id: {{token}}-read
                resourceType: sample
                action: read
                requirement:
                  permission: sample.read
            """;
    }

    private static string
        CreateDuplicateIdentifierManifest(
            string token)
    {
        return $$"""
            schemaVersion: 1
            application:
              id: {{token}}
              name: {{token}}
            policies:
              - id: duplicate
                resourceType: sample
                action: read
                requirement:
                  permission: sample.read
              - id: duplicate
                resourceType: sample
                action: write
                requirement:
                  permission: sample.write
            """;
    }

    private static string
        CreateDuplicateRouteManifest(
            string token)
    {
        return $$"""
            schemaVersion: 1
            application:
              id: {{token}}
              name: {{token}}
            policies:
              - id: first
                resourceType: sample
                action: read
                requirement:
                  permission: sample.read
              - id: second
                resourceType: sample
                action: read
                requirement:
                  role: reader
            """;
    }

    private static string
        CreateUnknownPropertyManifest(
            string token)
    {
        return $$"""
            schemaVersion: 1
            application:
              id: {{token}}
              name: {{token}}
              unexpectedProperty: rejected
            policies: []
            """;
    }

    private static string
        CreateMultipleRequirementKindManifest(
            string token)
    {
        return $$"""
            schemaVersion: 1
            application:
              id: {{token}}
              name: {{token}}
            policies:
              - id: invalid-kind
                resourceType: sample
                action: read
                requirement:
                  permission: sample.read
                  role: reader
            """;
    }

    private static string CreateNestedNotManifest(
        string token,
        int depth)
    {
        var builder =
            new StringBuilder();

        builder.AppendLine(
            "schemaVersion: 1");

        builder.AppendLine(
            "application:");

        builder.AppendLine(
            $"  id: {token}");

        builder.AppendLine(
            $"  name: {token}");

        builder.AppendLine(
            "policies:");

        builder.AppendLine(
            "  - id: nested");

        builder.AppendLine(
            "    resourceType: sample");

        builder.AppendLine(
            "    action: read");

        builder.AppendLine(
            "    requirement:");

        var indentation =
            "      ";

        for (var index = 0;
             index < depth;
             index++)
        {
            builder.Append(
                indentation);

            builder.AppendLine(
                "not:");

            indentation +=
                "  ";
        }

        builder.Append(
            indentation);

        builder.AppendLine(
            "permission: sample.read");

        return builder.ToString();
    }

    private static string
        CreateMultipleDocumentManifest(
            string token)
    {
        return
            CreateValidPermissionManifest(
                token) +
            "\n---\n" +
            CreateValidPermissionManifest(
                token + "-second");
    }

    private static string CreateAnchorManifest(
        string token)
    {
        return $$"""
            schemaVersion: 1
            application:
              id: {{token}}
              name: {{token}}
            policies:
              - id: anchored
                resourceType: sample
                action: read
                requirement: &shared
                  permission: sample.read
            """;
    }

    private static string CreateTaggedManifest(
        string token)
    {
        return $$"""
            --- !rulegate
            schemaVersion: 1
            application:
              id: {{token}}
              name: {{token}}
            policies: []
            """;
    }

    private static string CreateMalformedManifest(
        string token)
    {
        return $$"""
            schemaVersion: 1
            application:
              id: {{token}}
               name: malformed-indentation
            policies:
              - [
            """;
    }

    private static XunitException CreateFuzzFailure(
        int seed,
        int caseIndex,
        string category,
        Exception exception)
    {
        return new XunitException(
            $"Deterministic fuzz failure. " +
            $"Seed={seed}; Case={caseIndex}; " +
            $"Category={category}; " +
            $"Exception={exception}");
    }
}
