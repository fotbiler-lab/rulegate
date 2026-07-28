using Fotbiler.RuleGate.Manifest.Compilation;

namespace Fotbiler.RuleGate.Cli.Generation.CSharp;

internal sealed record ManifestCSharpGenerationResult(
    string ManifestPath,
    ManifestCompilationResult Compilation,
    CSharpGenerationResult? Generation)
{
    public bool IsSuccess =>
        Compilation.IsSuccess
        && Generation?.IsSuccess == true;

    public string? Source =>
        Generation?.Source;
}
