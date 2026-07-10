using Roguebane.Core;
using Roguebane.Core.Content;
using Roguebane.Core.Layout;

namespace Roguebane.Core.Tests;

// FigureBinding maps the Core anatomy onto a humanoid's visual parts: per-part condition (with
// L/R limb splitting) and the armored-vs-bare choice. Pins the sim->figure contract.
public class FigureBindingTests
{
    private static Body Humanoid(out BodyPart armL, out BodyPart armR, out BodyPart legs, out BodyPart torso)
    {
        var b = new Body();
        armL = new BodyPart("armL", Stat.Str, 4);
        armR = new BodyPart("armR", Stat.Str, 4);
        legs = new BodyPart("legs", Stat.Dex, 4);
        torso = new BodyPart("torso", Stat.Con, 6);
        b.Add(armL); b.Add(armR); b.Add(legs); b.Add(torso);
        return b;
    }

    [Fact]
    public void PairIndexOfDistinguishesLeftRightLimbsAndIsMinusOneForUnpaired()
    {
        // The reticle-mount fix (Doug item 3) maps an aimed limb to its visual half via this index;
        // L=0, R=1, and unpaired parts (head/torso/boots) return -1 so they read as a single part.
        Assert.Equal(0, FigureBinding.PairIndexOf("armL"));
        Assert.Equal(1, FigureBinding.PairIndexOf("armR"));
        Assert.Equal(0, FigureBinding.PairIndexOf("legL"));
        Assert.Equal(1, FigureBinding.PairIndexOf("legR"));
        Assert.Equal(-1, FigureBinding.PairIndexOf("head"));
        Assert.Equal(-1, FigureBinding.PairIndexOf("torso"));
        Assert.Equal(-1, FigureBinding.PairIndexOf("boots"));
    }

    [Fact]
    public void PairedLimbsSplitLeftAndRightAcrossTheGroup()
    {
        var b = Humanoid(out var armL, out _, out _, out _);
        b.Damage(armL, 4); // break the FIRST str part only

        Assert.Equal(PartCondition.Broken, FigureBinding.Condition(b, "armL"));
        Assert.Equal(PartCondition.Healthy, FigureBinding.Condition(b, "armR"));
    }

    [Fact]
    public void ConditionRidesContribution()
    {
        var b = Humanoid(out _, out _, out _, out var torso);
        Assert.Equal(PartCondition.Healthy, FigureBinding.Condition(b, "torso"));
        b.Damage(torso, 4); // 2/6 left -> below half -> damaged
        Assert.Equal(PartCondition.Damaged, FigureBinding.Condition(b, "torso"));
    }

    [Fact]
    public void BareShowsOnlyWhileTheGroupIsUnarmoured()
    {
        var b = Humanoid(out _, out _, out _, out _);
        Assert.True(FigureBinding.UseBare(b, "legL"));   // no leg armour yet
        b.Equip(Shops.Hide);                              // leather on the DEX (legs) group
        Assert.False(FigureBinding.UseBare(b, "legL"));   // now armoured -> plain row
    }

    [Fact]
    public void NonBarePartsNeverGoBare()
    {
        var b = Humanoid(out _, out _, out _, out _);
        Assert.False(FigureBinding.UseBare(b, "head"));
        Assert.False(FigureBinding.UseBare(b, "torso"));
        Assert.False(FigureBinding.UseBare(b, "boots"));
    }

    [Fact]
    public void IsArmoredTracksTheGroupArmor()
    {
        var b = Humanoid(out _, out _, out _, out _);
        Assert.False(FigureBinding.IsArmored(b, "torso"));
        b.Equip(Shops.Plate); // CON / torso group
        Assert.True(FigureBinding.IsArmored(b, "torso"));
    }

    [Fact]
    public void DisabledArmorRendersBareAndUnringed()
    {
        // §6e: the paper-doll is CAPABILITY truth — a worn piece whose GOVERNING attribute (its
        // LINE's, §6c) collapses sheds its rendered look (bare limb / no composed ring);
        // assignment stays on the Equipment card.
        var b = Humanoid(out var armL, out var armR, out var legs, out _);
        b.Equip(Shops.Hide);  // §6c leather LEGS piece — governed by DEX
        b.Equip(Shops.Plate); // §6c plate CHEST piece (Breastplate) — governed by STR
        Assert.False(FigureBinding.UseBare(b, "legL"));
        Assert.True(FigureBinding.IsArmored(b, "torso"));

        b.Damage(legs, 4);                    // the DEX pool collapses
        b.Damage(armL, 4); b.Damage(armR, 4); // both arms break -> STR 0 (§6c's own example)
        Assert.True(FigureBinding.UseBare(b, "legL"));      // disabled leather -> bare row
        Assert.False(FigureBinding.IsArmored(b, "torso"));  // disabled plate -> no ring
    }

    [Fact]
    public void BrokenArmLosesItsHandSlot()
    {
        // §6 hard override: hand 0 = handR = the SECOND Str part (add order armL, armR).
        var b = Humanoid(out var armL, out var armR, out _, out _);
        Assert.True(FigureBinding.HandUsable(b, 0));
        Assert.True(FigureBinding.HandUsable(b, 1));
        b.Damage(armR, 4); // break the right arm -> hand 0 gone, hand 1 intact
        Assert.False(FigureBinding.HandUsable(b, 0));
        Assert.True(FigureBinding.HandUsable(b, 1));
        b.Damage(armL, 4);
        Assert.False(FigureBinding.HandUsable(b, 1));
    }

    [Fact]
    public void OnlyLimbsHaveABareVariant()
    {
        Assert.True(FigureBinding.HasBareVariant("armL"));
        Assert.True(FigureBinding.HasBareVariant("legR"));
        Assert.False(FigureBinding.HasBareVariant("torso")); // -> shell draws a composed indicator
        Assert.False(FigureBinding.HasBareVariant("head"));
    }
}
