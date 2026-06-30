namespace Roguebane.Core.Tests;

// §8: foes erode the player's PARTS, not its HP — the limb chosen by a per-foe TARGETING PERSONALITY.
public class FoeTargetingTests
{
    // arm STR 4 (largest), head INT 1 (smallest), legs DEX 3, chest CON 2.
    private static Body Body4()
    {
        var b = new Body();
        b.Add(new BodyPart("arm", Stat.Str, 4));
        b.Add(new BodyPart("head", Stat.Int, 1));
        b.Add(new BodyPart("legs", Stat.Dex, 3));
        b.Add(new BodyPart("chest", Stat.Con, 2));
        return b;
    }

    [Fact]
    public void SmartStripsTheLargestLiveStatShare()
    {
        var part = FoeTargeting.Pick(FoeAim.Smart, Body4(), new Rng(1));
        Assert.Equal(Stat.Str, part!.Stat); // arm, contribution 4
    }

    [Fact]
    public void IneptWastesTheSwingOnTheSmallestShare()
    {
        var part = FoeTargeting.Pick(FoeAim.Inept, Body4(), new Rng(1));
        Assert.Equal(Stat.Int, part!.Stat); // head, contribution 1
    }

    [Fact]
    public void SmartTracksErosion()
    {
        var body = Body4();
        body.Damage(body.Parts[0], 4); // smash the arm to 0 -> legs (3) is now the largest standing
        var part = FoeTargeting.Pick(FoeAim.Smart, body, new Rng(1));
        Assert.Equal(Stat.Dex, part!.Stat);
    }

    [Fact]
    public void RandomOnlyEverPicksAStandingPart()
    {
        var body = Body4();
        body.Damage(body.Parts[0], 4); // arm gone
        var rng = new Rng(7);
        for (var i = 0; i < 50; i++)
            Assert.True(body.Contribution(FoeTargeting.Pick(FoeAim.Random, body, rng)!) > 0);
    }

    [Fact]
    public void RandomIsDeterministicForASeed()
    {
        var x = new Rng(3);
        var y = new Rng(3);
        for (var i = 0; i < 20; i++) // two identical streams pick the same sequence
            Assert.Equal(FoeTargeting.Pick(FoeAim.Random, Body4(), x)!.Id,
                         FoeTargeting.Pick(FoeAim.Random, Body4(), y)!.Id);
    }

    [Fact]
    public void NoStandingPartReturnsNull()
    {
        var body = new Body();
        var part = new BodyPart("p", Stat.Str, 2);
        body.Add(part);
        body.Damage(part, 2);
        Assert.Null(FoeTargeting.Pick(FoeAim.Smart, body, new Rng(1)));
    }
}
