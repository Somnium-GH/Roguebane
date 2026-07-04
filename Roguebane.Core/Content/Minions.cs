namespace Roguebane.Core.Content;

// Minions are content. The POC bestiary is INT-funded (the Binder's summons) + one DEX pet; other
// stat re-homes are parked (see "Needs human"). §9 T1 blessed-initial numbers, 2026-07-04: archetype
// split at roughly T1 1H-weapon-technique parity (~0.4 dmg/s) — fast/weak vs slow/strong, same total
// DPS, different lever.
public static class Minions
{
    // Fast/weak: frequent small hits. Timer 25 ticks (2.5s), Power 1 -> ~0.4 dmg/s.
    public static readonly Minion Skeleton = new("skeleton", Stat.Int, Reserve: 2, Power: 1, Timer: 25,
        Desc: "A raised thrall that strikes for {power} damage every {timer} ticks.");

    // Slow/strong, replaces Shade's role: Timer 100 ticks (10s), Power 4 -> ~0.4 dmg/s.
    public static readonly Minion Golem = new("golem", Stat.Int, Reserve: 3, Power: 4, Timer: 100,
        Desc: "A bound golem that hits hard for {power} every {timer} ticks while its reserve holds.");

    // DEX-gated pet (§9: DEX = utility/evasion, not raw DPS) — deliberately the weakest per-reserve-
    // point placeholder; its real distinguishing EFFECT (evasion/accuracy grant, not damage) rides the
    // minion stat->role pass (§17 #5) as its own slice. Timer 40 ticks (4s), Power 1 -> ~0.25 dmg/s.
    public static readonly Minion Hound = new("hound", Stat.Dex, Reserve: 1, Power: 1, Timer: 40,
        Desc: "A hound that nips for {power} every {timer} ticks.");

    // Duplicated Skeleton's role with no distinct playstyle now that Golem fills the slow/strong slot
    // (DESIGN_SPEC §9) -- likely retired, NOT yet deleted (Needs human: confirm with Doug first).
    // Kept off `All` so new content doesn't pick it up by accident while the retire call is pending.
    public static readonly Minion Shade = new("shade", Stat.Int, Reserve: 3, Power: 2, Timer: 50,
        Desc: "A bound shade that hits hard for {power} every {timer} ticks while its reserve holds.");

    public static readonly IReadOnlyList<Minion> All = new[] { Skeleton, Golem, Hound };
}
