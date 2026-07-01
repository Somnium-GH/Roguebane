namespace Roguebane.Core.Tests;

// §10 in-combat part-heal: a repair technique mends the caster's OWN most-damaged part (never HP),
// re-powering what a smashed part had cascaded off. Reconcile trigger for live foe part-aim (G1).
public class HealTechniqueTests
{
    private static BodyPart Arm => new("arm", Stat.Str, 4);
    private static BodyPart Chest => new("chest", Stat.Con, 4);

    private static Body Wounded(out BodyPart arm)
    {
        arm = Arm;
        var body = new Body();
        body.Add(arm);
        body.Add(Chest);
        body.Damage(arm, 3); // arm STR 4 -> contribution 1
        return body;
    }

    private static Technique Heal(TechniqueKind kind, int power) =>
        new("heal", Stat.Con, Reserve: 1, kind, Cooldown: kind == TechniqueKind.Timered ? 10 : 0, power, Heals: true);

    [Fact]
    public void RepairsTheMostDamagedPart()
    {
        var body = Wounded(out var arm);
        var caster = new Caster(body);
        caster.Activate(Heal(TechniqueKind.Sustained, power: 2));

        caster.Step();

        Assert.Equal(3, body.Contribution(arm)); // 1 + 2 mended
    }

    [Fact]
    public void RevivesABrokenPartBackIntoThePool()
    {
        var arm = Arm;
        var body = new Body();
        body.Add(arm);
        body.Add(Chest);
        body.Damage(arm, 4); // arm broken -> STR 0
        Assert.Equal(0, body.Capacity(Stat.Str));

        var caster = new Caster(body);
        caster.Activate(Heal(TechniqueKind.Sustained, power: 2));
        caster.Step();

        Assert.Equal(2, body.Capacity(Stat.Str)); // stat back in the pool
    }

    [Fact]
    public void HoldsFireWhenNothingIsHurt()
    {
        var body = new Body();
        body.Add(Arm);
        body.Add(Chest);
        var heal = Heal(TechniqueKind.Timered, power: 2);
        var caster = new Caster(body);
        caster.Activate(heal);

        for (var i = 0; i < 30; i++) caster.Step();

        // Whole body -> the heal never discharges, so it stays READY to fire the instant a part is hit.
        Assert.True(caster.IsReady(heal));
        Assert.Equal(4, body.Capacity(Stat.Str));
        Assert.Equal(4, body.Capacity(Stat.Con));
    }

    [Fact]
    public void BandageIsAHealAndNowInThePalette()
    {
        Assert.True(Content.Techniques.Bandage.Heals);
        Assert.Contains(Content.Techniques.Bandage, Content.Techniques.All); // live: kits carry the heal
    }
}
