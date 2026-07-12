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
    public void ShieldsFullyBlockEachHitThenLandUnmitigatedOnceDepleted()
    {
        // §6b mitigation rule CHANGE [LOCKED 2026-07-12, Doug]: any standing point FULLY blocks a normal
        // hit — no bleed-through even when power exceeds the points. Points still deplete (up to what
        // stands); once the pool hits 0, the next hit lands completely unmitigated. (Was: absorb what
        // fits, spill the remainder.)
        var body = Shielded();
        new Caster(body).Activate(Techniques.Barkskin);
        var defender = new Fighter(body, maxHp: 20);

        var atkBody = new Body();
        atkBody.Add(new BodyPart("arm", Stat.Str, 4));
        var strike = new Technique("strike", Stat.Str, 1, TechniqueKind.Sustained, 0, Power: 2);
        var attacker = new Caster(atkBody, defender);
        attacker.Activate(strike);

        attacker.Step();                       // power 2 vs 3 points -> fully blocked, 2 points spent
        Assert.Equal(20, defender.Hp);          // nothing reaches HP
        Assert.Equal(1, body.ShieldPoints);

        attacker.Step();                       // power 2 vs 1 point -> STILL fully blocked, NO spill
        Assert.Equal(0, body.ShieldPoints);
        Assert.Equal(20, defender.Hp);          // was 19 under the old spillover rule

        attacker.Step();                       // shield now at 0 -> the hit lands unmitigated
        Assert.Equal(18, defender.Hp);          // full power 2
    }

    [Fact]
    public void ShieldMitigation_3PowerVs2Points_BlocksFullyThenLandsUnmitigated()
    {
        // Doug's LOCKED worked example verbatim (2026-07-12): 3 power vs 2 standing points.
        var body = Shielded();
        body.RaiseShield("test", 2, regenEvery: 1_000_000);
        var defender = new Fighter(body, maxHp: 20);
        var atk = new Body();
        atk.Add(new BodyPart("arm", Stat.Str, 4));
        var strike = new Technique("s3", Stat.Str, 1, TechniqueKind.Sustained, 0, Power: 3);
        var attacker = new Caster(atk, defender);
        attacker.Activate(strike);

        attacker.Step();                       // 3 vs 2 -> shield drops to 0, ZERO lands (rule #1)
        Assert.Equal(20, defender.Hp);
        Assert.Equal(0, body.ShieldPoints);

        attacker.Step();                       // shield at 0 -> full 3 lands (rule #2)
        Assert.Equal(17, defender.Hp);
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
