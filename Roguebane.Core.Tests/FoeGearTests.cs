using Roguebane.Core.Content;

namespace Roguebane.Core.Tests;

// CHUNK D item 1 (STATUS.md, §8 symmetry): a foe that WIELDS a real Weapon and fights with a
// weapon-consulting verb must scale off the actual Weapon record and cascade off the same way a
// player's does -- proving Body.Wield/Consulted/DisabledGear need zero foe-special-casing.
public class FoeGearTests
{
    private static Fighter Bystander() => new(new Body(), maxHp: 500); // survives; applies no pressure

    private static Battle FightWith(Foe foe) =>
        new(new Caster(new Body()), new Encounter("e", foe, foePartAim: true), Bystander());

    [Fact]
    public void OgreSwingDealsTheWieldedMacesActualPowerNotAFlatHardcodedNumber()
    {
        var foe = Foes.Ogre("ogre");
        var player = Bystander();
        var battle = new Battle(new Caster(new Body()), new Encounter("e", foe, foePartAim: true), player);

        for (var i = 0; i < 90; i++) battle.Step(); // one Swing cooldown (Iron Mace timer 1.1x haste'd 80)

        Assert.Equal(500 - Armory.Maces[0].Power, player.Hp); // exactly the wielded weapon's own Power
    }

    [Fact]
    public void SmashingTheOgresWeaponArmDropsTheMaceFromConsultedGearJustLikeAPlayers()
    {
        var foe = Foes.Ogre("ogre");
        var frame = foe.Frame!;
        Assert.NotEmpty(frame.Consulted(Armory.Swing)); // wielding, sane baseline

        frame.Damage(frame.Parts[0], 3); // arm STR 4 -> 1, below the Iron Mace's Reserve 3

        Assert.Empty(frame.Consulted(Armory.Swing)); // DisabledGear sheds it -- the same cascade a player gets
    }

    [Fact]
    public void WithTheWeaponArmSmashedTheOgreNeverLandsAHit()
    {
        var foe = Foes.Ogre("ogre");
        foe.Frame!.Damage(foe.Frame!.Parts[0], 3); // silence Swing before the fight starts
        var player = Bystander();
        var battle = new Battle(new Caster(new Body()), new Encounter("e", foe, foePartAim: true), player);

        for (var i = 0; i < 200; i++) battle.Step();

        Assert.Equal(500, player.Hp); // no weapon to consult -> EffectivePower is 0 -> Hit() never lands
    }
}
