namespace Fotbiler.RuleGate.Cli.Generation.CSharp;

internal sealed record CSharpGenerationInput(
    string NamespaceName,
    IReadOnlyCollection<string> PolicyIds,
    IReadOnlyCollection<string> ResourceTypes,
    IReadOnlyCollection<string> Actions);
