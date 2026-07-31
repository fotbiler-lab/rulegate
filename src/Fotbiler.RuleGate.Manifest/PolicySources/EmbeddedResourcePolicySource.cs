using System.Reflection;
using Fotbiler.RuleGate.Abstractions.Policies;
using Fotbiler.RuleGate.Manifest.Compilation;

namespace Fotbiler.RuleGate.Manifest.PolicySources;

public sealed class EmbeddedResourcePolicySource : IPolicySource
{
    private readonly Assembly _assembly;
    private readonly string _resourceName;
    private readonly RuleGateManifestCompiler _compiler;

    public EmbeddedResourcePolicySource(
        Assembly assembly,
        string resourceName,
        string? name = null)
        : this(
            assembly,
            resourceName,
            name,
            new RuleGateManifestCompiler())
    {
    }

    public EmbeddedResourcePolicySource(
        Assembly assembly,
        string resourceName,
        string? name,
        RuleGateManifestCompiler compiler)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceName);
        ArgumentNullException.ThrowIfNull(compiler);

        _assembly = assembly;
        _resourceName = resourceName;
        _compiler = compiler;

        Name = name ??
            $"embedded:{assembly.GetName().Name}:{resourceName}";

        ArgumentException.ThrowIfNullOrWhiteSpace(Name);
    }

    public string Name { get; }

    public async ValueTask<PolicySourceLoadResult> LoadAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Stream? stream;

        try
        {
            stream = _assembly.GetManifestResourceStream(
                _resourceName);
        }
        catch (Exception)
        {
            return ReadFailure();
        }

        if (stream is null)
        {
            return PolicySourceLoadResult.Failure(
            [
                new PolicySourceDiagnostic(
                    ManifestPolicySourceCodes
                        .EmbeddedResourceNotFound,
                    $"Embedded policy resource '{_resourceName}' was not found.")
            ]);
        }

        try
        {
            await using (stream)
            using (var reader = new StreamReader(stream))
            {
                var yaml = await reader.ReadToEndAsync(
                    cancellationToken);

                return _compiler
                    .CompileFromText(yaml)
                    .ToPolicySourceLoadResult();
            }
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return ReadFailure();
        }
    }

    private static PolicySourceLoadResult ReadFailure()
    {
        return PolicySourceLoadResult.Failure(
        [
            new PolicySourceDiagnostic(
                ManifestPolicySourceCodes
                    .EmbeddedResourceReadFailed,
                "The embedded policy resource could not be read.")
        ]);
    }
}
