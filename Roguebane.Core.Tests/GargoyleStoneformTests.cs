namespace Roguebane.Core.Tests;

// CHUNK D item 2's third Foe Effect (FOES.md, Gargoyle): the inverse of Insubstantial -- while its
// CON (chest) part stands whole, every landed hit deals 1 less PART damage (min 1) to whatever part
// it's aimed at; HP still lands full. Proven via a bare fixture, not Foes.Gargoyle (that foe's
// "stone fists ~ Iron Axe profile" gear-authoring hasn't been checked yet, same open question the
// Skeleton Jab/Dagger conflict raised -- this proof only needs the effect's own vocabulary).
public class GargoyleStoneformTests
{
    private static Body CasterBody()
    {
        var body = new Body();
        body.Add(new BodyPart("arm", Stat.Str, 12));
        return body;
    }

    private static Technique Smash(int power) =>
        new("smash", Stat.Str, 1, TechniqueKind.Sustained, Cooldown: 0, Power: power);

    private static Caster Aimed(Foe foe, BodyPart part, int power)
    {
        var caster = new Caster(CasterBody(), foe);
        var tech = Smash(power);
        caster.Activate(tech);
        caster.Aim(tech, foe, part);
        return caster;
    }

    private static Foe StoneformFoe(out BodyPart legs, out BodyPart chest)
    {
        var frame = new Body();
        frame.Add(new BodyPart("arm", Stat.Str, 3));
        frame.Add(new BodyPart("head", Stat.Int, 2));
        legs = new BodyPart("legs", Stat.Dex, 5);
        chest = new BodyPart("chest", Stat.Con, 4);
        frame.Add(legs);
        frame.Add(chest);
        return new Foe("gargoyle-test", 12, frame, effect: FoeEffectKind.Stoneform);
    }

    [Fact]
    public void AHitElsewhereLandsOneLessPartDamageWhileTheChestStandsWhole()
    {
        var foe = StoneformFoe(out var legs, out _);
        var caster = Aimed(foe, legs, power: 3);

        caster.Step();

        Assert.Equal(5 - 2, foe.Frame!.Contribution(legs)); // power 3, discounted to 2
        Assert.Equal(12 - 3, foe.Hp); // HP lands full power regardless
    }

    [Fact]
    public void TheHitThatDamagesTheChestItselfIsStillDiscounted()
    {
        var foe = StoneformFoe(out _, out var chest);
        var caster = Aimed(foe, chest, power: 3);

        caster.Step();

        Assert.Equal(4 - 2, foe.Frame!.Contribution(chest)); // gated on standing whole GOING IN
        Assert.Equal(12 - 3, foe.Hp);
    }

    [Fact]
    public void OnceTheChestIsDamagedLaterHitsElsewhereLandFullPartDamage()
    {
        var foe = StoneformFoe(out var legs, out var chest);
        foe.Frame!.Damage(chest, 4); // break Stoneform before the fight starts (chest capacity 4)

        var caster = Aimed(foe, legs, power: 3);
        caster.Step();

        Assert.Equal(5 - 3, foe.Frame!.Contribution(legs)); // full power now -- the effect is gone for good
    }

    [Fact]
    public void APowerOneHitStillDealsOnePartDamageNotZero()
    {
        var foe = StoneformFoe(out var legs, out _);
        var caster = Aimed(foe, legs, power: 1);

        caster.Step();

        Assert.Equal(5 - 1, foe.Frame!.Contribution(legs)); // min-1 floor, not fully negated
    }
}
