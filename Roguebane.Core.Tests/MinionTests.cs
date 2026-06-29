using Roguebane.Core.Content;

namespace Roguebane.Core.Tests;

// G7 (minions): bay-bound, stat-reserving allied units that auto-fire and cascade off when their
// stat drains — the same body rule as techniques.
public class MinionTests
{
    private static Body IntBody(int intel)
    {
        var body = new Body();
        body.Add(new BodyPart("head", Stat.Int, intel));
        return body;
    }

    [Fact]
    public void AMinionAutoFiresOnTheFront()
    {
        var foe = new Foe("front", 100);
        var caster = new Caster(IntBody(9), foe);
        Assert.True(caster.Summon(Minions.Skeleton, bayCap: 3)); // power 1

        caster.Step();
        Assert.Equal(99, foe.Hp);
    }

    [Fact]
    public void BaysCapTheNumberOfMinions()
    {
        var caster = new Caster(IntBody(20), null);
        Assert.True(caster.Summon(Minions.Skeleton, bayCap: 1));
        Assert.False(caster.Summon(Minions.Shade, bayCap: 1)); // no free bay
        Assert.Equal(1, caster.MinionCount);
    }

    [Fact]
    public void SummoningGatesOnFreeStat()
    {
        var caster = new Caster(IntBody(2), null); // only 2 INT
        Assert.True(caster.Summon(Minions.Skeleton, bayCap: 3)); // reserves 2 -> ok
        Assert.False(caster.Summon(Minions.Shade, bayCap: 3));   // needs 3 more INT, none free
    }

    [Fact]
    public void DrainingTheStatDismissesTheMinion()
    {
        var body = IntBody(2);
        var head = body.Parts[0];
        var foe = new Foe("front", 100);
        var caster = new Caster(body, foe);
        caster.Summon(Minions.Skeleton, bayCap: 3);

        caster.Step();
        Assert.Equal(99, foe.Hp);

        body.Damage(head, 2); // INT -> 0, skeleton can no longer stand
        caster.Step();        // pruned before firing
        Assert.Equal(0, caster.MinionCount);
        Assert.Equal(99, foe.Hp); // no further chip
    }

    [Fact]
    public void DismissReturnsTheReservedStat()
    {
        var body = IntBody(5);
        var caster = new Caster(body, null);
        caster.Summon(Minions.Shade, bayCap: 3); // reserves 3
        Assert.Equal(2, body.Available(Stat.Int));

        caster.Dismiss(Minions.Shade);
        Assert.Equal(5, body.Available(Stat.Int));
        Assert.Equal(0, caster.MinionCount);
    }

    [Fact]
    public void SummonerHasThreeBaysTheWardenNone()
    {
        Assert.Equal(3, Chassrium.Summoner.Bays);
        Assert.Equal(0, Chassrium.Warden.Bays);
        Assert.Equal(1, Chassrium.Grunt.Bays);
    }

    [Fact]
    public void UngatedMinionCostsNoStatAndSurvivesADrain()
    {
        var body = IntBody(0); // no INT at all
        var foe = new Foe("front", 100);
        var caster = new Caster(body, foe);
        var retinue = new Minion("retinue", Stat.Int, 0, 1, MinionGate.None);

        Assert.True(caster.Summon(retinue, bayCap: 3)); // ungated -> succeeds with zero INT
        body.Damage(body.Parts[0], 5);                  // drain does nothing it depends on
        caster.Step();
        Assert.Equal(1, caster.MinionCount);            // still standing
        Assert.Equal(99, foe.Hp);
    }

    [Fact]
    public void AltCostMinionSpendsChargeNotStat()
    {
        var body = IntBody(0);
        var caster = new Caster(body, null, maxCharge: 3);
        var imp = new Minion("imp", Stat.Int, 0, 1, MinionGate.AltCost, AltCost: 2);

        Assert.True(caster.Summon(imp, bayCap: 3)); // pays 2 charge
        Assert.Equal(1, caster.Charge);
        var imp2 = new Minion("imp2", Stat.Int, 0, 1, MinionGate.AltCost, AltCost: 2);
        Assert.False(caster.Summon(imp2, bayCap: 3)); // only 1 charge left
    }
}
