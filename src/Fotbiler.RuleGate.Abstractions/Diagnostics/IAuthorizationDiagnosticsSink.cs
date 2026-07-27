namespace Fotbiler.RuleGate.Abstractions.Diagnostics;

public interface IAuthorizationDiagnosticsSink
{
    bool IsEnabled { get; }

    ValueTask WriteAsync(
        AuthorizationEvaluationDiagnostic diagnostic,
        CancellationToken cancellationToken = default);
}
