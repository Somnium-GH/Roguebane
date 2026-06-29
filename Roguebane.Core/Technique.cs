namespace Roguebane.Core;

public enum TechniqueKind
{
    Timered,   // reserves allocation while active, bursts every Cooldown ticks
    Sustained, // reserves allocation while active, outputs every tick
}

// A technique is content, not code: kind, allocation cost, cadence, and power are data.
// One tick loop interprets all of them.
public sealed record Technique(
    string Id,
    TechniqueKind Kind,
    IReadOnlyDictionary<Attribute, int> Cost,
    int Cooldown,
    int Power);
