using Roguebane.Core.Content;

namespace Roguebane.Core.Tests;

// Every CORE must be PLAYABLE end to end: assembled from a Race + its OWN default kit (not the bespoke
// balance-sim body), it drives a full campaign to a terminal state without hanging or crashing — and the
// loop is actually BEATABLE with a real core, not just the sim body.
public class CoreCampaignTests
{
    private static Campaign Embark(Race race, CoreRune core) =>
        Forge.EmbarkCampaign(race, core, core.NewLoadout(), core.Kit, Maps.StandardLegs(3));

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

    private static CampaignState RunCampaign(Race race, CoreRune core)
    {
        var c = Embark(race, core);
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
    public void EveryRaceAndCoreDrivesAFullCampaignToATerminalState()
    {
        // No hang, no crash: every Race x Core combo assembled with its own kit resolves to Won or Lost.
        foreach (var race in Races.Roster)
            foreach (var core in CoreRunes.Roster)
            {
                var outcome = RunCampaign(race, core);
                Assert.True(outcome is CampaignState.Won or CampaignState.Lost,
                    $"{race.Id}/{core.Id} did not terminate (state {outcome})");
            }
    }

    [Fact]
    public void EveryRaceAndCoreWinsTheCampaignWithPartAimPlay()
    {
        // The run is winnable for real (not just with the synthetic sim body): with the INTENDED §8 play
        // -- part-aim the foe's STR arm to cascade its strike off, disabling offense -- EVERY Race x Core
        // combo clears the campaign on its own default kit at design-scale race stats (incl. the frail
        // Elf + shield-less glass/caster cores), because disabling the boss beats out-tanking it.
        foreach (var race in Races.Roster)
            foreach (var core in CoreRunes.Roster)
                Assert.Equal(CampaignState.Won, RunCampaign(race, core));
    }
}
