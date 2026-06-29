namespace Roguebane.Core.Tests;

// G1: structured foes take LOCALIZED part damage, and a technique can aim a specific foe part.
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

    [Fact]
    public void PartAimErodesTheStatBeforeHp()
    {
        var foe = StructuredFoe("golem", 20);
        var arm = foe.Frame!.Parts[0];
        var caster = new Caster(CasterBody(), foe);

        var smash = Spell("smash", 2);
        caster.Activate(smash);
        caster.Aim(smash, foe, arm);

        caster.Step();

        Assert.Equal(1, foe.Frame!.Contribution(arm)); // 3 - 2
        Assert.Equal(20, foe.Hp);                       // HP untouched while the part holds
    }

    [Fact]
    public void OverkillSpillsIntoHpOnceThePartBottomsOut()
    {
        var foe = StructuredFoe("golem", 20);
        var arm = foe.Frame!.Parts[0]; // capacity 3
        var caster = new Caster(CasterBody(), foe);

        var smash = Spell("smash", 5);
        caster.Activate(smash);
        caster.Aim(smash, foe, arm);

        caster.Step();

        Assert.Equal(0, foe.Frame!.Contribution(arm)); // part gone
        Assert.Equal(18, foe.Hp);                       // 5 - 3 absorbed = 2 overkill to HP
    }

    [Fact]
    public void DownedPartSendsAllSubsequentDamageToHp()
    {
        var foe = StructuredFoe("golem", 20);
        var arm = foe.Frame!.Parts[0];
        var caster = new Caster(CasterBody(), foe);

        var smash = Spell("smash", 3);
        caster.Activate(smash);
        caster.Aim(smash, foe, arm);

        caster.Step(); // part 3 -> 0, no overkill
        Assert.Equal(0, foe.Frame!.Contribution(arm));
        Assert.Equal(20, foe.Hp);

        caster.Step(); // part already gone -> full hit to HP
        Assert.Equal(17, foe.Hp);
    }

    [Fact]
    public void PartAimFallsBackToHpDamageWhenItsFoeDiesAndAimReturnsToTheFront()
    {
        var front = new Foe("front", 100);
        var flank = StructuredFoe("flank", 2);
        var arm = flank.Frame!.Parts[0];
        var caster = new Caster(CasterBody(), front);

        var smash = Spell("smash", 3);
        caster.Activate(smash);
        caster.Aim(smash, flank, arm);

        caster.Step(); // erodes flank's arm 3 -> 0 (no HP), flank still up
        Assert.False(flank.Down);
        Assert.Equal(0, flank.Frame!.Contribution(arm));

        caster.Step(); // arm gone -> 3 spills to flank HP -> flank down
        Assert.True(flank.Down);

        caster.Step(); // flank down -> aim returns to the unstructured front (HP damage)
        Assert.Equal(97, front.Hp);
    }
}
