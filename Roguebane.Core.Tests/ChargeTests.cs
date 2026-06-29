namespace Roguebane.Core.Tests;

// G7 (magic/charge): magic-tier techniques draw a finite resource per discharge; when it is dry they
// hold fire but keep their reservation, and they refill out of combat.
public class ChargeTests
{
    private static Body IntBody(int intel)
    {
        var body = new Body();
        body.Add(new BodyPart("head", Stat.Int, intel));
        return body;
    }

    private static Technique Spell(int power, int chargeCost) =>
        new("spell", Stat.Int, 1, TechniqueKind.Sustained, Cooldown: 0, Power: power, ChargeCost: chargeCost);

    [Fact]
    public void AMagicTechniqueSpendsChargePerTick()
    {
        var foe = new Foe("front", 100);
        var caster = new Caster(IntBody(9), foe, maxCharge: 3);
        caster.Activate(Spell(2, chargeCost: 1));

        caster.Step();
        Assert.Equal(2, caster.Charge); // 3 -> 2
        Assert.Equal(98, foe.Hp);
    }

    [Fact]
    public void WhenChargeIsDryTheSpellHoldsFireButStaysActive()
    {
        var foe = new Foe("front", 100);
        var spell = Spell(2, chargeCost: 1);
        var caster = new Caster(IntBody(9), foe, maxCharge: 1);
        caster.Activate(spell);

        caster.Step(); // spends the last charge, fires
        caster.Step(); // dry: holds fire
        Assert.Equal(0, caster.Charge);
        Assert.Equal(98, foe.Hp);          // only one hit landed
        Assert.True(caster.IsActive(spell)); // reservation kept
    }

    [Fact]
    public void RechargeRefillsThePool()
    {
        var foe = new Foe("front", 100);
        var spell = Spell(2, chargeCost: 1);
        var caster = new Caster(IntBody(9), foe, maxCharge: 2);
        caster.Activate(spell);

        caster.Step(); caster.Step(); // drains to 0
        Assert.Equal(0, caster.Charge);

        caster.Recharge();
        Assert.Equal(2, caster.Charge);
        caster.Step();
        Assert.Equal(94, foe.Hp); // 100 - 2 - 2 - 2
    }

    [Fact]
    public void NonMagicTechniquesIgnoreChargeEntirely()
    {
        var foe = new Foe("front", 100);
        var caster = new Caster(IntBody(9), foe, maxCharge: 0); // no magic economy
        caster.Activate(Spell(2, chargeCost: 0));               // free technique

        caster.Step();
        Assert.Equal(98, foe.Hp);
    }
}
