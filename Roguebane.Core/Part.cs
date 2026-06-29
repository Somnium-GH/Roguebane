namespace Roguebane.Core;

public enum PartRole
{
    Generic,
    Head, // the casting organ — silence it and the body cannot cast
}

// A part is content, not code: identity, attribute demand, role, and durability are data.
// Runtime enabled/health state lives on the Entity, not here. MaxHealth 0 = indestructible.
public sealed record Part(
    string Id,
    IReadOnlyDictionary<Attribute, int> Demand,
    PartRole Role = PartRole.Generic,
    int MaxHealth = 0);
