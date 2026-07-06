using System.Linq;
using Roguebane.Core.Content;

namespace Roguebane.Core.Tests;

// Sacrifice (TECHNIQUES.md, LOCKED 2026-07-05): consumes a fielded minion to mend the caster's own
// most-damaged part. Heal formula APPROVED as the standing placeholder (Doug, 2026-07-05 -
// "placeholder for now"): 4x the consumed minion's Reserve (T1 -> 4, Iron Golem T2 -> 8).
public class SacrificeHealTests
{
    private static Body Wounded(int capacity, int damage, out BodyPart arm)
    {
        arm = new BodyPart("arm", Stat.Str, capacity);
        var body = new Body();
        body.Add(arm);
        body.Add(new BodyPart("head", Stat.Int, 20));
        body.Damage(arm, damage);
        return body;
    }

    // Sacrifice is Timered (Cooldown 80): it charges like any other Timered technique before its
    // first discharge, same as a minion's own Timer (MinionTests) -- never fires on activation tick.
    private static void RunOutCooldown(Caster caster)
    {
        for (var i = 0; i < Techniques.Sacrifice.Cooldown; i++) caster.Step();
    }

    [Fact]
    public void HealsFourTimesTheConsumedMinionsReserveAndDismissesIt()
    {
        var body = Wounded(capacity: 10, damage: 6, out var arm); // contribution 4
        var caster = new Caster(body, null);
        caster.Summon(Minions.Skeleton, minionCap: 3); // Reserve 1 -> heal 4

        caster.Activate(Techniques.Sacrifice);
        RunOutCooldown(caster);

        Assert.Equal(0, caster.MinionCount); // consumed, not just damaged
        Assert.Equal(8, body.Contribution(arm)); // 4 + (4 x Reserve 1)
    }

    [Fact]
    public void ConsumesTheHighestReserveFieldedMinionFirst()
    {
        var body = Wounded(capacity: 20, damage: 15, out var arm); // contribution 5
        var caster = new Caster(body, null);
        caster.Summon(Minions.Skeleton, minionCap: 3); // Reserve 1
        caster.Summon(Minions.IronGolem, minionCap: 3); // Reserve 2 -> heal 8, consumed first

        caster.Activate(Techniques.Sacrifice);
        RunOutCooldown(caster);

        Assert.Equal(1, caster.MinionCount);
        Assert.Equal("skeleton", caster.Minions.Single().Id);
        Assert.Equal(13, body.Contribution(arm)); // 5 + (4 x Reserve 2)
    }

    [Fact]
    public void HoldsFireWithNoMinionFielded()
    {
        var body = Wounded(capacity: 10, damage: 6, out var arm);
        var caster = new Caster(body, null);

        caster.Activate(Techniques.Sacrifice);
        RunOutCooldown(caster);

        Assert.Equal(4, body.Contribution(arm)); // untouched - no minion to consume
    }
}
