namespace Roguebane.Core;

public enum TechniqueKind
{
    Timered,   // reserves its stat while active, bursts every Cooldown ticks
    Sustained, // reserves its stat while active, outputs every tick
}

// A technique is content, not code: it reserves one stat (STR swing, INT spell, CON block...)
// while engaged, and one tick loop interprets kind/cadence/power. Reserve doubles as the stat
// requirement — lose enough of the stat and the body sheds it (e.g. a smashed head silences spells).
// CHARGE (§6b/§10) is the SHIELD-PIERCE resource: ONLY a ShieldPiercing technique draws it — each
// discharge bypasses the defender's shield pool and spends ChargeCost (>=1) of charge; dry => it HOLDS
// the pierce but keeps its reservation. ChargeCost on a non-piercing technique is inert (do not author).
public sealed record Technique(
    string Id,
    Stat Stat,
    int Reserve,
    TechniqueKind Kind,
    int Cooldown,
    int Power,
    int ChargeCost = 0,
    WeaponUse Consults = WeaponUse.None,
    bool Heals = false, // a REPAIR technique: on discharge it mends the caster's own most-damaged part
                        // (by Power) instead of striking a target (the §10 part-heal). No target needed.
    int ShieldLayers = 0, // >0 marks a SHIELD SOURCE (§6b): a passive that maintains this many 1-dmg
    int ShieldRegen = 0,  // layers on the body, one regenerating every ShieldRegen ticks (CON-scaled).
    bool ShieldPiercing = false); // ignores the shield pool; costs Charge per use (§6b Charge = pierce).
