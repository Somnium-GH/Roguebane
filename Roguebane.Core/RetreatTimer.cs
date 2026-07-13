namespace Roguebane.Core;

// DEX-timed Retreat/Redeploy availability (item 5, Doug 2026-07-12 — placeholder numbers, LOCKED shape).
// The timer starts when the player ARRIVES at a node and fills to full over 30s, sped up by DEX. It is
// exposed to the shell as a 0..1 progress fraction for a fill-bar/countdown on the Retreat/Redeploy
// button. Pure + deterministic: the same (dexCapacity, elapsedTicks) always yields the same progress,
// so it obeys CLAUDE.md's fixed-timestep rule.
//
// Placeholder numbers (Doug's own framing — tunable later, not a final balance pass):
//   base 30s = 300 ticks at the fixed 10 ticks/sec clock; 2% faster per DEX point, capped, reusing the
//   EXACT technique-cooldown haste convention (Caster.HasteRate / Caster.HasteCap) rather than a new cap.
public static class RetreatTimer
{
    public const int BaseTicks = 300; // 30 seconds at 10 ticks/sec

    // Ticks until the button is available for a given DEX pool: the 300-tick base reduced by the capped
    // DEX haste. Floored at 1 tick so a huge DEX can never make it instant/zero.
    public static int EffectiveTicks(int dexCapacity)
    {
        var hastePct = System.Math.Min(Caster.HasteCap, System.Math.Max(0, dexCapacity) * Caster.HasteRate);
        return System.Math.Max(1, (int)System.Math.Round(BaseTicks * (1 - hastePct / 100.0)));
    }

    // 0..1 progress toward availability (1.0 = ready), for the UI fill-bar.
    public static double Progress(int dexCapacity, int elapsedTicks) =>
        System.Math.Clamp(elapsedTicks / (double)EffectiveTicks(dexCapacity), 0.0, 1.0);

    public static bool Ready(int dexCapacity, int elapsedTicks) => elapsedTicks >= EffectiveTicks(dexCapacity);
}
