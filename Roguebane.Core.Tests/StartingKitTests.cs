using Roguebane.Core.Content;

namespace Roguebane.Core.Tests;

// §7a: the six starting kits (weapon + armor + minion per core), locked 2026-07-04. Mechanical
// equip only — no worn-art render yet (LAYOUT_CONTRACT §12a is a separate slice).
public class StartingKitTests
{
    private static Body Assemble(CoreRune core) =>
        core.NewBody(Races.Human, core.NewLoadout());

    [Fact]
    public void GruntWieldsLongswordAndShieldWornInPlate()
    {
        var body = Assemble(CoreRunes.Grunt);
        Assert.Contains(body.Hands, w => w.Id == "longsword_iron");
        Assert.Contains(body.Hands, w => w.Id == "shield_wooden");
        Assert.Equal("armor_str_head_iron", body.ArmorOn(Stat.Int)?.Id);
        Assert.Equal("armor_str_chest_iron", body.ArmorOn(Stat.Con)?.Id);
        Assert.Equal("armor_str_arms_iron", body.ArmorOn(Stat.Str)?.Id);
        Assert.Equal("armor_str_legs_iron", body.ArmorOn(Stat.Dex)?.Id);
    }

    [Fact]
    public void WardenWieldsLongswordAndIronBucklerWornInPlate()
    {
        var body = Assemble(CoreRunes.Warden);
        Assert.Contains(body.Hands, w => w.Id == "longsword_iron");
        Assert.Contains(body.Hands, w => w.Id == "shield_iron"); // Iron Buckler, T2
        Assert.Equal("armor_str_chest_iron", body.ArmorOn(Stat.Con)?.Id);
    }

    [Fact]
    public void AdeptWieldsStaffWornInRobeAndFieldsASkeleton()
    {
        var body = Assemble(CoreRunes.Adept);
        Assert.Contains(body.Hands, w => w.Id == "staff_wooden");
        Assert.Equal("armor_int_chest_cotton", body.ArmorOn(Stat.Con)?.Id);
        Assert.Equal("armor_int_head_cotton", body.ArmorOn(Stat.Int)?.Id);
        Assert.Contains(Minions.Skeleton, CoreRunes.Adept.MinionKit);
    }

    [Fact]
    public void SummonerWieldsWandAndCharmWornInRobe()
    {
        var body = Assemble(CoreRunes.Summoner);
        Assert.Contains(body.Hands, w => w.Id == "wand_adept");
        Assert.Contains(body.Hands, w => w.Id == "charm_wooden");
        Assert.Equal("armor_int_chest_cotton", body.ArmorOn(Stat.Con)?.Id);
    }

    [Fact]
    public void ReaverWieldsTwinDaggersWornInLeather()
    {
        var body = Assemble(CoreRunes.Reaver);
        Assert.Equal(2, body.Hands.Count(w => w.Id == "dagger_iron"));
        Assert.Equal("armor_dex_legs_plain", body.ArmorOn(Stat.Dex)?.Id);
        Assert.Equal("armor_dex_arms_plain", body.ArmorOn(Stat.Str)?.Id);
    }

    [Fact]
    public void RangerWieldsShortSwordAndBowWornInLeather()
    {
        var body = Assemble(CoreRunes.Ranger);
        Assert.Contains(body.Hands, w => w.Id == "shortsword_iron");
        Assert.Equal("bow", body.Ranged?.Id);
        Assert.Equal("armor_dex_legs_plain", body.ArmorOn(Stat.Dex)?.Id);
    }

    [Fact]
    public void EveryRosterCoreEquipsItsWholeArmorKit()
    {
        // No piece silently fails its equip gate — Human's baseline attrs cover every §7a kit.
        foreach (var core in CoreRunes.Roster)
        {
            var body = Assemble(core);
            foreach (var piece in core.ArmorKit)
                Assert.Equal(piece, body.ArmorOn(piece.Slot));
        }
    }

    // P3 balance pass [LOCKED 2026-07-04]: every race+core combo must equip its FULL default kit —
    // Equip/Wield/EquipRanged gate on raw Capacity (§17 SUSTAIN MODEL note), so this is the frailest
    // race (Elf, Con 7) crossed with every core, not just Human.
    [Fact]
    public void EveryRaceAndCoreEquipsItsWholeDefaultKit()
    {
        foreach (var race in Races.Roster)
            foreach (var core in CoreRunes.Roster)
            {
                var body = core.NewBody(race, core.NewLoadout());
                foreach (var piece in core.ArmorKit)
                    Assert.Equal(piece, body.ArmorOn(piece.Slot));
                foreach (var weapon in core.WeaponKit)
                    Assert.True(body.Hands.Contains(weapon) || body.Ranged == weapon,
                        $"{race.Id}/{core.Id}: {weapon.Id} failed to equip");
            }
    }
}
