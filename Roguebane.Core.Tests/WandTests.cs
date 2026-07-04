using Roguebane.Core.Content;

namespace Roguebane.Core.Tests;

// §10 wand redefinition (2026-07-03, resolves §17 #18): a cast consulting WANDS deals its damage
// MINUS the foe's standing shield-point count — the pool is NOT consumed, no Charge is spent.
// What lands is a normal part+HP hit (§8). Economy math asserted, not assumed.
public class WandTests
{
    private static Body IntBody(int intCap = 8)
    {
        var b = new Body();
        b.Add(new BodyPart("head", Stat.Int, intCap));
        b.Add(new BodyPart("armL", Stat.Str, 3));
        b.Add(new BodyPart("armR", Stat.Str, 3));
        return b;
    }

    // A wand verb: consults the primary INT weapon (the wand lends its damage), fires each tick.
    private static readonly Technique Zap =
        new("zap", Stat.Int, 0, TechniqueKind.Sustained, 0, Power: 0, Consults: WeaponUse.Primary);

    private static Foe Shielded(int shields)
    {
        var frame = new Body();
        frame.Add(new BodyPart("chest", Stat.Con, 4));
        if (shields > 0) frame.RaiseShield("test", shields, regenEvery: 1_000_000);
        return new Foe("t", 100, frame);
    }

    [Fact]
    public void WandDamageIsBluntedByStandingShieldsWithoutConsumingThem()
    {
        // The spec's own example: tier-3 wand (6 dmg) vs 4 shield points -> 2 land, pool stays 4.
        var body = IntBody();
        Assert.True(body.Wield(Armory.Wands[2])); // Gemstone Wand: 6 dmg, 6 INT
        var foe = Shielded(4);
        var c = new Caster(body, foe);
        Assert.True(c.Activate(Zap));
        c.Step();

        Assert.Equal(98, foe.Hp);                    // 6 - 4 = 2 landed
        Assert.Equal(4, foe.Frame!.ShieldPoints);    // the pool was NOT consumed
    }

    [Fact]
    public void ABigEnoughStackBluntsTheWandCompletely()
    {
        var body = IntBody();
        Assert.True(body.Wield(Armory.Wands[0])); // Adept Wand: 2 dmg
        var foe = Shielded(5);
        var c = new Caster(body, foe);
        Assert.True(c.Activate(Zap));
        c.Step();

        Assert.Equal(100, foe.Hp);                // 2 - 5 -> nothing lands
        Assert.Equal(5, foe.Frame!.ShieldPoints); // and nothing is consumed
    }

    [Fact]
    public void OrdinaryHitsStillConsumeThePool()
    {
        // Contrast: a melee consult eats shield points (AbsorbShields), it doesn't subtract.
        var body = IntBody();
        Assert.True(body.Wield(Armory.Sword)); // Iron Longsword: 4 dmg (STR consult)
        var foe = Shielded(3);
        var c = new Caster(body, foe);
        Assert.True(c.Activate(new Technique("swing", Stat.Str, 0, TechniqueKind.Sustained, 0,
            Power: 0, Consults: WeaponUse.Primary)));
        c.Step();

        Assert.Equal(99, foe.Hp);                 // 4 - 3 absorbed = 1 lands
        Assert.Equal(0, foe.Frame!.ShieldPoints); // the pool was CONSUMED
    }
}
