using Roguebane.Core.Content;

namespace Roguebane.Core.Tests;

// §8 SYMMETRY (one shared framework): a foe drives its Frame through the SAME caster the player uses,
// so it can HEAL and SHIELD itself with real techniques -- not hardcoded specials. These lock that a
// foe's own offense caster runs the §10 part-heal and §6b shield on the foe body, mid-Battle.
public class FoeSymmetryTests
{
    private static Body FoeFrame()
    {
        var frame = new Body();
        frame.Add(new BodyPart("foe-arm", Stat.Str, 4));  // the part the player would smash
        frame.Add(new BodyPart("foe-head", Stat.Int, 4)); // powers a shield source
        frame.Add(new BodyPart("foe-chest", Stat.Con, 4)); // powers the heal
        return frame;
    }

    private static Technique Strike => new("strike", Stat.Str, 1, TechniqueKind.Sustained, 0, Power: 1);
    private static Technique Mend =>
        new("mend", Stat.Con, 1, TechniqueKind.Sustained, Cooldown: 0, Power: 2, Heals: true);

    private static Fighter Bystander() => new(new Body(), maxHp: 500); // survives; applies no pressure

    private static Battle FightWith(Foe foe) =>
        new(new Caster(new Body()), new Encounter("e", foe, foePartAim: true), Bystander());

    [Fact]
    public void AFoeRepairsItsOwnSmashedPartViaAHealTechnique()
    {
        var frame = FoeFrame();
        frame.Damage(frame.Parts[0], 3); // arm STR 4 -> 1, as if the player had smashed it
        var foe = new Foe("healer", 40, frame, new[] { Strike, Mend });

        var battle = FightWith(foe);
        for (var i = 0; i < 5; i++) battle.Step();

        // The foe's OWN offense caster ran Mend on its most-damaged part (§10), same as the player.
        Assert.Equal(4, frame.Contribution(frame.Parts[0])); // arm mended back to full
    }

    [Fact]
    public void WithoutTheHealTechniqueTheSmashedPartStaysDown()
    {
        var frame = FoeFrame();
        frame.Damage(frame.Parts[0], 3);
        var foe = new Foe("no-heal", 40, frame, new[] { Strike }); // no heal in the arsenal

        var battle = FightWith(foe);
        for (var i = 0; i < 5; i++) battle.Step();

        Assert.Equal(1, frame.Contribution(frame.Parts[0])); // nothing repairs it
    }

    [Fact]
    public void AFoeRaisesItsOwnShieldViaAShieldTechnique()
    {
        var frame = FoeFrame();
        var foe = new Foe("warded", 40, frame, new[] { Strike, Techniques.Barkskin });

        var battle = FightWith(foe);
        battle.Step();

        // Same §6b path as the player: the foe's caster raised a shield pool on the foe body.
        Assert.True(frame.ShieldPoints > 0);
    }
}
