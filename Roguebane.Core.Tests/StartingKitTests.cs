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
    public void WardenWieldsLongswordAndShieldWornInPlate()
    {
        // cores.json target (balance (14)): Iron Buckler downgraded to a Wooden Shield (drops CON req
        // 10->9); a Shepherd's Sling was added on the ranged slot.
        var body = Assemble(CoreRunes.Warden);
        Assert.Contains(body.Hands, w => w.Id == "longsword_iron");
        Assert.Contains(body.Hands, w => w.Id == "shield_wooden");
        Assert.Equal("armor_str_chest_iron", body.ArmorOn(Stat.Con)?.Id);
    }

    [Fact]
    public void AdeptWieldsStaffWornInRobe()
    {
        var body = Assemble(CoreRunes.Adept);
        Assert.Contains(body.Hands, w => w.Id == "staff_wooden");
        Assert.Equal("armor_int_chest_cotton", body.ArmorOn(Stat.Con)?.Id);
        Assert.Equal("armor_int_head_cotton", body.ArmorOn(Stat.Int)?.Id);
        Assert.Empty(CoreRunes.Adept.MinionKit); // v6: no minion cap, the Scholar fields nothing
    }

    [Fact]
    public void SummonerWieldsWandAndShieldWornInRobe()
    {
        // cores.json target kit (2026-07-12): Adept Wand + Wooden Shield (the old Wooden Charm was
        // dropped when Summoner picked up Blast/Brace and the shield in the rebuild).
        var body = Assemble(CoreRunes.Summoner);
        Assert.Contains(body.Hands, w => w.Id == "wand_adept");
        Assert.Contains(body.Hands, w => w.Id == "shield_wooden");
        Assert.Equal("armor_int_chest_cotton", body.ArmorOn(Stat.Con)?.Id);
    }

    [Fact]
    public void ReaverWieldsMixedBladesWornInLeather()
    {
        // cores.json target (2026-07-12 balance (14)): the twin-dagger kit became a STR+DEX mixed pair
        // (Iron Longsword + Iron Rapier) so Frenzy/Flurry's stat-flexibility actually matters.
        var body = Assemble(CoreRunes.Reaver);
        Assert.Contains(body.Hands, w => w.Id == "longsword_iron");
        Assert.Contains(body.Hands, w => w.Id == "rapier_iron");
        Assert.Equal("armor_dex_legs_plain", body.ArmorOn(Stat.Dex)?.Id);
        Assert.Equal("armor_dex_arms_plain", body.ArmorOn(Stat.Str)?.Id);
    }

    [Fact]
    public void RangerWieldsAxeBowAndShieldWornInLeather()
    {
        // cores.json target (balance (14)): dropped the dagger for an Iron Axe and gained a Wooden
        // Shield (hand items); the Short Bow still mounts the separate ranged slot.
        var body = Assemble(CoreRunes.Ranger);
        Assert.Contains(body.Hands, w => w.Id == "axe_iron");
        Assert.Contains(body.Hands, w => w.Id == "shield_wooden");
        Assert.Equal("bow_short", body.Ranged?.Id);
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
