using Roguebane.Core.Content;

namespace Roguebane.Core.Tests;

// Mitigation on the incoming-hit path (§8: shields + full evade are the ONLY mitigations). Leather
// EVASION can fully dodge a hit (seeded); a shield source absorbs hits before they land.
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
    public void StackedLeatherEvadesSomeHits()
    {
        // §6c: evade stacks per worn sustained piece (full tier-4 set = 4 x 8% = 32%) — a leather
        // build takes strictly less damage than a bare one over the same seeded exchange.
        var body = Humanoid();
        foreach (var l in new[] { Content.ArmorLines.LeatherHead, Content.ArmorLines.LeatherChest,
            Content.ArmorLines.LeatherArms, Content.ArmorLines.LeatherLegs })
            body.Equip(l[3]); // full tier-4 leather: 4 x 8% = 32% evade (§6c stacking)
        Assert.Equal(32, body.EvasionPercent());
        var (attacker, target) = Duel(body);
        var leather = DamageOver(attacker, target, 300);

        var (atkB, tgtB) = Duel(Humanoid());
        var bare = DamageOver(atkB, tgtB, 300);
        Assert.True(leather < bare, $"evade should reduce damage taken ({leather} vs {bare})");
    }

    [Fact]
    public void WithoutLeatherTheHitsLand()
    {
        var (attacker, target) = Duel(Humanoid());
        Assert.True(DamageOver(attacker, target, 300) > 0);
    }

    [Fact]
    public void AShieldSourceAbsorbsHitsBeforeHp()
    {
        // §8: the CON flat block is gone; a shield source is the mitigation. Its layers eat Jab hits
        // (the defender's caster isn't ticked here, so the layers drain and don't regen) -> the shielded
        // build takes strictly less than a bare one.
        var shieldedBody = Humanoid();
        new Caster(shieldedBody).Activate(Techniques.Stoneskin); // INT-powered 3-layer shield
        var (atkS, tgtS) = Duel(shieldedBody);
        var shielded = DamageOver(atkS, tgtS, 300);

        var (atkB, tgtB) = Duel(Humanoid());
        var bare = DamageOver(atkB, tgtB, 300);

        Assert.True(shielded < bare, $"shield should reduce damage taken ({shielded} vs {bare})");
    }

    [Fact]
    public void EvasionRollsAreDeterministicForAGivenSeed()
    {
        int Run()
        {
            var body = Humanoid();
            body.Equip(Content.ArmorLines.LeatherLegs[3]); // 8% evade, seeded RNG
            var (attacker, target) = Duel(body);
            return DamageOver(attacker, target, 300);
        }

        Assert.Equal(Run(), Run()); // same seed => same damage taken
    }
}
