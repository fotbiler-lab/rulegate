namespace Fotbiler.RuleGate.Abstractions.Policies;

public enum AuthorizationAttributeOperandKind
{
    Literal = 0,
    Subject = 1,
    Resource = 2,
    Context = 3
}
