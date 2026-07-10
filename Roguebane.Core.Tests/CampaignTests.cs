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
        var foe = c.Enemy;
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
        // Activation default refinement [LOCKED 2026-07-09]: Timered techniques go cold on every
        // encounter rearm now, so re-toggle each fight like a real player would (filtered to inactive
        // so an already-active Sustained default is never double-toggled off).
        foreach (var t in c.Current.Equipment) if (!c.IsActive(t)) c.Toggle(t);
        var guard = 0;
        while (c.Current.State == ExpeditionState.Fighting && guard++ < 10000) { AimAll(c); c.Tick(); }
        c.Redeploy(); // a cleared node holds at Cleared -> redeploy back to the chart before the next jump
    }

    [Fact]
    public void WinningALegAdvancesToTheNextCity()
    {
        var c = FullLoadout();
        Assert.Equal(0, c.LegIndex);

        ClearLeg(c);

        Assert.Equal(CampaignState.Redeploying, c.State);
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

    // §6e technique roster, campaign layer: equip/unequip must mirror into _loadout (not just the live
    // leg's Expedition), because NewLeg() rebuilds Current FROM _loadout on every leg advance — a change
    // that only touched the live leg would be silently lost the moment the party wins and marches on.
    private static Campaign CappedCampaign(int slots)
    {
        var body = Sessions.DemoBody();
        var caster = new Caster(body, requireAim: true);
        var legs = new Func<CityMap>[]
        {
            () => Maps.StandardLeg(autoResolveCastle: false),
            () => Maps.StandardLeg(autoResolveCastle: false),
        };
        return new Campaign(Forge.PlayerFighter(body), caster, new[] { Techniques.Jab }, legs,
            techniqueSlots: slots);
    }

    [Fact]
    public void CampaignEquipTechniqueAddsToTheCurrentLegsBar()
    {
        var c = CappedCampaign(2);
        Assert.True(c.EquipTechnique(Techniques.Cleave));
        Assert.Contains(Techniques.Cleave, c.Current.Equipment);
    }

    [Fact]
    public void CampaignEquipRefusesPastTheChassisKitCap()
    {
        var c = CappedCampaign(1); // already full at 1 slot
        Assert.False(c.EquipTechnique(Techniques.Cleave));
    }

    [Fact]
    public void CampaignUnequipTechniqueRemovesFromTheCurrentLegsBar()
    {
        var c = CappedCampaign(2);
        Assert.True(c.UnequipTechnique(Techniques.Jab));
        Assert.DoesNotContain(Techniques.Jab, c.Current.Equipment);
    }

    [Fact]
    public void CampaignReorderMovesATechniqueInTheCurrentLegsBar()
    {
        var c = CappedCampaign(2);
        c.EquipTechnique(Techniques.Cleave); // [Jab, Cleave]
        Assert.True(c.ReorderTechnique(Techniques.Cleave, 0));
        Assert.Equal(new[] { Techniques.Cleave, Techniques.Jab }, c.Current.Equipment);
    }

    [Fact]
    public void ReorderedTechniqueOrderSurvivesALegAdvance()
    {
        var c = FullLoadout(); // Techniques.All, uncapped, active + AUTO
        var reordered = c.Current.Equipment[^1];
        Assert.True(c.ReorderTechnique(reordered, 0));
        Assert.Equal(reordered, c.Current.Equipment[0]);

        ClearLeg(c); // NewLeg() rebuilds Current from _loadout

        Assert.Equal(reordered, c.Current.Equipment[0]); // order preserved, not reset by the rebuild
    }

    [Fact]
    public void UnequippedTechniqueStaysUnequippedAfterALegAdvance()
    {
        var c = FullLoadout(); // Techniques.All, uncapped, active + AUTO
        Assert.True(c.UnequipTechnique(Techniques.Bandage));
        Assert.DoesNotContain(Techniques.Bandage, c.Current.Equipment);

        ClearLeg(c); // NewLeg() rebuilds Current from _loadout

        Assert.DoesNotContain(Techniques.Bandage, c.Current.Equipment); // not resurrected by the rebuild
    }
}
