namespace Fotbiler.RuleGate.Abstractions.Diagnostics;

public enum AuthorizationRequirementKind
{
    Custom = 0,
    Permission = 1,
    Role = 2,
    Attribute = 3,
    All = 4,
    Any = 5,
    Not = 6,
    AttributeComparison = 7
}
