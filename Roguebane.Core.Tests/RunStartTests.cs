using Roguebane.Core.Content;

namespace Roguebane.Core.Tests;

// Regression for the run-start CRASH (a stray glyph drawn on the freshly-started MAP screen). Drives
// the shell's exact start path -- build a chassis, Redeploy a campaign -- and locks the render-facing
// contract the map screen reads at run-start, so a future regression that guts that state fails here
// headlessly instead of only at draw time. (The glyph fold itself is covered by GlyphSafeTests.)
public class RunStartTests
{
    private static Expedition StartedRun()
    {
        var build = Sessions.NewBuild();
        var campaign = build.Redeploy(Maps.StandardLegs(3));
        return campaign.Current; // the leg the run screen renders (Exp in the shell)
    }

    [Fact]
    public void AFreshRunOpensAtCampReadyToChoose()
    {
        var exp = StartedRun();
        Assert.Equal(ExpeditionState.Choosing, exp.State);
        Assert.Equal("camp", exp.Map.CurrentId);
        Assert.NotEmpty(exp.Options); // charted jumps to draw on the chart
        Assert.Null(exp.Battle);      // no fight yet
        Assert.Null(exp.Enemy);
    }

    [Fact]
    public void TheRunStartStateTheMapScreenDrawsIsCoherent()
    {
        var exp = StartedRun();

        // Player figure + HP bar.
        Assert.True(exp.Player.Hp > 0);
        Assert.True(exp.Player.Hp <= exp.Player.MaxHp);
        Assert.NotEmpty(exp.Player.Body.Parts);

        // Run-resource readouts (supplies / war-party / support / gold).
        Assert.True(exp.Map.WarPartyDistance > 0);
        Assert.True(exp.Map.SupportBank >= 0);
        Assert.True(exp.Gold >= 0);

        // Bay lane + equipment/gear bar sources are readable (the crash was drawing the gear bar).
        Assert.True(exp.Bays >= 0);
        Assert.Equal(0, exp.MinionCount);
        Assert.NotNull(exp.Equipment);          // may be empty (the dash placeholder) -- just must be drawable
        Assert.NotNull(exp.Player.Body.Hands); // the wielded-weapon chips
    }
}
