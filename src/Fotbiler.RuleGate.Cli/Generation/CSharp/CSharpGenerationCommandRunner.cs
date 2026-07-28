using System.Text;

using Fotbiler.RuleGate.Cli.ExitCodes;
using Fotbiler.RuleGate.Cli.Output;
using Fotbiler.RuleGate.Cli.Validation;

namespace Fotbiler.RuleGate.Cli.Generation.CSharp;

internal sealed class CSharpGenerationCommandRunner
{
    private readonly ManifestCSharpGenerationRunner _runner;

    public CSharpGenerationCommandRunner(
        ManifestCSharpGenerationRunner runner)
    {
        ArgumentNullException.ThrowIfNull(
            runner);

        _runner = runner;
    }

    public async Task<int> RunAsync(
        string? manifestPath,
        string namespaceName,
        string? outputPath,
        bool check,
        TextWriter output,
        TextWriter error,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            namespaceName);

        ArgumentNullException.ThrowIfNull(
            output);

        ArgumentNullException.ThrowIfNull(
            error);

        if (check
            && string.IsNullOrWhiteSpace(
                outputPath))
        {
            error.WriteLine(
                "error: --check requires --output.");

            return RuleGateExitCodes.UsageError;
        }

        var result =
            await _runner.GenerateAsync(
                manifestPath,
                namespaceName,
                cancellationToken);

        if (!result.Compilation.IsSuccess)
        {
            var report =
                ManifestValidationReport.Create(
                    result.ManifestPath,
                    result.Compilation);

            new TextValidationReporter()
                .Write(
                    report,
                    output,
                    error);

            return RuleGateExitCodes.ManifestInvalid;
        }

        if (result.Generation?.IsSuccess != true)
        {
            WriteGenerationDiagnostics(
                result,
                error);

            return RuleGateExitCodes.ManifestInvalid;
        }

        string source =
            result.Source
            ?? throw new InvalidOperationException(
                "Successful C# generation did not produce source.");

        if (check)
        {
            return await CheckOutputAsync(
                result,
                source,
                outputPath
                ?? throw new InvalidOperationException(
                    "Check mode requires an output path."),
                output,
                error,
                cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(
                outputPath))
        {
            await output.WriteAsync(
                source);

            return RuleGateExitCodes.Success;
        }

        string fullOutputPath =
            Path.GetFullPath(
                outputPath);

        await WriteAtomicallyAsync(
            fullOutputPath,
            source,
            cancellationToken);

        WriteOutputSummary(
            "RuleGate C# source generated.",
            result,
            fullOutputPath,
            output);

        return RuleGateExitCodes.Success;
    }

    private static async Task<int> CheckOutputAsync(
        ManifestCSharpGenerationResult result,
        string source,
        string outputPath,
        TextWriter output,
        TextWriter error,
        CancellationToken cancellationToken)
    {
        string fullOutputPath =
            Path.GetFullPath(
                outputPath);

        if (!File.Exists(
                fullOutputPath))
        {
            WriteCheckFailure(
                "RuleGate C# output is missing.",
                result,
                fullOutputPath,
                error);

            return RuleGateExitCodes.ManifestInvalid;
        }

        byte[] actualBytes =
            await File.ReadAllBytesAsync(
                fullOutputPath,
                cancellationToken);

        byte[] expectedBytes =
            new UTF8Encoding(
                encoderShouldEmitUTF8Identifier: false)
                .GetBytes(
                    source);

        if (!actualBytes
                .AsSpan()
                .SequenceEqual(
                    expectedBytes))
        {
            WriteCheckFailure(
                "RuleGate C# output is stale.",
                result,
                fullOutputPath,
                error);

            return RuleGateExitCodes.ManifestInvalid;
        }

        WriteOutputSummary(
            "RuleGate C# output is current.",
            result,
            fullOutputPath,
            output);

        return RuleGateExitCodes.Success;
    }

    private static void WriteOutputSummary(
        string heading,
        ManifestCSharpGenerationResult result,
        string fullOutputPath,
        TextWriter output)
    {
        output.WriteLine(
            heading);

        output.WriteLine();

        output.WriteLine(
            $"Manifest: {result.ManifestPath}");

        output.WriteLine(
            $"Output: {fullOutputPath}");

        output.WriteLine(
            $"Policies: {result.Compilation.Policies.Count}");
    }

    private static void WriteCheckFailure(
        string heading,
        ManifestCSharpGenerationResult result,
        string fullOutputPath,
        TextWriter error)
    {
        error.WriteLine(
            heading);

        error.WriteLine();

        error.WriteLine(
            $"Manifest: {result.ManifestPath}");

        error.WriteLine(
            $"Output: {fullOutputPath}");

        error.WriteLine();

        error.WriteLine(
            "Run the command without --check to regenerate the file.");
    }

    private static void WriteGenerationDiagnostics(
        ManifestCSharpGenerationResult result,
        TextWriter error)
    {
        error.WriteLine(
            "RuleGate C# generation failed.");

        error.WriteLine();

        error.WriteLine(
            $"File: {result.ManifestPath}");

        IReadOnlyList<CSharpGenerationDiagnostic> diagnostics =
            result.Generation?.Diagnostics
            ?? Array.Empty<CSharpGenerationDiagnostic>();

        foreach (var diagnostic in diagnostics)
        {
            error.WriteLine();

            error.WriteLine(
                $"GENERATION {diagnostic.Code}");

            error.WriteLine(
                $"  {diagnostic.Message}");
        }

        error.WriteLine();

        error.WriteLine(
            $"{diagnostics.Count} error(s) found.");
    }

    private static async Task WriteAtomicallyAsync(
        string outputPath,
        string source,
        CancellationToken cancellationToken)
    {
        string? directory =
            Path.GetDirectoryName(
                outputPath);

        if (directory is null)
        {
            throw new InvalidOperationException(
                "The generated output path has no parent directory.");
        }

        Directory.CreateDirectory(
            directory);

        string temporaryPath =
            Path.Combine(
                directory,
                $".{Path.GetFileName(outputPath)}." +
                $"{Guid.NewGuid():N}.tmp");

        try
        {
            await File.WriteAllTextAsync(
                temporaryPath,
                source,
                new UTF8Encoding(
                    encoderShouldEmitUTF8Identifier: false),
                cancellationToken);

            File.Move(
                temporaryPath,
                outputPath,
                overwrite: true);
        }
        finally
        {
            if (File.Exists(
                    temporaryPath))
            {
                File.Delete(
                    temporaryPath);
            }
        }
    }
}
