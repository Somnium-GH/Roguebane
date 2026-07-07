using Roguebane.Core.Content;

namespace Roguebane.Core.Tests;

// CHUNK D item 1's last piece (STATUS.md): the first real Foe Effect (FOES.md's design rules),
// proven through the shared Caster.Hit path -- Insubstantial reads live part damage the same way
// ArmorSustained/DisabledGear already do, so it's one more consult site, not a foe-only interpreter.
public class WraithInsubstantialTests
{
    private static Body CasterBody()
    {
        var body = new Body();
        body.Add(new BodyPart("head", Stat.Int, 12));
        return body;
    }

    private static Technique Spell(int power) =>
        new("smash", Stat.Int, 1, TechniqueKind.Sustained, Cooldown: 0, Power: power);

    private static Caster Aimed(Foe foe, BodyPart part, int power)
    {
        var caster = new Caster(CasterBody(), foe);
        var spell = Spell(power);
        caster.Activate(spell);
        caster.Aim(spell, foe, part);
        return caster;
    }

    [Fact]
    public void AHitElsewhereLandsOneLessHpWhileTheHeadStandsWhole()
    {
        var foe = Foes.Wraith("wraith"); // parts: [0] arm [1] head (Int 4) [2] legs [3] chest
        var chest = foe.Frame!.Parts[3];
        var caster = Aimed(foe, chest, power: 3);

        caster.Step();

        Assert.Equal(10 - 2, foe.Hp); // power 3, reduced to 2 -- the head hasn't been touched
    }

    [Fact]
    public void TheHitThatDamagesTheHeadItselfStillLandsFullHp()
    {
        var foe = Foes.Wraith("wraith");
        var head = foe.Frame!.Parts[1];
        var caster = Aimed(foe, head, power: 2);

        caster.Step();

        Assert.Equal(10 - 2, foe.Hp); // this hit is what breaks Insubstantial, so it lands in full
        Assert.Equal(2, foe.Frame!.Contribution(head)); // headInt 4 - 2
    }

    [Fact]
    public void OnceTheHeadIsDamagedLaterHitsElsewhereLandFullHpToo()
    {
        var foe = Foes.Wraith("wraith");
        var head = foe.Frame!.Parts[1];
        var chest = foe.Frame!.Parts[3];
        foe.Frame!.Damage(head, 1); // break Insubstantial before the fight starts

        var caster = Aimed(foe, chest, power: 3);
        caster.Step();

        Assert.Equal(10 - 3, foe.Hp); // full power now -- the effect is gone for good
    }
}
