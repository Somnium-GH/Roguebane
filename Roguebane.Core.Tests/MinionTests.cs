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
    public void AMinionAutoFiresOnItsOwnTimerNotEveryTick()
    {
        // §9 [RESOLVED 2026-07-04]: a minion charges to its OWN Timer before its first discharge,
        // same as a Timered technique — never on every combat tick.
        var foe = new Foe("front", 100);
        var caster = new Caster(IntBody(9), foe);
        Assert.True(caster.Summon(Minions.Skeleton, bayCap: 3)); // power 1, Timer 25

        for (var i = 0; i < Minions.Skeleton.Timer - 1; i++) caster.Step();
        Assert.Equal(100, foe.Hp); // still charging

        caster.Step(); // the 25th tick -> discharge
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
    public void DrainingTheStatIdlesTheMinionAndRecoveryReRaisesFree()
    {
        // §9 [LOCKED 2026-07-02]: Summons pays ONCE — a drained gate stat only IDLES the minion (it
        // stays summoned, silent), and it re-raises FREE when the stat recovers. No re-pay.
        var body = IntBody(2);
        var foe = new Foe("front", 100);
        var caster = new Caster(body, foe, maxSummons: 1);
        Assert.True(caster.Summon(Minions.Skeleton, bayCap: 3)); // spends the only summon
        Assert.Equal(0, caster.SummonsLeft);

        var head = body.Parts.Single();
        body.Damage(head, 2);                    // drain INT -> the reservation cascades off
        caster.Step();                           // countdown keeps ticking in the background while idle
        Assert.Equal(1, caster.MinionCount);     // still SUMMONED (idle), not dismissed
        Assert.Equal(100, foe.Hp);               // but silent while idle

        body.Repair(head, 2);                    // stat recovers
        // free re-raise (no Summons left, none needed); the countdown that kept ticking while idle
        // (Timer - 2 steps so far) must still run out before the first post-recovery discharge.
        for (var i = 0; i < Minions.Skeleton.Timer - 2; i++) caster.Step();
        Assert.Equal(100, foe.Hp);               // still charging
        caster.Step();
        Assert.Equal(99, foe.Hp);                // firing again
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
        Assert.Equal(3, CoreRunes.Summoner.Bays);
        Assert.Equal(0, CoreRunes.Warden.Bays);
        Assert.Equal(1, CoreRunes.Grunt.Bays);
    }

    [Fact]
    public void UngatedMinionCostsNoStatAndSurvivesADrain()
    {
        var body = IntBody(0); // no INT at all
        var foe = new Foe("front", 100);
        var caster = new Caster(body, foe);
        var retinue = new Minion("retinue", Stat.Int, 0, 1, Timer: 1, Gate: MinionGate.None);

        Assert.True(caster.Summon(retinue, bayCap: 3)); // ungated -> succeeds with zero INT
        body.Damage(body.Parts[0], 5);                  // drain does nothing it depends on
        caster.Step();
        Assert.Equal(1, caster.MinionCount);            // still standing
        Assert.Equal(99, foe.Hp);
    }

    // G7 in play: the Summoner chassis fields its minion kit into its bays at assembly, and those
    // minions auto-fire on the front foe — so the summoner archetype actually fights through summons
    // even when the player aims no techniques (requireAim holds the bar).
    [Fact]
    public void TheSummonerFieldsItsMinionsWhichChipTheFrontFoeUnaided()
    {
        var chassis = CoreRunes.Summoner;
        var exp = Forge.Embark(Races.Human, chassis, chassis.NewLoadout(), chassis.Kit, Maps.StandardLeg(autoResolveCastle: false));
        Assert.True(exp.MinionCount > 0); // bays filled at assembly

        exp.Enter("a2");
        var foe = exp.Enemy!;
        var hp = foe.Hp;

        for (var i = 0; i < 200; i++) exp.Tick(); // no technique is aimed -> only the minions act
        Assert.True(foe.Hp < hp);
    }

    [Fact]
    public void TheMinionKitIsCappedByTheCoreRuneBays()
    {
        var reaver = CoreRunes.Reaver; // zero bays
        var exp = Forge.Embark(Races.Human, reaver, reaver.NewLoadout(), reaver.Kit, Maps.StandardLeg(autoResolveCastle: false));
        Assert.Equal(0, exp.MinionCount); // no bays -> nothing fielded
    }

    [Fact]
    public void AltCostSummonDoesNotSpendCharge()
    {
        // Charge is the shield-pierce resource now, NOT summon fuel (§9). An alt-cost summon leaves
        // Charge untouched (its real HP/stat cost is TBD until an alt-cost minion is authored).
        var body = IntBody(0);            // no INT to reserve
        var caster = new Caster(body, null, maxCharge: 3);
        var imp = new Minion("imp", Stat.Int, 0, 1, Timer: 1, Gate: MinionGate.AltCost, AltCost: 2);

        Assert.True(caster.Summon(imp, bayCap: 3)); // succeeds without a stat reservation
        Assert.Equal(3, caster.Charge);             // Charge untouched
    }
}
