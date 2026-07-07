using Roguebane.Core.Content;

namespace Roguebane.Core.Tests;

// CHUNK D item 1's other half (STATUS.md, §8 symmetry): FoeGearTests proved a foe's WIELDED weapon
// scales and cascades exactly like a player's; this proves the same for WORN armor. Body.Equip/
// Damage's PartMitigation/ArmorSustained read nothing but Stat pools and part ids -- no Foe type is
// involved anywhere in that path -- so a bare foe-shaped Body (single arm, §6 anatomy) is the whole
// proof surface. Deliberately does NOT reuse Foes.Ogre's numbers: FOES.md's Dire Ogre spec (parts
// 5/1/2/4, Iron Warhammer, STR Breastplate) doesn't actually fit its own arm -- see STATUS.md's new
// Needs Doug note. This fixture sidesteps that by equipping armor alone, no competing weapon.
public class FoeArmorTests
{
    // Foe anatomy shape (Foes.cs): one arm (Str), head (Int), legs (Dex), chest (Con) -- no second
    // arm like the player Humanoid fixtures use, matching how every foe body is actually built.
    private static Body GearedFoe(out BodyPart arm, out BodyPart chest)
    {
        var b = new Body();
        arm = new BodyPart("arm", Stat.Str, 4);
        b.Add(arm);
        b.Add(new BodyPart("head", Stat.Int, 1));
        b.Add(new BodyPart("legs", Stat.Dex, 2));
        chest = new BodyPart("chest", Stat.Con, 6);
        b.Add(chest);
        return b;
    }

    [Fact]
    public void WornPlateMitigatesAFoesPartDamageExactlyLikeAPlayers()
    {
        var foe = GearedFoe(out _, out var chest);
        Assert.True(foe.Equip(ArmorLines.PlateChest[0])); // Iron Breastplate: Str-governed, Requirement 2, arm has 4

        foe.Damage(chest, 6); // PartMitigation 2 eats 2 of the 6 -> 4 lands, same math as the player case

        Assert.Equal(2, foe.Contribution(chest)); // 6 capacity - 4 mitigated damage
    }

    [Fact]
    public void SmashingTheGoverningArmDropsTheFoesArmorFromSustainJustLikeAPlayers()
    {
        var foe = GearedFoe(out var arm, out var chest);
        Assert.True(foe.Equip(ArmorLines.PlateChest[0]));
        Assert.True(foe.ArmorSustained(ArmorLines.PlateChest[0]));

        foe.Damage(arm, 3); // Str 4 -> 1, below the Breastplate's own Requirement (2) -- its governing stat

        Assert.False(foe.ArmorSustained(ArmorLines.PlateChest[0])); // DisabledGear sheds it, same cascade a player gets
        foe.Damage(chest, 6); // no mitigation left to apply
        Assert.Equal(0, foe.Contribution(chest)); // full raw damage lands, not 4
    }
}
