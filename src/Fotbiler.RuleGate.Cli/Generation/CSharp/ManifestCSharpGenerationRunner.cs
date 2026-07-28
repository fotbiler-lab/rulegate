using Fotbiler.RuleGate.Manifest.Compilation;
using Fotbiler.RuleGate.Manifest.Configuration;

namespace Fotbiler.RuleGate.Cli.Generation.CSharp;

internal sealed class ManifestCSharpGenerationRunner
{
    private readonly RuleGateManifestCompiler _compiler;
    private readonly CSharpCodeGenerator _generator;

    public ManifestCSharpGenerationRunner(
        RuleGateManifestCompiler compiler,
        CSharpCodeGenerator generator)
    {
        ArgumentNullException.ThrowIfNull(
            compiler);

        ArgumentNullException.ThrowIfNull(
            generator);

        _compiler = compiler;
        _generator = generator;
    }

    public async Task<ManifestCSharpGenerationResult>
        GenerateAsync(
            string? path,
            string namespaceName,
            CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            namespaceName);

        var requestedPath =
            string.IsNullOrWhiteSpace(path)
                ? RuleGateManifestDefaults.FileName
                : path;

        var fullPath =
            Path.GetFullPath(
                requestedPath);

        var compilation =
            await _compiler.CompileFromFileAsync(
                fullPath,
                cancellationToken);

        if (!compilation.IsSuccess)
        {
            return new ManifestCSharpGenerationResult(
                fullPath,
                compilation,
                null);
        }

        var input =
            new CSharpGenerationInput(
                namespaceName,
                compilation.Policies
                    .Select(
                        static policy => policy.Id)
                    .ToArray(),
                compilation.Policies
                    .Select(
                        static policy => policy.ResourceType)
                    .ToArray(),
                compilation.Policies
                    .Select(
                        static policy => policy.Action)
                    .ToArray());

        var generation =
            _generator.Generate(
                input);

        return new ManifestCSharpGenerationResult(
            fullPath,
            compilation,
            generation);
    }
}
