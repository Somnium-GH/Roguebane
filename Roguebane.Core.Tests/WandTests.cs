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
    public void ATomeOffhandAddsFlatSpellDamage()
    {
        // §6d (WEAPONS.md flat rescale 2026-07-12, was ×0.1/tier): Glowing Tome (tier 4) = +4 FLAT spell
        // damage. Gemstone Wand 6 dmg -> 6 + 4 = 10. INT 10: sustains wand (reserve 6) + tome (4) together.
        var body = IntBody(10);
        Assert.True(body.Wield(Armory.Wands[2]));
        Assert.True(body.Wield(Armory.Tomes[3]));
        Assert.Equal(4, body.SpellImplementBonus);
        var foe = Shielded(0);
        var c = new Caster(body, foe);
        Assert.True(c.Activate(Zap));
        c.Step();
        Assert.Equal(90, foe.Hp); // 10 landed

        // A broken off-hand arm (hand 1 = armL) silences the tome's bonus.
        var armL = body.Parts.First(p => p.Id == "armL");
        body.Damage(armL, 9);
        Assert.Equal(0, body.SpellImplementBonus);
    }

    [Fact]
    public void SpellBonusIsFlatPerTier_T3EqualsPlus3_AndStaffMatchesTomeWithoutStacking()
    {
        // "T3 = +3" LOCKED (2026-07-12, Doug): a T3-owned spell implement grants exactly +3, flat/
        // additive. A 2H staff carries the SAME +1/tier bonus as a tome, read off the wielded staff
        // directly; the two never STACK (the effective bonus is the MAX tier across usable sources).
        Assert.Equal(3, WithSpellImplement(Armory.Tomes[2]));    // Ornate Tome, tier 3 -> +3
        Assert.Equal(3, WithSpellImplement(Armory.Staffs[2]));   // Ornate Staff, tier 3 -> +3 (its OWN bonus)
        Assert.Equal(4, WithSpellImplement(Armory.Tomes[3]));    // T4 owned -> +4
    }

    private static int WithSpellImplement(Weapon w)
    {
        var body = IntBody(12);
        Assert.True(body.Wield(w));
        return body.SpellImplementBonus;
    }

    [Fact]
    public void ACharmOffhandAddsFlatMinionDamage()
    {
        var body = IntBody();
        Assert.True(body.Wield(Armory.Charms[1])); // Bone Charm, tier 2 -> +2 flat
        Assert.Equal(2, body.CharmMinionBonus);
        body.Unwield(Armory.Charms[1]);
        Assert.Equal(0, body.CharmMinionBonus);
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
        for (var i = 0; i < 70; i++) c.Step(); // Shot: cd 80, 12% haste (6 DEX) -> 70

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
