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
}
