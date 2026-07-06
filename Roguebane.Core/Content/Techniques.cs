namespace Roguebane.Core.Content;

// Techniques on the four-stat model, all interpreted by the one tick loop. Cooldowns are in COMBAT
// TICKS at the fixed 10 ticks/sec clock (value/10 = seconds); base speed 8.0s (80 ticks) anchors a
// x1.0 verb (TECHNIQUES.md). Weapon-verbs (Jab/Cleave/Lunge) consult the wielded weapon for power
// (DamageMult scales the SUM of consulted weapon power; Power stays 0) same as Armory's Swing/Frenzy/
// Flurry/Shot. Spells (Ember/Siphon) are weapon-independent: Power is their innate base. Shield
// sources and heals are Sustained/Timered passives on their own stat. Tier ladders are PARKED
// (TECHNIQUES.md Open/TBD) -- every entry below is T1 only.
public static class Techniques
{
    public static readonly Technique Jab =
        new("jab", Stat.Str, Reserve: 1, TechniqueKind.Timered, Cooldown: 40, Power: 0, DamageMult: 0.5,
            Consults: WeaponUse.Primary,
            Desc: "A quick strike with your wielded weapon, for half its power.");

    public static readonly Technique Cleave =
        new("cleave", Stat.Str, Reserve: 2, TechniqueKind.Timered, Cooldown: 120, Power: 0, DamageMult: 1.5,
            Consults: WeaponUse.Primary,
            Desc: "A heavy swing with your wielded weapon, for half again its power.");

    public static readonly Technique Lunge =
        new("lunge", Stat.Dex, Reserve: 1, TechniqueKind.Timered, Cooldown: 48, Power: 0, DamageMult: 0.75,
            Consults: WeaponUse.Primary,
            Desc: "A darting stab with your wielded weapon, for three-quarters its power.");

    public static readonly Technique Ember =
        new("ember", Stat.Int, Reserve: 1, TechniqueKind.Timered, Cooldown: 30, Power: 1,  // ~3s bolt
            Desc: "A fast fire bolt for {power} damage; a targeted hit feeds Resonance.");

    // The lifesteal spell (TECHNIQUES.md): on a CLEAN landed part-hit (never shield-absorbed, never an
    // already-broken part -- Caster.Hit's shared on-hit-boon gate) it mends the caster's own
    // most-damaged part by the damage just dealt.
    public static readonly Technique Siphon =
        new("siphon", Stat.Int, Reserve: 2, TechniqueKind.Timered, Cooldown: 60, Power: 2, Lifesteal: true,
            Desc: "A draining bolt for {power} that heals you by the same, replenishing your own attribute damage. No lifesteal on a shield-absorbed hit or a broken part; a targeted hit feeds Resonance.");

    // The CON shield source: a held passive that reserves CON and maintains a regenerating pool of
    // shield layers -- the §6b mitigation that stands between a hit and the body (§8: shields + full
    // evade are the only mitigations). T1 rung of the CON guard ladder (Steel is T2).
    public static readonly Technique Brace =
        new("brace", Stat.Con, Reserve: 2, TechniqueKind.Sustained, Cooldown: 0, Power: 0,
            ShieldLayers: 4, ShieldRegen: 20, // +1 pip / 2.0s
            Desc: "Hold a pool of 4 CON shield points, each absorbing one hit, +1 pip / 2.0s. Requires a shield equipped.");

    // T2 rung of the CON guard ladder (Warden signature). Tier ladders are parked elsewhere
    // (TECHNIQUES.md Open/TBD) but Steel is itself a distinct T2 entry, not a scaled Brace.
    public static readonly Technique Steel =
        new("steel", Stat.Con, Reserve: 3, TechniqueKind.Sustained, Cooldown: 0, Power: 0,
            ShieldLayers: 8, ShieldRegen: 15, // +1 pip / 1.5s
            Desc: "A stronger held guard: pool 8, +1 pip / 1.5s.");

    // T1 rung of the INT ward ladder (barkskin -> stoneskin -> steelskin -> diamondskin,
    // TECHNIQUES.md; higher rungs are parked, T1 only).
    public static readonly Technique Barkskin =
        new("barkskin", Stat.Int, Reserve: 1, TechniqueKind.Sustained, Cooldown: 0, Power: 0,
            ShieldLayers: 3, ShieldRegen: 30, // +1 pip / 3.0s
            Desc: "A held spell keeping 3 shield points, +1 pip / 3.0s.");

    // T2 rung of the INT ward ladder (barkskin -> stoneskin -> steelskin -> diamondskin,
    // TECHNIQUES.md), numbers locked by Doug 2026-07-05 (CHUNK A item 5): pool 6, +2 pips/3.0s.
    public static readonly Technique Stoneskin =
        new("stoneskin", Stat.Int, Reserve: 2, TechniqueKind.Sustained, Cooldown: 0, Power: 0,
            ShieldLayers: 6, ShieldRegen: 15, // +2 pips / 3.0s
            Desc: "A stronger held ward: pool 6, +2 pips / 3.0s.");

    // T1 STR guard (TECHNIQUES.md).
    public static readonly Technique Bind =
        new("bind", Stat.Str, Reserve: 2, TechniqueKind.Sustained, Cooldown: 0, Power: 0,
            ShieldLayers: 2, ShieldRegen: 25, // +1 pip / 2.5s
            Desc: "A held guard keeping 2 shield points, +1 pip / 2.5s.");

    // T1 DEX guard (TECHNIQUES.md).
    public static readonly Technique Parry =
        new("parry", Stat.Dex, Reserve: 2, TechniqueKind.Sustained, Cooldown: 0, Power: 0,
            ShieldLayers: 1, ShieldRegen: 20, // +1 pip / 2.0s
            Desc: "A held guard keeping 1 shield point, +1 pip / 2.0s.");

    // The §10 in-combat part-heal (CON): mends the most-damaged part ~1 every 8s, reserving CON while
    // held. Kept OUT of `All` for now so it stays opt-in content (no balance shift to the default
    // palette); it is the reconcile trigger for live foe part-aim (G1, staged off until heals exist).
    public static readonly Technique Bandage =
        new("bandage", Stat.Con, Reserve: 2, TechniqueKind.Timered, Cooldown: 80, Power: 1, Heals: true,
            Desc: "Mends your most-damaged part {power} / 8.0s. The flat baseline.", Side: TargetSide.Self);

    // T2 rung of the CON heal ladder (Warden signature, TECHNIQUES.md).
    public static readonly Technique Suture =
        new("suture", Stat.Con, Reserve: 3, TechniqueKind.Timered, Cooldown: 80, Power: 2, Heals: true,
            Desc: "Mends your most-damaged part {power} / 8.0s. (Warden signature.)", Side: TargetSide.Self);

    // Sacrifice (TECHNIQUES.md, LOCKED 2026-07-05): consumes 1 fielded minion per discharge to mend the
    // most-damaged part -- Reserve 0/Consults None by design (it costs a MINION, not a stat). Heal =
    // 4 x the consumed minion's Reserve (its tier proxy: Skeleton/Hound T1 -> 4, Iron Golem T2 -> 8).
    // Heal numbers APPROVED as the standing placeholder (Doug, 2026-07-05 - "placeholder for now",
    // RULES_SNAPSHOT.md) -- not final-locked, but no longer blocking. Pinned by SacrificeHealTests.
    public static readonly Technique Sacrifice =
        new("sacrifice", Stat.Con, Reserve: 0, TechniqueKind.Timered, Cooldown: 80, Power: 0,
            Heals: true, ConsumesMinion: true,
            Desc: "Consume one of your fielded minions to mend your most-damaged part; the heal scales with the minion's tier.",
            Side: TargetSide.Self);

    // Bandage is in the palette + the starting kits: every build fights with a part-heal so it can
    // survive live foe part-aim on skirmishes; a build that drops it pays the intended penalty. Steel/
    // Barkskin/Bind/Parry/Suture/Sacrifice stay opt-in (higher-tier content) until a kit picks them.
    public static readonly IReadOnlyList<Technique> All =
        new[] { Jab, Cleave, Lunge, Ember, Siphon, Brace, Bandage };
}
