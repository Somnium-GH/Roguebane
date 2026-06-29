using Roguebane.Core.Content;

namespace Roguebane.Core.Tests;

// Mitigation on the incoming-hit path: leather EVASION can dodge a hit (seeded), and a held CON
// block blunts a whole-HP hit. Both are felt against an attacking Caster.
public class MitigationTests
{
    private static Body Humanoid()
    {
        var b = new Body();
        b.Add(new BodyPart("arm-l", Stat.Str, 4));
        b.Add(new BodyPart("arm-r", Stat.Str, 4));
        b.Add(new BodyPart("leg-l", Stat.Dex, 3));
        b.Add(new BodyPart("leg-r", Stat.Dex, 3));
        b.Add(new BodyPart("head", Stat.Int, 4));
        b.Add(new BodyPart("chest", Stat.Con, 6));
        return b;
    }

    private static (Caster attacker, Fighter target) Duel(Body defender)
    {
        var target = new Fighter(defender, 50);
        var attacker = new Caster(Humanoid(), target);
        attacker.UseRng(new Rng(42));
        attacker.Activate(Techniques.Jab); // Str hit on the HP pool (no part aim)
        return (attacker, target);
    }

    private static int DamageOver(Caster attacker, Fighter target, int steps)
    {
        for (var i = 0; i < steps; i++) attacker.Step();
        return target.MaxHp - target.Hp;
    }

    [Fact]
    public void LeatherLegsCanFullyDodgeWholeHpHits()
    {
        var body = Humanoid();
        body.Equip(new Armor("dodge", Stat.Dex, ArmorKind.Leather, 100)); // certain dodge
        var (attacker, target) = Duel(body);

        Assert.Equal(0, DamageOver(attacker, target, 40));
    }

    [Fact]
    public void WithoutLeatherTheHitsLand()
    {
        var (attacker, target) = Duel(Humanoid());
        Assert.True(DamageOver(attacker, target, 40) > 0);
    }

    [Fact]
    public void AHeldConBlockBluntsTheHit()
    {
        var body = Humanoid();
        body.Activate(new Active("block", Stat.Con, 2)); // reserve 2 CON -> blocks 2 off each HP hit
        var (attacker, target) = Duel(body);

        // Jab is power 2; block 2 => every hit fully absorbed.
        Assert.Equal(0, DamageOver(attacker, target, 40));
    }

    [Fact]
    public void EvasionRollsAreDeterministicForAGivenSeed()
    {
        int Run()
        {
            var body = Humanoid();
            body.Equip(new Armor("dodge", Stat.Dex, ArmorKind.Leather, 50)); // half dodge
            var (attacker, target) = Duel(body);
            return DamageOver(attacker, target, 60);
        }

        Assert.Equal(Run(), Run()); // same seed => same damage taken
    }
}
