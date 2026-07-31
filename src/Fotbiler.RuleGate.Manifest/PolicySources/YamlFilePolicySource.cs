using Fotbiler.RuleGate.Abstractions.Policies;
using Fotbiler.RuleGate.Manifest.Compilation;

namespace Fotbiler.RuleGate.Manifest.PolicySources;

public sealed class YamlFilePolicySource : IPolicySource
{
    private readonly RuleGateManifestCompiler _compiler;

    public YamlFilePolicySource(
        string path,
        YamlPolicyFileOptions? options = null,
        string? name = null)
        : this(
            path,
            options,
            name,
            new RuleGateManifestCompiler())
    {
    }

    public YamlFilePolicySource(
        string path,
        YamlPolicyFileOptions? options,
        string? name,
        RuleGateManifestCompiler compiler)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(compiler);

        FullPath = Path.GetFullPath(path);
        Name = name ?? $"yaml-file:{FullPath}";
        ArgumentException.ThrowIfNullOrWhiteSpace(Name);

        ReloadOnChange = options?.ReloadOnChange ?? false;
        _compiler = compiler;
    }

    public string Name { get; }

    public string FullPath { get; }

    public bool ReloadOnChange { get; }

    public async ValueTask<PolicySourceLoadResult> LoadAsync(
        CancellationToken cancellationToken = default)
    {
        var compilation =
            await _compiler.CompileFromFileAsync(
                FullPath,
                cancellationToken);

        return compilation.ToPolicySourceLoadResult();
    }
}
