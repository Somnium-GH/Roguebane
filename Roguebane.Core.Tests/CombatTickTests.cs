namespace Roguebane.Core.Tests;

public class CombatTickTests
{
    private static BodyPart _head = null!;

    private static Body Body(int str = 12, int intel = 12, int dex = 12, int con = 12)
    {
        _head = new BodyPart("head", Stat.Int, intel);
        var body = new Body();
        body.Add(new BodyPart("arms", Stat.Str, str));
        body.Add(new BodyPart("legs", Stat.Dex, dex));
        body.Add(_head);
        body.Add(new BodyPart("chest", Stat.Con, con));
        return body;
    }

    private static Technique Spell(string id, int reserve, int power) =>
        new(id, Stat.Int, reserve, TechniqueKind.Sustained, Cooldown: 0, Power: power);

    [Fact]
    public void TimeredFiresOnCooldown()
    {
        var foe = new Foe("dummy", 1000);
        var caster = new Caster(Body(), foe);
        caster.Activate(new Technique("jab", Stat.Str, 1, TechniqueKind.Timered, Cooldown: 2, Power: 3));

        for (var i = 0; i < 4; i++) caster.Step();

        Assert.Equal(1000 - 2 * 3, foe.Hp); // fires at tick 2 and 4
    }

    [Fact]
    public void SustainedOutputsEveryTick()
    {
        var foe = new Foe("dummy", 1000);
        var caster = new Caster(Body(), foe);
        caster.Activate(Spell("drain", reserve: 1, power: 2));

        for (var i = 0; i < 3; i++) caster.Step();

        Assert.Equal(1000 - 3 * 2, foe.Hp);
    }

    [Fact]
    public void ParallelByAllocation_SecondTechniqueBlockedWhenStatExhausted()
    {
        var caster = new Caster(Body(intel: 2), new Foe("dummy", 1000));

        Assert.True(caster.Activate(Spell("drain", reserve: 2, power: 2)));
        Assert.False(caster.Activate(Spell("ember", reserve: 1, power: 1))); // INT 2 fully reserved
        Assert.Equal(1, caster.ActiveCount);
    }

    [Fact]
    public void DeactivatingReturnsTheStatForAnother()
    {
        var caster = new Caster(Body(intel: 2), new Foe("dummy", 1000));
        var drain = Spell("drain", reserve: 2, power: 2);

        caster.Activate(drain);
        caster.Deactivate(drain);
        Assert.True(caster.Activate(Spell("ember", reserve: 1, power: 1)));
    }

    [Fact]
    public void SmashingTheHeadSilencesSpells()
    {
        var body = Body(intel: 4);
        var caster = new Caster(body, new Foe("dummy", 1000));
        caster.Activate(Spell("drain", reserve: 3, power: 2));

        body.Damage(_head, 4); // INT 4 -> 0, the spell reservation cascades off

        caster.Step();
        Assert.Equal(0, caster.ActiveCount); // silenced
    }

    [Fact]
    public void StrMovesSurviveAHeadSmash()
    {
        var body = Body(intel: 4);
        var caster = new Caster(body, new Foe("dummy", 1000));
        var jab = new Technique("jab", Stat.Str, 1, TechniqueKind.Timered, Cooldown: 1, Power: 3);
        caster.Activate(jab);

        body.Damage(_head, 99); // head gone, but arms still swing

        caster.Step();
        Assert.True(caster.IsActive(jab));
    }

    [Fact]
    public void SimulationIsDeterministic_SameInputsSameOutcome()
    {
        static int Run()
        {
            var foe = new Foe("dummy", 100000);
            var caster = new Caster(Body(), foe);
            caster.Activate(new Technique("jab", Stat.Str, 1, TechniqueKind.Timered, 2, 3));
            caster.Activate(new Technique("cleave", Stat.Str, 3, TechniqueKind.Timered, 4, 4));
            caster.Activate(Spell("drain", reserve: 2, power: 2));
            for (var i = 0; i < 50; i++) caster.Step();
            return 100000 - foe.Hp;
        }

        Assert.Equal(Run(), Run());
    }
}
