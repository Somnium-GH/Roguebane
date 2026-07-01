namespace Roguebane.Core.Tests;

// CHARGE = the shield-pierce resource (§6b/§10): ONLY a shield-piercing technique draws it per
// discharge, its damage bypasses the shield pool, and when charge is dry it holds fire but keeps its
// reservation; charge refills out of combat. Non-piercing techniques ignore charge entirely.
public class ChargeTests
{
    private static Body IntBody(int intel)
    {
        var body = new Body();
        body.Add(new BodyPart("head", Stat.Int, intel));
        return body;
    }

    // A shield-piercing spell that spends `chargeCost` charge per discharge.
    private static Technique Piercer(int power, int chargeCost) =>
        new("pierce", Stat.Int, 1, TechniqueKind.Sustained, Cooldown: 0, Power: power,
            ChargeCost: chargeCost, ShieldPiercing: true);

    [Fact]
    public void APiercingTechniqueSpendsChargePerTick()
    {
        var foe = new Foe("front", 100);
        var caster = new Caster(IntBody(9), foe, maxCharge: 3);
        caster.Activate(Piercer(2, chargeCost: 1));

        caster.Step();
        Assert.Equal(2, caster.Charge); // 3 -> 2
        Assert.Equal(98, foe.Hp);
    }

    [Fact]
    public void WhenChargeIsDryThePierceHoldsButStaysActive()
    {
        var foe = new Foe("front", 100);
        var spell = Piercer(2, chargeCost: 1);
        var caster = new Caster(IntBody(9), foe, maxCharge: 1);
        caster.Activate(spell);

        caster.Step(); // spends the last charge, fires
        caster.Step(); // dry: holds fire
        Assert.Equal(0, caster.Charge);
        Assert.Equal(98, foe.Hp);            // only one hit landed
        Assert.True(caster.IsActive(spell)); // reservation kept
    }

    [Fact]
    public void RechargeRefillsThePool()
    {
        var foe = new Foe("front", 100);
        var spell = Piercer(2, chargeCost: 1);
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
    public void NonPiercingTechniquesIgnoreChargeEntirely()
    {
        var foe = new Foe("front", 100);
        var caster = new Caster(IntBody(9), foe, maxCharge: 0); // no charge available
        // A plain (non-piercing) technique never draws charge, even at maxCharge 0.
        caster.Activate(new Technique("plain", Stat.Int, 1, TechniqueKind.Sustained, 0, Power: 2));

        caster.Step();
        Assert.Equal(98, foe.Hp);
    }

    [Fact]
    public void ShieldPiercingBypassesTheShieldPool()
    {
        // A shielded defender; a piercing hit ignores the layers, a non-piercing hit is absorbed.
        var defBody = new Body();
        defBody.Add(new BodyPart("head", Stat.Int, 4));
        defBody.Add(new BodyPart("chest", Stat.Con, 6));
        var defender = new Fighter(defBody, maxHp: 20);
        new Caster(defBody).Activate(Content.Techniques.Stoneskin); // raise a 3-layer shield
        Assert.Equal(3, defBody.ShieldPoints);

        var atk = new Caster(IntBody(9), defender, maxCharge: 5);
        atk.Activate(Piercer(2, chargeCost: 1)); // shield-piercing
        atk.Step();

        Assert.Equal(3, defBody.ShieldPoints); // pool untouched
        Assert.Equal(18, defender.Hp);          // the 2 landed straight on HP
    }
}
