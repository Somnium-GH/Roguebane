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
    public void ATomeOffhandMultipliesSpellDamage()
    {
        // §6d: Glowing Tome (tier 4) = +0.4x spell damage. Gemstone Wand 6 dmg -> round(6 x 1.4) = 8.
        var body = IntBody();
        Assert.True(body.Wield(Armory.Wands[2]));
        Assert.True(body.Wield(Armory.Tomes[3]));
        Assert.Equal(1.4, body.TomeSpellMult, 3);
        var foe = Shielded(0);
        var c = new Caster(body, foe);
        Assert.True(c.Activate(Zap));
        c.Step();
        Assert.Equal(92, foe.Hp); // 8 landed

        // A broken off-hand arm (hand 1 = armL) silences the tome's bonus.
        var armL = body.Parts.First(p => p.Id == "armL");
        body.Damage(armL, 9);
        Assert.Equal(1.0, body.TomeSpellMult, 3);
    }

    [Fact]
    public void ACharmOffhandMultipliesMinionDamage()
    {
        var body = IntBody();
        Assert.True(body.Wield(Armory.Charms[1])); // Bone Charm, tier 2 -> x1.2
        Assert.Equal(1.2, body.CharmMinionMult, 3);
        body.Unwield(Armory.Charms[1]);
        Assert.Equal(1.0, body.CharmMinionMult, 3);
    }

    [Fact]
    public void ASlingLoosesThroughShieldsForChargeLikeABow()
    {
        // §6d slice 6: the sling rides the bow's exact pierce+Charge path — weaker (placeholder
        // damage 1, §17 #9), one-handed. Shot consults the primary DEX weapon, whichever it is.
        var body = new Body();
        body.Add(new BodyPart("legL", Stat.Dex, 3));
        body.Add(new BodyPart("legR", Stat.Dex, 3));
        body.Add(new BodyPart("armL", Stat.Str, 3));
        body.Add(new BodyPart("armR", Stat.Str, 3));
        body.Add(new BodyPart("head", Stat.Int, 3)); // INT funds the Charge pool
        Assert.True(body.EquipRanged(Armory.Slings[1])); // Braided Sling: reserve 2, power 1

        var foe = Shielded(5);
        var c = new Caster(body, foe, maxCharge: 3);
        var charge0 = c.Charge;
        Assert.True(c.Activate(Armory.Shot));
        for (var i = 0; i < 3; i++) c.Step(); // Shot: timered cd 3

        Assert.Equal(99, foe.Hp);                 // 1 landed THROUGH 5 shield points
        Assert.Equal(5, foe.Frame!.ShieldPoints); // full bypass, pool untouched
        Assert.Equal(charge0 - 1, c.Charge);      // one Charge spent per loose
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
