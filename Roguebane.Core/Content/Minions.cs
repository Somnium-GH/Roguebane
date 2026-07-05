namespace Roguebane.Core.Content;

// Minions are content. The POC bestiary is INT-funded (the Binder's summons) + one DEX pet; other
// stat re-homes are parked (see "Needs human"). §9 T1 blessed-initial numbers, 2026-07-04: archetype
// split at roughly T1 1H-weapon-technique parity (~0.4 dmg/s) — fast/weak vs slow/strong, same total
// DPS, different lever.
public static class Minions
{
    // T1 INT, r1: fast/weak, frequent small hits. Timer 30 ticks (3.0s), Power 1 (v6 sync).
    public static readonly Minion Skeleton = new("skeleton", Stat.Int, Reserve: 1, Power: 1, Timer: 30,
        Desc: "A raised thrall that strikes for {power} damage every {timer} ticks.");

    // T2 INT, r2: slow/strong, replaces Shade's role. Timer 50 ticks (5.0s), Power 3 (v6 sync).
    public static readonly Minion IronGolem = new("iron_golem", Stat.Int, Reserve: 2, Power: 3, Timer: 50,
        Desc: "A bound iron golem that hits hard for {power} every {timer} ticks while its reserve holds.");

    // T1 DEX pet, r1: Timer 40 ticks (4s), Power 1, +5% accuracy while fielded (TECHNIQUES.md).
    public static readonly Minion Hound = new("hound", Stat.Dex, Reserve: 1, Power: 1, Timer: 40,
        AccuracyBonus: 5,
        Desc: "A hound that nips for {power} every {timer} ticks and sharpens your aim while fielded.");

    // Duplicated Skeleton's role with no distinct playstyle now that Golem fills the slow/strong slot
    // (DESIGN_SPEC §9) -- likely retired, NOT yet deleted (Needs human: confirm with Doug first).
    // Kept off `All` so new content doesn't pick it up by accident while the retire call is pending.
    public static readonly Minion Shade = new("shade", Stat.Int, Reserve: 3, Power: 2, Timer: 50,
        Desc: "A bound shade that hits hard for {power} every {timer} ticks while its reserve holds.");

    public static readonly IReadOnlyList<Minion> All = new[] { Skeleton, IronGolem, Hound };
}
