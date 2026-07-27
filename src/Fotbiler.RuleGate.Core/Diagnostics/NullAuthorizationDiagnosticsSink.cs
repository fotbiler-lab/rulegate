using Fotbiler.RuleGate.Abstractions.Diagnostics;

namespace Fotbiler.RuleGate.Core.Diagnostics;

internal sealed class NullAuthorizationDiagnosticsSink
    : IAuthorizationDiagnosticsSink
{
    internal static NullAuthorizationDiagnosticsSink
        Instance
    {
        get;
    } = new();

    private NullAuthorizationDiagnosticsSink()
    {
    }

    public bool IsEnabled => false;

    public ValueTask WriteAsync(
        AuthorizationEvaluationDiagnostic diagnostic,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(diagnostic);

        cancellationToken.ThrowIfCancellationRequested();

        return ValueTask.CompletedTask;
    }
}
