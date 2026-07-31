using Fotbiler.RuleGate.Abstractions.Policies;
using Fotbiler.RuleGate.Manifest.Compilation;
using Fotbiler.RuleGate.Manifest.Models;
using Fotbiler.RuleGate.Manifest.PolicySources;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace Fotbiler.RuleGate.AspNetCore.PolicySources;

public sealed class ConfigurationPolicySource : IPolicySource
{
    private readonly IConfigurationSection _section;
    private readonly RuleGateManifestCompiler _compiler;

    public ConfigurationPolicySource(
        IConfiguration configuration,
        string sectionPath,
        ConfigurationPolicySourceOptions? options = null,
        string? name = null)
        : this(
            configuration,
            sectionPath,
            options,
            name,
            new RuleGateManifestCompiler())
    {
    }

    public ConfigurationPolicySource(
        IConfiguration configuration,
        string sectionPath,
        ConfigurationPolicySourceOptions? options,
        string? name,
        RuleGateManifestCompiler compiler)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionPath);
        ArgumentNullException.ThrowIfNull(compiler);

        _section = configuration.GetSection(sectionPath);
        _compiler = compiler;

        SectionPath = sectionPath;
        ReloadOnChange = options?.ReloadOnChange ?? false;
        Name = name ?? $"configuration:{sectionPath}";

        ArgumentException.ThrowIfNullOrWhiteSpace(Name);
    }

    public string Name { get; }

    public string SectionPath { get; }

    public bool ReloadOnChange { get; }

    public ValueTask<PolicySourceLoadResult> LoadAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_section.Exists())
        {
            return ValueTask.FromResult(
                PolicySourceLoadResult.Failure(
                [
                    new PolicySourceDiagnostic(
                        ConfigurationPolicySourceCodes
                            .SectionNotFound,
                        $"Configuration section '{SectionPath}' was not found.",
                        SectionPath)
                ]));
        }

        RuleGateManifest? manifest;

        try
        {
            manifest = _section.Get<RuleGateManifest>(
                options =>
                    options.ErrorOnUnknownConfiguration =
                        true);
        }
        catch (Exception)
        {
            return ValueTask.FromResult(
                BindingFailure());
        }

        if (manifest is null)
        {
            return ValueTask.FromResult(
                BindingFailure());
        }

        var result = _compiler
            .CompileFromManifest(manifest)
            .ToPolicySourceLoadResult();

        return ValueTask.FromResult(result);
    }

    internal IChangeToken GetReloadToken()
    {
        return _section.GetReloadToken();
    }

    private PolicySourceLoadResult BindingFailure()
    {
        return PolicySourceLoadResult.Failure(
        [
            new PolicySourceDiagnostic(
                ConfigurationPolicySourceCodes.BindingFailed,
                $"Configuration section '{SectionPath}' could not be bound to a RuleGate manifest.",
                SectionPath)
        ]);
    }
}
