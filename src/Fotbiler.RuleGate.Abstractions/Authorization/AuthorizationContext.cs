using Fotbiler.RuleGate.Abstractions.Attributes;

namespace Fotbiler.RuleGate.Abstractions.Authorization;

public sealed class AuthorizationContext
{
    public AuthorizationContext(
        DateTimeOffset evaluationTime,
        AuthorizationAttributes? attributes = null)
    {
        EvaluationTime = evaluationTime;

        Attributes = attributes
            ?? AuthorizationAttributes.Empty;
    }

    public DateTimeOffset EvaluationTime { get; }

    public AuthorizationAttributes Attributes { get; }
}
