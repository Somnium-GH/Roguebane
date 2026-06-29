namespace Roguebane.Core;

public enum TechniqueKind
{
    Timered,   // reserves its stat while active, bursts every Cooldown ticks
    Sustained, // reserves its stat while active, outputs every tick
}

// A technique is content, not code: it reserves one stat (STR swing, INT spell, CON block...)
// while engaged, and one tick loop interprets kind/cadence/power. Reserve doubles as the stat
// requirement — lose enough of the stat and the body sheds it (e.g. a smashed head silences spells).
// ChargeCost > 0 marks a magic-tier technique: beyond reserving its stat, each discharge also draws
// from the finite charge resource (distinct from the attribute pool; user-facing name deferred).
// When charge is dry the technique holds fire but keeps its reservation.
public sealed record Technique(
    string Id,
    Stat Stat,
    int Reserve,
    TechniqueKind Kind,
    int Cooldown,
    int Power,
    int ChargeCost = 0);
