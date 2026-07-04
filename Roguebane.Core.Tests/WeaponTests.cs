using Roguebane.Core.Content;

namespace Roguebane.Core.Tests;

// G2: weapons are stat-sticks; techniques CONSULT them for cost and power; losing the arm drops them.
public class WeaponTests
{
    private static (Body, BodyPart, BodyPart) ArmsBody(int strEach)
    {
        var body = new Body();
        var l = new BodyPart("arm-l", Stat.Str, strEach);
        var r = new BodyPart("arm-r", Stat.Str, strEach);
        body.Add(l);
        body.Add(r);
        return (body, l, r);
    }

    [Fact]
    public void WieldingGatesOnStatCapacity()
    {
        var (weak, _, _) = ArmsBody(1);   // 2 STR total
        Assert.False(weak.Wield(Armory.Maces[0])); // Iron Mace needs 3, can't lift it

        var (strong, _, _) = ArmsBody(3); // 6 STR total
        Assert.True(strong.Wield(Armory.Maces[0]));
    }

    [Fact]
    public void TwoHandsIsTheLimit()
    {
        var (body, _, _) = ArmsBody(4); // 8 STR
        Assert.True(body.Wield(Armory.Sword));
        Assert.True(body.Wield(Armory.Axe));
        Assert.False(body.Wield(Armory.Dagger)); // no third hand
    }

    [Fact]
    public void ASwingConsultsThePrimaryWeaponForPowerAndReserve()
    {
        var (body, _, _) = ArmsBody(3); // 6 STR
        body.Wield(Armory.Sword);       // Iron Longsword: power 4, reserve 2
        var foe = new Foe("front", 100);
        var caster = new Caster(body, foe);

        Assert.True(caster.Activate(Armory.Swing));
        Assert.Equal(2, body.Reserved(Stat.Str)); // sword's reserve, not the technique's 0

        caster.Step(); caster.Step(); // timered cd 2: fires on the 2nd tick
        Assert.Equal(96, foe.Hp);      // sword power 4
    }

    [Fact]
    public void WithoutAWeaponAConsultingTechniqueCannotActivate()
    {
        var (body, _, _) = ArmsBody(6);
        var caster = new Caster(body, new Foe("f", 10));
        Assert.False(caster.Activate(Armory.Swing)); // nothing to swing
    }

    [Fact]
    public void FrenzyConsultsBothWeaponsSummingTheirReserveAndPower()
    {
        var (body, _, _) = ArmsBody(5); // 10 STR
        body.Wield(Armory.Sword);       // Iron Longsword r2 p4
        body.Wield(Armory.Axe);         // Iron Axe r1 p3
        var foe = new Foe("front", 100);
        var caster = new Caster(body, foe);

        Assert.True(caster.Activate(Armory.Frenzy));
        Assert.Equal(3, body.Reserved(Stat.Str)); // 2 + 1

        for (var i = 0; i < 3; i++) caster.Step(); // cd 3
        Assert.Equal(93, foe.Hp);                  // 4 + 3 = 7
    }

    [Fact]
    public void SmashingAnArmDropsAWeaponItCanNoLongerLift()
    {
        var (body, l, r) = ArmsBody(2); // 4 STR, the Iron Mace needs 3
        body.Wield(Armory.Maces[0]);
        Assert.Single(body.Hands);

        body.Damage(l, 2); // STR 4 -> 2, below the mace's threshold
        Assert.Empty(body.Hands);
    }
}
