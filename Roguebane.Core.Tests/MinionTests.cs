using Roguebane.Core.Content;

namespace Roguebane.Core.Tests;

// G7 (minions): capacity-bound, stat-reserving allied units that auto-fire and cascade off when their
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
        Assert.True(caster.Summon(Minions.Skeleton, minionCap: 3)); // power 1, Timer 25

        for (var i = 0; i < Minions.Skeleton.Timer - 1; i++) caster.Step();
        Assert.Equal(100, foe.Hp); // still charging

        caster.Step(); // the 25th tick -> discharge
        Assert.Equal(99, foe.Hp);
    }

    [Fact]
    public void MinionCapCapsTheNumberOfMinions()
    {
        var caster = new Caster(IntBody(20), null);
        Assert.True(caster.Summon(Minions.Skeleton, minionCap: 1));
        Assert.False(caster.Summon(Minions.Shade, minionCap: 1)); // no free slot
        Assert.Equal(1, caster.MinionCount);
    }

    [Fact]
    public void SummoningGatesOnFreeStat()
    {
        var caster = new Caster(IntBody(2), null); // only 2 INT
        Assert.True(caster.Summon(Minions.Skeleton, minionCap: 3)); // reserves 2 -> ok
        Assert.False(caster.Summon(Minions.Shade, minionCap: 3));   // needs 3 more INT, none free
    }

    [Fact]
    public void DrainingTheStatIdlesTheMinionAndRecoveryReRaisesFree()
    {
        // §9 [LOCKED 2026-07-02]: Summons pays ONCE — a drained gate stat only IDLES the minion (it
        // stays summoned, silent), and it re-raises FREE when the stat recovers. No re-pay.
        var body = IntBody(2);
        var foe = new Foe("front", 100);
        var caster = new Caster(body, foe, maxSummons: 1);
        Assert.True(caster.Summon(Minions.Skeleton, minionCap: 3)); // spends the only summon
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
        caster.Summon(Minions.Shade, minionCap: 3); // reserves 3
        Assert.Equal(2, body.Available(Stat.Int));

        caster.Dismiss(Minions.Shade);
        Assert.Equal(5, body.Available(Stat.Int));
        Assert.Equal(0, caster.MinionCount);
    }

    [Fact]
    public void SummonerHasThreeMinionCapTheWardenOne()
    {
        Assert.Equal(3, CoreRunes.Summoner.MinionCap);
        Assert.Equal(1, CoreRunes.Warden.MinionCap);
        Assert.Equal(2, CoreRunes.Grunt.MinionCap);
    }

    [Fact]
    public void UngatedMinionCostsNoStatAndSurvivesADrain()
    {
        var body = IntBody(0); // no INT at all
        var foe = new Foe("front", 100);
        var caster = new Caster(body, foe);
        var retinue = new Minion("retinue", Stat.Int, 0, 1, Timer: 1, Gate: MinionGate.None);

        Assert.True(caster.Summon(retinue, minionCap: 3)); // ungated -> succeeds with zero INT
        body.Damage(body.Parts[0], 5);                  // drain does nothing it depends on
        caster.Step();
        Assert.Equal(1, caster.MinionCount);            // still standing
        Assert.Equal(99, foe.Hp);
    }

    // G7 in play: the Summoner chassis fields its minion kit into its capacity at assembly, and those
    // minions auto-fire on the front foe — so the summoner archetype actually fights through summons
    // even when the player aims no techniques (requireAim holds the bar).
    [Fact]
    public void TheSummonerFieldsItsMinionsWhichChipTheFrontFoeUnaided()
    {
        var chassis = CoreRunes.Summoner;
        var exp = Forge.Embark(Races.Human, chassis, chassis.NewLoadout(), chassis.Kit, Maps.StandardLeg(autoResolveCastle: false));
        Assert.True(exp.MinionCount > 0); // capacity filled at assembly

        exp.Enter("a2");
        var foe = exp.Enemy!;
        var hp = foe.Hp;

        for (var i = 0; i < 200; i++) exp.Tick(); // no technique is aimed -> only the minions act
        Assert.True(foe.Hp < hp);
    }

    [Fact]
    public void TheMinionKitIsCappedByTheCoreRuneMinionCap()
    {
        var reaver = CoreRunes.Reaver; // zero minion capacity
        var exp = Forge.Embark(Races.Human, reaver, reaver.NewLoadout(), reaver.Kit, Maps.StandardLeg(autoResolveCastle: false));
        Assert.Equal(0, exp.MinionCount); // no capacity -> nothing fielded
    }

    [Fact]
    public void AltCostSummonDoesNotSpendCharge()
    {
        // Charge is the shield-pierce resource now, NOT summon fuel (§9). An alt-cost summon leaves
        // Charge untouched (its real HP/stat cost is TBD until an alt-cost minion is authored).
        var body = IntBody(0);            // no INT to reserve
        var caster = new Caster(body, null, maxCharge: 3);
        var imp = new Minion("imp", Stat.Int, 0, 1, Timer: 1, Gate: MinionGate.AltCost, AltCost: 2);

        Assert.True(caster.Summon(imp, minionCap: 3)); // succeeds without a stat reservation
        Assert.Equal(3, caster.Charge);             // Charge untouched
    }

    // §6e ORDERING ("slot index IS the hotkey"): minions are a SLOT-ordered list, not id-sorted — a
    // fresh Summon appends to the first free slot.
    [Fact]
    public void MinionsListInSummonOrderNotIdOrder()
    {
        var caster = new Caster(IntBody(10), null);
        Assert.True(caster.Summon(Minions.IronGolem, minionCap: 3));    // "iron_golem" summoned first...
        Assert.True(caster.Summon(Minions.Skeleton, minionCap: 3)); // ...then "skeleton" (alphabetically earlier)
        Assert.Equal(new[] { "iron_golem", "skeleton" }, caster.Minions.Select(m => m.Id));
    }

    [Fact]
    public void DismissCompactsTheMinionOrderLeft()
    {
        var caster = new Caster(IntBody(10), null); // IronGolem(2) + Skeleton(1) + Shade(3) = 6 INT
        caster.Summon(Minions.IronGolem, minionCap: 3);
        caster.Summon(Minions.Skeleton, minionCap: 3);
        caster.Summon(Minions.Shade, minionCap: 3);

        caster.Dismiss(Minions.IronGolem); // remove the FIRST slot
        Assert.Equal(new[] { "skeleton", "shade" }, caster.Minions.Select(m => m.Id));
    }

    [Fact]
    public void ReorderMinionMovesItWithinTheMinionStrip()
    {
        var caster = new Caster(IntBody(10), null);
        caster.Summon(Minions.IronGolem, minionCap: 3);
        caster.Summon(Minions.Skeleton, minionCap: 3);

        Assert.True(caster.ReorderMinion(Minions.IronGolem, 1)); // move golem behind skeleton
        Assert.Equal(new[] { "skeleton", "iron_golem" }, caster.Minions.Select(m => m.Id));
    }

    [Fact]
    public void ReorderMinionFailsForAnUnsummonedMinion()
    {
        var caster = new Caster(IntBody(10), null);
        caster.Summon(Minions.Skeleton, minionCap: 3);
        Assert.False(caster.ReorderMinion(Minions.IronGolem, 0)); // never summoned -> no minion slot to move
    }

    // §6e minion-membership toggle: the MINIONS tab's equip/unequip. The Summoner fields Skeleton+Golem
    // at embark (2 of its 3 slots), leaving one free for a bought minion to fill.
    private static Expedition SummonerExpedition()
    {
        var chassis = CoreRunes.Summoner;
        return Forge.Embark(Races.Human, chassis, chassis.NewLoadout(), chassis.Kit,
            Maps.StandardLeg(autoResolveCastle: false));
    }

    [Fact]
    public void SummonMinionMovesAStashedMinionIntoAFreeSlot()
    {
        // v6: Wand+Charm+Ember+Barkskin leave the Summoner exactly 1 free INT after its kit's
        // Skeleton+Golem -- Shade (3 INT, also retired from Minions.All) no longer fits. Hound
        // gates on DEX instead, which the Summoner never touches, so it proves the free SLOT
        // (MinionCap 3) fills independent of the INT pool being nearly spent.
        var exp = SummonerExpedition();
        Assert.Equal(2, exp.MinionCount); // Skeleton + Golem from the kit
        exp.Stash.AddMinion(Minions.Hound); // as if bought from a merchant

        Assert.True(exp.SummonMinion(Minions.Hound));
        Assert.Contains(Minions.Hound, exp.Minions);
        Assert.Equal(3, exp.MinionCount);
    }

    [Fact]
    public void SummonMinionFailsMidCombat()
    {
        var exp = SummonerExpedition();
        exp.Stash.AddMinion(Minions.Shade);
        exp.Enter("a2"); // State -> Fighting
        Assert.False(exp.SummonMinion(Minions.Shade));
    }

    [Fact]
    public void DismissMinionFreesTheSlotAndLeavesItResummonable()
    {
        var exp = SummonerExpedition();
        Assert.True(exp.DismissMinion(Minions.IronGolem)); // leaves a slot, not removed from any pool
        Assert.Equal(1, exp.MinionCount);
        Assert.DoesNotContain(Minions.IronGolem, exp.Minions);

        Assert.True(exp.SummonMinion(Minions.IronGolem)); // kit membership persists -> re-summon works
        Assert.Equal(2, exp.MinionCount);
    }

    [Fact]
    public void DismissMinionFailsForAMinionNotInAnySlot()
    {
        var exp = SummonerExpedition();
        Assert.False(exp.DismissMinion(Minions.Shade)); // never summoned
    }
}
