namespace Roguebane.Core.Content;

// Six techniques on the four-stat model, all interpreted by the one tick loop. Cooldowns are in
// COMBAT TICKS at the fixed 10 ticks/sec clock (value/10 = seconds): weak attacks ~4-6s, strong
// ~12-15s, small damage (1-3) so a fight runs 30s+ and stays watchable. DEX further shortens these
// at cast time (haste). Brace alone is Sustained (a held CON reservation, power 0 — the §6b shield
// source); the damage techniques auto-repeat on their real-second cadence. STR swings, DEX strikes,
// INT bolts (silenced if the head drains), CON braces.
public static class Techniques
{
    public static readonly Technique Jab =
        new("jab", Stat.Str, Reserve: 1, TechniqueKind.Timered, Cooldown: 50, Power: 2,    // ~5s
            Desc: "A quick swing of your blade for {power} damage.");

    public static readonly Technique Cleave =
        new("cleave", Stat.Str, Reserve: 2, TechniqueKind.Timered, Cooldown: 140, Power: 3, // ~14s; reserve 2 so the frail Elf can still swing it
            Desc: "A heavy committed arc that lands {power} damage.");

    public static readonly Technique Lunge =
        new("lunge", Stat.Dex, Reserve: 1, TechniqueKind.Timered, Cooldown: 45, Power: 2,  // ~4.5s
            Desc: "A darting thrust for {power} damage.");

    public static readonly Technique Ember =
        new("ember", Stat.Int, Reserve: 1, TechniqueKind.Timered, Cooldown: 30, Power: 1,  // ~3s bolt
            Desc: "A mote of fire hurled from afar for {power} damage.");

    public static readonly Technique Drain =
        new("drain", Stat.Int, Reserve: 2, TechniqueKind.Timered, Cooldown: 60, Power: 2,  // ~6s
            Desc: "A draining bolt that saps {power} from the struck part.");

    // The CON shield source (was the flat "block", retired with §8): a held passive that reserves CON
    // and maintains a regenerating pool of shield layers — the §6b mitigation that now stands between a
    // hit and the body (§8: shields + full evade are the only mitigations). Numbers placeholder.
    public static readonly Technique Brace =
        new("brace", Stat.Con, Reserve: 2, TechniqueKind.Sustained, Cooldown: 0, Power: 0,
            ShieldLayers: 4, ShieldRegen: 15,
            Desc: "Raise your guard and hold a wall of regenerating shield layers.");

    // The §10 in-combat part-heal (CON): mends the most-damaged part ~1 every 8s, reserving CON while
    // held. Kept OUT of `All` for now so it stays opt-in content (no balance shift to the default
    // palette); it is the reconcile trigger for live foe part-aim (G1, staged off until heals exist).
    public static readonly Technique Bandage =
        new("bandage", Stat.Con, Reserve: 1, TechniqueKind.Timered, Cooldown: 80, Power: 1, Heals: true,
            Desc: "Binds your most-damaged part, mending {power} each pass.", Side: TargetSide.Self);

    // A §6b SHIELD SOURCE: a passive INT spell that holds a pool of 3 stone layers, each eating one hit,
    // one regenerating ~every 3s (faster with CON). Reserves INT while held. Opt-in content (not in
    // `All`): dormant until placed in a kit, so it perturbs no current balance. Numbers are placeholder.
    public static readonly Technique Stoneskin =
        new("stoneskin", Stat.Int, Reserve: 2, TechniqueKind.Sustained, Cooldown: 0, Power: 0,
            ShieldLayers: 3, ShieldRegen: 30,
            Desc: "A held spell that keeps stone layers between you and harm.");

    // Bandage is now in the palette + the starting kits (below): every build fights with a part-heal so
    // it can survive live foe part-aim on skirmishes; a build that drops it pays the intended penalty.
    public static readonly IReadOnlyList<Technique> All =
        new[] { Jab, Cleave, Lunge, Ember, Drain, Brace, Bandage };
}
