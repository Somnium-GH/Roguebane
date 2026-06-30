using Roguebane.Core.Content;

namespace Roguebane.Core.Tests;

// G2 gear: the Stash carries a gear pack; equipping moves a piece onto the body (honoring its wield/
// wear gates) and unequipping returns it. A piece is never in both the pack and on the body at once.
public class GearingTests
{
    private static Body StrBody(int str)
    {
        var b = new Body();
        b.Add(new BodyPart("arm-l", Stat.Str, str)); // shares STR across two arms
        b.Add(new BodyPart("arm-r", Stat.Str, str));
        b.Add(new BodyPart("chest", Stat.Con, 4));
        return b;
    }

    [Fact]
    public void EquippingAWeaponMovesItFromThePackOntoTheBody()
    {
        var pack = new Stash();
        var body = StrBody(2); // STR 4 total — lifts the Sword (reserve 3)
        pack.AddWeapon(Armory.Sword);

        Assert.True(Gearing.EquipWeapon(pack, body, Armory.Sword));
        Assert.Contains(Armory.Sword, body.Hands);
        Assert.False(pack.HasWeapon(Armory.Sword)); // not in both places
    }

    [Fact]
    public void AWeaponTheBodyCannotLiftStaysInThePack()
    {
        var pack = new Stash();
        var body = StrBody(1); // STR 2 total — under the Axe's reserve 4
        pack.AddWeapon(Armory.Axe);

        Assert.False(Gearing.EquipWeapon(pack, body, Armory.Axe));
        Assert.True(pack.HasWeapon(Armory.Axe));     // still carried
        Assert.DoesNotContain(Armory.Axe, body.Hands);
    }

    [Fact]
    public void EquippingAnUncarriedWeaponFails()
    {
        var pack = new Stash();
        var body = StrBody(3);
        Assert.False(Gearing.EquipWeapon(pack, body, Armory.Sword)); // not in the pack
        Assert.DoesNotContain(Armory.Sword, body.Hands);
    }

    [Fact]
    public void UnequippingAWeaponReturnsItToThePack()
    {
        var pack = new Stash();
        var body = StrBody(2);
        pack.AddWeapon(Armory.Sword);
        Gearing.EquipWeapon(pack, body, Armory.Sword);

        Assert.True(Gearing.UnequipWeapon(pack, body, Armory.Sword));
        Assert.DoesNotContain(Armory.Sword, body.Hands);
        Assert.True(pack.HasWeapon(Armory.Sword));
    }

    [Fact]
    public void EquippingArmorDisplacesTheWornPieceBackToThePack()
    {
        var pack = new Stash();
        var body = StrBody(2);
        var plate = new Armor("plate", Stat.Con, ArmorKind.Plate, 2);
        var heavy = new Armor("heavy", Stat.Con, ArmorKind.Plate, 3); // same group (chest)
        pack.AddArmor(plate);
        pack.AddArmor(heavy);

        Assert.True(Gearing.EquipArmor(pack, body, plate));
        Assert.Equal(plate, body.ArmorOn(Stat.Con));

        Assert.True(Gearing.EquipArmor(pack, body, heavy)); // same group -> swaps
        Assert.Equal(heavy, body.ArmorOn(Stat.Con));
        Assert.True(pack.HasArmor(plate));  // the displaced piece came back
        Assert.False(pack.HasArmor(heavy)); // the worn piece left the pack
    }

    [Fact]
    public void UnequippingArmorReturnsItToThePack()
    {
        var pack = new Stash();
        var body = StrBody(2);
        var plate = new Armor("plate", Stat.Con, ArmorKind.Plate, 2);
        pack.AddArmor(plate);
        Gearing.EquipArmor(pack, body, plate);

        Assert.True(Gearing.UnequipArmor(pack, body, Stat.Con));
        Assert.Null(body.ArmorOn(Stat.Con));
        Assert.True(pack.HasArmor(plate));
    }
}
