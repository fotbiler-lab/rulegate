namespace Fotbiler.RuleGate.Abstractions.Policies;

public enum AuthorizationAttributeOperator
{
    Equal = 0,
    NotEqual = 1,
    GreaterThan = 2,
    GreaterThanOrEqual = 3,
    LessThan = 4,
    LessThanOrEqual = 5,
    Contains = 6,
    StartsWith = 7,
    EndsWith = 8,
    ContainsAny = 9,
    ContainsAll = 10,
    In = 11,
    NotIn = 12,
    Intersects = 13,
    IsEmpty = 14,
    IsNotEmpty = 15,
    Exists = 16,
    NotExists = 17,
    IsNull = 18,
    IsNotNull = 19
}
