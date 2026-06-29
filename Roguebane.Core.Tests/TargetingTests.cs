namespace Roguebane.Core.Tests;

public class TargetingTests
{
    private static Body CasterBody()
    {
        var body = new Body();
        body.Add(new BodyPart("head", Stat.Int, 12));
        return body;
    }

    private static Technique Spell(string id, int power) =>
        new(id, Stat.Int, 1, TechniqueKind.Sustained, Cooldown: 0, Power: power);

    [Fact]
    public void AimDirectsATechniqueAtItsOwnFoe()
    {
        var front = new Foe("front", 100);
        var flank = new Foe("flank", 100);
        var caster = new Caster(CasterBody(), front);

        var jab = Spell("jab", 2);
        var hex = Spell("hex", 3);
        caster.Activate(jab);
        caster.Activate(hex);
        caster.Aim(hex, flank); // jab follows the front, hex hits the flank

        caster.Step();

        Assert.Equal(98, front.Hp); // jab
        Assert.Equal(97, flank.Hp); // hex
    }

    [Fact]
    public void UnaimedTechniqueFollowsTheDefaultFront()
    {
        var front = new Foe("front", 100);
        var caster = new Caster(CasterBody(), front);
        caster.Activate(Spell("jab", 4));

        caster.Step();
        Assert.Equal(96, front.Hp);
    }

    [Fact]
    public void AimFallsBackToTheFrontWhenItsFoeDies()
    {
        var front = new Foe("front", 100);
        var flank = new Foe("flank", 2);
        var caster = new Caster(CasterBody(), front);

        var hex = Spell("hex", 2);
        caster.Activate(hex);
        caster.Aim(hex, flank);

        caster.Step(); // flank 2 -> 0
        Assert.True(flank.Down);
        caster.Step(); // flank down, hex falls back to the front
        Assert.Equal(98, front.Hp);
    }
}
