using Roguebane.Core.Content;

namespace Roguebane.Core.Tests;

// §6b shields wired onto the body: a shield SOURCE (Barkskin) raises a regenerating pool that absorbs
// incoming hits before HP/parts, sheds when its stat is smashed, and stays opt-in content.
public class ShieldWiringTests
{
    private static Body Shielded()
    {
        var body = new Body();
        body.Add(new BodyPart("head", Stat.Int, 4));  // powers Barkskin (INT 1 reserve)
        body.Add(new BodyPart("chest", Stat.Con, 4));
        return body;
    }

    [Fact]
    public void RaisingAShieldSourcePutsLayersOnTheBody()
    {
        var body = Shielded();
        new Caster(body).Activate(Techniques.Barkskin);
        Assert.Equal(3, body.ShieldPoints); // 3 stone layers
    }

    [Fact]
    public void ShieldsAbsorbHitsBeforeHpThenSpill()
    {
        var body = Shielded();
        new Caster(body).Activate(Techniques.Barkskin);
        var defender = new Fighter(body, maxHp: 20);

        var atkBody = new Body();
        atkBody.Add(new BodyPart("arm", Stat.Str, 4));
        var strike = new Technique("strike", Stat.Str, 1, TechniqueKind.Sustained, 0, Power: 2);
        var attacker = new Caster(atkBody, defender);
        attacker.Activate(strike);

        attacker.Step();                       // power 2 -> 3 layers absorb 2
        Assert.Equal(20, defender.Hp);          // nothing reaches HP
        Assert.Equal(1, body.ShieldPoints);

        attacker.Step();                       // power 2 -> 1 layer absorbs 1, 1 spills
        Assert.Equal(0, body.ShieldPoints);
        Assert.Equal(19, defender.Hp);          // the spill lands
    }

    [Fact]
    public void LayersRegenerateOnTheOwnersTick()
    {
        var body = Shielded();
        var caster = new Caster(body);
        caster.Activate(Techniques.Barkskin);
        body.AbsorbShields(3); // drain
        Assert.Equal(0, body.ShieldPoints);

        for (var i = 0; i < 200; i++) caster.Step(); // its own caster regenerates the layers
        Assert.Equal(3, body.ShieldPoints);           // back to full, capped at the layer count
    }

    [Fact]
    public void SmashingTheSourceStatShedsTheShield()
    {
        var body = new Body();
        body.Add(new BodyPart("head", Stat.Int, 1)); // exactly Barkskin's reserve
        body.Add(new BodyPart("chest", Stat.Con, 4));
        var caster = new Caster(body);
        caster.Activate(Techniques.Barkskin);
        Assert.Equal(3, body.ShieldPoints);

        body.Damage(body.Parts[0], 1); // INT 1 -> 0, below the reserve
        caster.Step();                  // prune the silenced source + shed its shield
        Assert.Equal(0, body.ShieldPoints);
    }

    [Fact]
    public void BarkskinIsAShieldSourceAndNotInTheDefaultPalette()
    {
        Assert.True(Techniques.Barkskin.ShieldLayers > 0);
        Assert.DoesNotContain(Techniques.Barkskin, Techniques.All);
    }
}
