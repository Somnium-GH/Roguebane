namespace Roguebane.Core.Tests;

// §8 [LOCKED]: a part-aimed hit erodes the targeted part's stat AND deals HP damage SIMULTANEOUSLY --
// same power, from the one hit. No part-vs-HP split, no HP-only-on-overkill path.
public class PartAimTests
{
    private static Body CasterBody()
    {
        var body = new Body();
        body.Add(new BodyPart("head", Stat.Int, 12));
        return body;
    }

    private static Technique Spell(string id, int power) =>
        new(id, Stat.Int, 1, TechniqueKind.Sustained, Cooldown: 0, Power: power);

    private static Foe StructuredFoe(string id, int hp)
    {
        var frame = new Body();
        frame.Add(new BodyPart($"{id}-arm", Stat.Str, 3));
        return new Foe(id, hp, frame);
    }

    private static (Caster, Foe, BodyPart) Setup(int foeHp, int power)
    {
        var foe = StructuredFoe("golem", foeHp);
        var arm = foe.Frame!.Parts[0];
        var caster = new Caster(CasterBody(), foe);
        var smash = Spell("smash", power);
        caster.Activate(smash);
        caster.Aim(smash, foe, arm);
        return (caster, foe, arm);
    }

    [Fact]
    public void APartHitErodesTheStatAndHpTogether()
    {
        var (caster, foe, arm) = Setup(foeHp: 20, power: 2);
        caster.Step();
        Assert.Equal(1, foe.Frame!.Contribution(arm)); // 3 - 2 stat
        Assert.Equal(18, foe.Hp);                       // and 2 HP, same hit
    }

    [Fact]
    public void TheStatFloorsAtZeroWhileHpTakesTheFullHit()
    {
        var (caster, foe, arm) = Setup(foeHp: 20, power: 5); // arm capacity 3
        caster.Step();
        Assert.Equal(0, foe.Frame!.Contribution(arm)); // floored, NOT spilled
        Assert.Equal(15, foe.Hp);                       // full 5 to HP (no overkill maths)
    }

    [Fact]
    public void EveryHitTakesHpEvenAfterThePartIsGone()
    {
        var (caster, foe, arm) = Setup(foeHp: 20, power: 3);
        caster.Step(); // arm 3 -> 0, HP 20 -> 17
        Assert.Equal(0, foe.Frame!.Contribution(arm));
        Assert.Equal(17, foe.Hp);

        caster.Step(); // arm stays 0, HP keeps dropping
        Assert.Equal(14, foe.Hp);
    }
}
