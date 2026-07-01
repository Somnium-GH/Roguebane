using Roguebane.Core.Content;

namespace Roguebane.Core.Tests;

// G6: the campaign spine — several legs to the Capital, one body/stash carrying through, a fresh war
// party each leg.
public class CampaignTests
{
    // Unattended drive-to-completion harness: arm + AUTO on. The player requires an explicit aim to
    // fire (no front fallback), so the harness re-aims at the first standing foe each tick (below).
    private static Campaign FullLoadout()
    {
        var c = Sessions.NewCampaign();
        foreach (var t in Techniques.All) c.Toggle(t);
        c.SetAuto(true); // global AUTO on so the re-aimed targets persist
        return c;
    }

    private static void AimAll(Campaign c)
    {
        var foe = c.Foes.FirstOrDefault(f => !f.Down);
        if (foe is null) return;
        foreach (var t in Techniques.All) if (c.IsActive(t)) c.Aim(t, foe);
    }

    private static void ClearLeg(Campaign c)
    {
        // camp -> a2 -> b (merchant) -> c1 -> castle, fighting each combat node to the end.
        Step(c, "a2");
        c.Enter("b");
        Step(c, "c1");
        Step(c, "castle");
    }

    private static void Step(Campaign c, string node)
    {
        c.Enter(node);
        var guard = 0;
        while (c.Current.State == ExpeditionState.Fighting && guard++ < 10000) { AimAll(c); c.Tick(); }
    }

    [Fact]
    public void WinningALegAdvancesToTheNextCity()
    {
        var c = FullLoadout();
        Assert.Equal(0, c.LegIndex);

        ClearLeg(c);

        Assert.Equal(CampaignState.Marching, c.State);
        Assert.Equal(1, c.LegIndex);
        Assert.Equal("camp", c.Current.Map.CurrentId); // a fresh leg
    }

    [Fact]
    public void WinningTheFinalLegTakesTheCapital()
    {
        var c = FullLoadout();
        for (var leg = 0; leg < c.LegCount; leg++) ClearLeg(c);

        Assert.Equal(CampaignState.Won, c.State);
        Assert.Equal(c.LegCount - 1, c.LegIndex);
    }

    [Fact]
    public void FreshWarPartyEachLeg()
    {
        var c = FullLoadout();
        var firstMarch = c.Current.Map.WarPartyDistance;
        ClearLeg(c);
        Assert.Equal(firstMarch, c.Current.Map.WarPartyDistance); // reset for the new leg
    }

    [Fact]
    public void StashCarriesAcrossLegs()
    {
        var c = FullLoadout();
        ClearLeg(c);
        Assert.True(c.Stash.Gold > 0); // spoils banked in leg 1 persist into leg 2
        Assert.Same(c.Stash, c.Current.Stash);
    }

    [Fact]
    public void HpRestoresAtEachNewCityButTheBodyPersists()
    {
        var c = FullLoadout();
        var body = c.Current.Player.Body;

        ClearLeg(c); // fight the whole leg (skirmishes erode parts, Bandage mends them) -> rest at city

        // HP is topped up at the new city (out-of-combat recovery, §10)...
        Assert.Equal(c.Current.Player.MaxHp, c.Current.Player.Hp);
        // ...but the SAME body carries into the next leg: parts persist, the city mints no fresh body.
        Assert.Same(body, c.Current.Player.Body);
    }
}
