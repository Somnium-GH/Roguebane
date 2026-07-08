using Roguebane.Core.Content;

namespace Roguebane.Core.Tests;

// CHUNK D item 2's eighth roster foe (FOES.md, Dire Ogre T2): arm STR raised 5->8->10 (Doug
// 2026-07-07, the Foe attribute-for-equipment rule). Gear alone (Iron Warhammer 5 STR + STR
// Breastplate 2 STR = 7) only needed the 8; the same-day reservation-additive bug fix
// (Caster.ResolveReservation/Body.EffectiveTechniqueReserve: Consults==Primary techniques no longer
// zero their own Reserve) makes Cleave's own 2 STR reserve additively real once active, so full
// demand is 7 + 2 = 9 -- arm STR 10 keeps Doug's +1-headroom preference over that.
public class FoeDireOgreTests
{
    private static Fighter Bystander() => new(new Body(), maxHp: 500); // survives; applies no pressure

    [Fact]
    public void DireOgreWieldsTheWarhammerConsultedByBothSwingAndCleave()
    {
        var foe = Foes.DireOgre("dire-ogre");
        var frame = foe.Frame!;
        Assert.NotEmpty(frame.Consulted(Armory.Swing));
        Assert.NotEmpty(frame.Consulted(Techniques.Cleave));
    }

    [Fact]
    public void SwingDealsTheWieldedWarhammersFullPower()
    {
        var foe = Foes.DireOgre("dire-ogre");
        var player = Bystander();
        var caster = new Caster(foe.Frame!, player);
        caster.Activate(Armory.Swing, auto: false);
        caster.Aim(Armory.Swing, player);
        var guard = 0;
        while (!caster.IsReady(Armory.Swing) && guard++ < 500) caster.Step();

        Assert.True(caster.Fire(Armory.Swing));
        Assert.Equal(500 - Armory.Warhammers[0].Power, player.Hp);
    }

    [Fact]
    public void CleaveDealsOneAndAHalfTimesTheWieldedWarhammersPower()
    {
        var foe = Foes.DireOgre("dire-ogre");
        var player = Bystander();
        var caster = new Caster(foe.Frame!, player);
        caster.Activate(Techniques.Cleave, auto: false);
        caster.Aim(Techniques.Cleave, player);
        var guard = 0;
        while (!caster.IsReady(Techniques.Cleave) && guard++ < 500) caster.Step();

        Assert.True(caster.Fire(Techniques.Cleave));
        var expected = (int)Math.Round(Armory.Warhammers[0].Power * 1.5, MidpointRounding.AwayFromZero);
        Assert.Equal(500 - expected, player.Hp);
    }

    [Fact]
    public void SmashingDireOgresWeaponArmDropsTheWarhammerFromBothConsultedTechniques()
    {
        var foe = Foes.DireOgre("dire-ogre");
        var frame = foe.Frame!;
        Assert.NotEmpty(frame.Consulted(Armory.Swing));
        Assert.NotEmpty(frame.Consulted(Techniques.Cleave));

        frame.Damage(frame.Parts[0], 6); // arm STR 10 -> 4, below the Iron Warhammer's Reserve 5

        Assert.Empty(frame.Consulted(Armory.Swing));
        Assert.Empty(frame.Consulted(Techniques.Cleave));
    }

    [Fact]
    public void DireOgresArmHasExactlyOneHeadroomAboveGearPlusActiveCleavesCombinedCost()
    {
        var foe = Foes.DireOgre("dire-ogre");
        var player = Bystander();
        var caster = new Caster(foe.Frame!, player);
        // Warhammer Req 5 + Breastplate Requirement 2 (Governing Str, Plate = 2 x Tier 1) == 7, gear-only.
        Assert.Equal(3, foe.Frame!.Available(Stat.Str));

        // Cleave's own Reserve 2 reserves additively once active (Caster.ResolveReservation) -- 7 + 2 == 9 of 10.
        Assert.True(caster.Activate(Techniques.Cleave, auto: false));
        Assert.Equal(1, foe.Frame!.Available(Stat.Str));
    }
}
