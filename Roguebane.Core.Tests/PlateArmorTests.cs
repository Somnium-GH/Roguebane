namespace Roguebane.Core.Tests;

// §8/§6b: the flat-protection plate is retired — PLATE is now a worn SHIELD SOURCE. Equipping it raises
// a shield pool (Value layers) on its part-group; the pool absorbs hits (§6b), and RIDES the group's
// condition (break the group and the worn shield sheds with it). Leather stays evasion, not a shield.
public class PlateArmorTests
{
    private static (Body body, BodyPart chest) ChestBody(int con)
    {
        var body = new Body();
        var chest = new BodyPart("chest", Stat.Con, con);
        body.Add(chest);
        return (body, chest);
    }

    [Fact]
    public void EquippingPlateRaisesAShieldPool()
    {
        var (body, _) = ChestBody(5);
        body.Equip(new Armor("plate", Stat.Con, ArmorKind.Plate, 2));
        Assert.Equal(2, body.ShieldPoints); // 2 worn layers
    }

    [Fact]
    public void ThePlateShieldAbsorbsIncomingDamage()
    {
        var (body, _) = ChestBody(5);
        body.Equip(new Armor("plate", Stat.Con, ArmorKind.Plate, 2));
        Assert.Equal(1, body.AbsorbShields(3)); // 2 layers eat 2, 1 spills
        Assert.Equal(0, body.ShieldPoints);
    }

    [Fact]
    public void BreakingThePartGroupShedsThePlateShield()
    {
        var (body, chest) = ChestBody(3);
        body.Equip(new Armor("plate", Stat.Con, ArmorKind.Plate, 2));
        Assert.Equal(2, body.ShieldPoints);

        body.Damage(chest, 3); // CON -> 0: the plate's group is gone
        Assert.Equal(0, body.ShieldPoints); // shield rides the part, sheds with it
    }

    [Fact]
    public void UnequippingDropsThePlateShield()
    {
        var (body, _) = ChestBody(5);
        body.Equip(new Armor("plate", Stat.Con, ArmorKind.Plate, 2));
        body.Unequip(Stat.Con);
        Assert.Equal(0, body.ShieldPoints);
    }

    [Fact]
    public void LeatherRaisesNoShield()
    {
        var (body, _) = ChestBody(5);
        body.Equip(new Armor("hide", Stat.Con, ArmorKind.Leather, 25));
        Assert.Equal(0, body.ShieldPoints); // evasion, not a shield pool
    }
}
