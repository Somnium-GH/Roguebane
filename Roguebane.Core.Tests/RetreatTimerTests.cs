using Roguebane.Core.Content;

namespace Roguebane.Core.Tests;

// Item 5 (Doug 2026-07-12): the DEX-timed Retreat/Redeploy availability formula — 30s base (300 ticks),
// 2% faster per DEX point, reusing the Caster.HasteRate/HasteCap convention (never a new cap). These
// pin the LOCKED math; the wiring (when it ticks / what it gates) is flagged separately in STATUS.
public class RetreatTimerTests
{
    [Fact]
    public void ZeroDex_takes_the_full_300_tick_base()
        => Assert.Equal(300, RetreatTimer.EffectiveTicks(0));

    [Theory]
    [InlineData(1, 294)]  // 2% -> 300*0.98 = 294
    [InlineData(5, 270)]  // 10% -> 270
    [InlineData(10, 240)] // 20% -> 240
    public void Dex_shaves_two_percent_per_point(int dex, int expected)
        => Assert.Equal(expected, RetreatTimer.EffectiveTicks(dex));

    [Fact]
    public void Haste_is_capped_reusing_Caster_HasteCap()
    {
        // At/above HasteCap/HasteRate DEX the reduction saturates at HasteCap% (28) -> 300*0.72 = 216.
        var atCap = Caster.HasteCap / Caster.HasteRate; // 14
        var expected = (int)System.Math.Round(300 * (1 - Caster.HasteCap / 100.0));
        Assert.Equal(expected, RetreatTimer.EffectiveTicks(atCap));
        Assert.Equal(expected, RetreatTimer.EffectiveTicks(atCap + 50)); // no further speedup past the cap
    }

    [Fact]
    public void Progress_runs_zero_to_one_and_clamps()
    {
        var eff = RetreatTimer.EffectiveTicks(0); // 300
        Assert.Equal(0.0, RetreatTimer.Progress(0, 0));
        Assert.Equal(0.5, RetreatTimer.Progress(0, eff / 2), 3);
        Assert.Equal(1.0, RetreatTimer.Progress(0, eff));
        Assert.Equal(1.0, RetreatTimer.Progress(0, eff * 3)); // clamps, never overfills
        Assert.False(RetreatTimer.Ready(0, eff - 1));
        Assert.True(RetreatTimer.Ready(0, eff));
    }

    // Integration: the timer starts at 0 on ARRIVAL (Enter -> Fighting) and fills as the fight ticks.
    [Fact]
    public void RetreatProgress_starts_at_zero_on_arrival_and_fills_while_fighting()
    {
        var exp = Sessions.Expedition();
        Assert.True(exp.Enter("a2")); // a skirmish
        Assert.Equal(ExpeditionState.Fighting, exp.State);
        Assert.Equal(0.0, exp.RetreatProgress); // starts on arrival, not on clear

        var dex = exp.Player.Body.Capacity(Stat.Dex);
        var ticks = 0;
        for (; ticks < 20 && exp.State == ExpeditionState.Fighting; ticks++) exp.Tick();

        Assert.Equal(ExpeditionState.Fighting, exp.State); // fight didn't end inside the window
        Assert.True(exp.RetreatProgress > 0);
        Assert.Equal(RetreatTimer.Progress(dex, ticks), exp.RetreatProgress, 6);
    }
}
