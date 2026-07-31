using Fotbiler.RuleGate.Abstractions.Diagnostics;

namespace Fotbiler.RuleGate.Cli.Explanation;

internal sealed class CapturingAuthorizationDiagnosticsSink :
    IAuthorizationDiagnosticsSink
{
    public bool IsEnabled => true;

    public AuthorizationEvaluationDiagnostic? Diagnostic
    {
        get;
        private set;
    }

    public ValueTask WriteAsync(
        AuthorizationEvaluationDiagnostic diagnostic,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(diagnostic);
        cancellationToken.ThrowIfCancellationRequested();

        Diagnostic = diagnostic;

        return ValueTask.CompletedTask;
    }
}
