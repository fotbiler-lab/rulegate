namespace Fotbiler.RuleGate.Abstractions.Attributes;

public enum AuthorizationAttributeValueKind
{
    Null = 0,
    String = 1,
    Boolean = 2,
    Number = 3,
    DateTimeOffset = 4,
    Collection = 5
}
