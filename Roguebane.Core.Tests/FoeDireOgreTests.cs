using Roguebane.Core.Content;

namespace Roguebane.Core.Tests;

// CHUNK D item 2's eighth roster foe (FOES.md, Dire Ogre T2): arm STR raised 5->8 (Doug 2026-07-07,
// the Foe attribute-for-equipment rule) so Iron Warhammer's 5 STR + STR Breastplate's 2 STR (7 total)
// leaves real T2 headroom instead of the old 5-STR arm's zero. Consults: Primary on BOTH Swing and
// Cleave zeroes their own Reserve field (Caster.ResolveReservation) -- the wielded WEAPON's own
// Reserve is what's tracked, so only gear (not technique activation) counts against the arm's budget.
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

        frame.Damage(frame.Parts[0], 4); // arm STR 8 -> 4, below the Iron Warhammer's Reserve 5

        Assert.Empty(frame.Consulted(Armory.Swing));
        Assert.Empty(frame.Consulted(Techniques.Cleave));
    }

    [Fact]
    public void DireOgresArmHasExactlyOneHeadroomAboveTheWarhammerAndBreastplatesCombinedCost()
    {
        var foe = Foes.DireOgre("dire-ogre");
        // Warhammer Req 5 + Breastplate Requirement 2 (Governing Str, Plate = 2 x Tier 1) == 7 of 8.
        Assert.Equal(1, foe.Frame!.Available(Stat.Str));
    }
}
