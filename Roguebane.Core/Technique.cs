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
// Which side a technique targets (§8 [LOCKED]): ENEMY techniques use the foe part-aim; SELF techniques
// act on the caster's own body (auto-pick, e.g. most-damaged part) and can never be aimed at the foe.
public enum TargetSide { Enemy, Self }

public sealed record Technique(
    string Id,
    Stat Stat,
    int Reserve,
    TechniqueKind Kind,
    int Cooldown,
    int Power,
    double DamageMult = 1.0, // weapon-consulting only: multiplies the SUM of consulted weapon power
                              // (the "verb" scaling — Jab .5x, Cleave 1.5x...). Inert when Consults is None.
    int ChargeCost = 0,
    WeaponUse Consults = WeaponUse.None,
    Stat? AltStat = null, // stat-flexible verbs (Frenzy/Flurry): consults EITHER Stat or AltStat's
                          // wielded weapon (TECHNIQUES.md/CORE_RUNES.md LOCKED 2026-07-05).
    bool Heals = false, // a REPAIR technique: on discharge it mends the caster's own most-damaged part
                        // (by Power) instead of striking a target (the §10 part-heal). No target needed.
    bool Lifesteal = false, // Siphon (TECHNIQUES.md): on a CLEAN landed part-hit (never a shield-absorbed
                            // hit, never an already-broken part -- the shared on-hit-boon gate), repairs
                            // the caster's own most-damaged part by the damage just dealt.
    bool ConsumesMinion = false, // Sacrifice (TECHNIQUES.md, LOCKED 2026-07-05): a Heals technique that,
                                 // on each discharge, consumes ONE fielded minion (freeing its reservation,
                                 // no Summons refund) instead of spending Power -- heal scales with the
                                 // consumed minion's Reserve (its tier proxy). Holds fire with no minion
                                 // fielded, same as Heals holds fire with no wound. Inert without Heals.
    int ShieldLayers = 0, // >0 marks a SHIELD SOURCE (§6b): a passive that maintains this many 1-dmg
    int ShieldRegen = 0,  // layers on the body, one regenerating every ShieldRegen ticks (CON-scaled).
    bool ShieldPiercing = false, // ignores the shield pool; costs Charge per use (§6b Charge = pierce).
    string Desc = "", // DISPLAY-ONLY card copy (design/01); {power} resolves from the data at render
                      // so the text can never contradict a tuning pass.
    TargetSide Side = TargetSide.Enemy) // §8 target side; heals declare Self
{
    // A shield source is ALWAYS PASSIVE (§6b [LOCKED]): it reserves + holds, never targets or fires —
    // derived from the data so a source can't be authored active by mistake.
    public bool IsPassive => ShieldLayers > 0;

    public string DescText => Desc.Replace("{power}", Power.ToString());
}
