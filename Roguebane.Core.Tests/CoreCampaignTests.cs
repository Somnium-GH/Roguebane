using Roguebane.Core.Content;

namespace Roguebane.Core.Tests;

// Every CORE must be PLAYABLE end to end: assembled from a Race + its OWN default kit (not the bespoke
// balance-sim body), it drives a full campaign to a terminal state without hanging or crashing — and the
// loop is actually BEATABLE with a real core, not just the sim body.
public class CoreCampaignTests
{
    private static Campaign Embark(CoreRune core) =>
        Forge.EmbarkCampaign(Races.Human, core, core.NewLoadout(), core.Kit, Maps.StandardLegs(3));

    private static void Fight(Campaign c, string node)
    {
        c.Enter(node);
        var guard = 0;
        while (c.Current.State == ExpeditionState.Fighting && guard++ < 10000)
        {
            if (c.Enemy is { } foe)
            {
                // Decent play: aim at the foe's STR arm when it has one, so smashing it cascades the
                // foe's strike off (its own body rule) -- disabling offense, not just chipping HP.
                var arm = foe.Frame?.Parts.FirstOrDefault(p => p.Stat == Stat.Str);
                foreach (var t in c.Current.Equipment)
                    if (c.IsActive(t)) { if (arm is not null) c.Aim(t, foe, arm); else c.Aim(t, foe); }
            }
            c.Tick();
        }
        if (c.State == CampaignState.Redeploying) c.Redeploy(); // a cleared node holds -> back to the chart
    }

    private static CampaignState RunCampaign(CoreRune core)
    {
        var c = Embark(core);
        foreach (var t in c.Current.Equipment) c.Toggle(t); // power the bar
        c.SetAuto(true);                                     // keep the re-aimed targets

        for (var leg = 0; leg < 3 && c.State == CampaignState.Redeploying; leg++)
        {
            Fight(c, "a2"); if (c.State != CampaignState.Redeploying) break;
            c.Enter("b");   // the merchant node: no fight
            Fight(c, "c1"); if (c.State != CampaignState.Redeploying) break;
            Fight(c, "castle");
        }
        return c.State;
    }

    [Fact]
    public void EveryCoreDrivesAFullCampaignToATerminalState()
    {
        // No hang, no crash: each core assembled with its own kit resolves to Won or Lost.
        foreach (var core in CoreRunes.Roster)
        {
            var outcome = RunCampaign(core);
            Assert.True(outcome is CampaignState.Won or CampaignState.Lost,
                $"{core.Id} did not terminate (state {outcome})");
        }
    }

    [Fact]
    public void EveryCoreWinsTheCampaignWithPartAimPlay()
    {
        // The run is winnable for real (not just with the synthetic sim body): with the INTENDED §8 play
        // -- part-aim the foe's STR arm to cascade its strike off, disabling offense -- EVERY core clears
        // the campaign on its own default kit. Even the shield-less glass/caster cores, at design-scale
        // race stats, because disabling the boss beats out-tanking it.
        foreach (var core in CoreRunes.Roster)
            Assert.Equal(CampaignState.Won, RunCampaign(core));
    }
}
