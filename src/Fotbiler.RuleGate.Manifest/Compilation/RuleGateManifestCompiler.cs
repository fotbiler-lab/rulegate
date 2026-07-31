using Fotbiler.RuleGate.Manifest.Loading;
using Fotbiler.RuleGate.Manifest.Mapping;
using Fotbiler.RuleGate.Manifest.Models;
using Fotbiler.RuleGate.Manifest.Validation;

namespace Fotbiler.RuleGate.Manifest.Compilation;

public sealed class RuleGateManifestCompiler
{
    private readonly RuleGateManifestYamlLoader _loader;
    private readonly RuleGateManifestMapper _mapper;

    public RuleGateManifestCompiler()
        : this(
            new RuleGateManifestYamlLoader(),
            new RuleGateManifestMapper(
                new RuleGateManifestValidator()))
    {
    }

    public RuleGateManifestCompiler(
        RuleGateManifestYamlLoader loader,
        RuleGateManifestMapper mapper)
    {
        ArgumentNullException.ThrowIfNull(loader);
        ArgumentNullException.ThrowIfNull(mapper);

        _loader = loader;
        _mapper = mapper;
    }

    public ManifestCompilationResult CompileFromText(
        string yaml)
    {
        var loadResult =
            _loader.LoadFromText(yaml);

        return Compile(loadResult);
    }

    public ManifestCompilationResult CompileFromManifest(
        RuleGateManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        return Compile(
            ManifestLoadResult.Success(manifest));
    }

    public async ValueTask<ManifestCompilationResult>
        CompileFromFileAsync(
            string path,
            CancellationToken cancellationToken = default)
    {
        var loadResult =
            await _loader.LoadFromFileAsync(
                path,
                cancellationToken);

        return Compile(loadResult);
    }

    private ManifestCompilationResult Compile(
        ManifestLoadResult loadResult)
    {
        if (!loadResult.IsSuccess)
        {
            return ManifestCompilationResult.LoadFailure(
                loadResult.Errors);
        }

        var mappingResult =
            _mapper.Map(loadResult.Manifest!);

        if (!mappingResult.IsSuccess)
        {
            return ManifestCompilationResult
                .ValidationFailure(
                    mappingResult.Errors);
        }

        return ManifestCompilationResult.Success(
            mappingResult.Policies);
    }
}
