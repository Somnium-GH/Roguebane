namespace Roguebane.Core.Content;

// The §6c armor ladders — names + structure are CANON (locked 2026-07-03 naming session; the
// engine data is the canon source — the CD gear catalog's display names drifted and are logged
// back, issues outbox). Ids follow the CD gear-catalog SPRITE convention
// (armor_{line-attr}_{slot}_{tier}) so sprites/gear/{id} resolves with zero mapping. Slots key to
// the §6 anatomy: Head=INT, Chest=CON, Arms=STR, Legs=DEX (a plate HELM covers the Head slot while
// its LINE — and sustain — is STR). The CON shield-object ladder (Wooden Shield → Tower Shield) is
// a hand-config item and ships with the §6d wield-model build, not here.
public static class ArmorLines
{
    private static readonly Dictionary<Stat, string> SlotWord = new()
    {
        [Stat.Int] = "head", [Stat.Con] = "chest", [Stat.Str] = "arms", [Stat.Dex] = "legs",
    };

    private static Armor[] Ladder(ArmorLine line, Stat slot, string attr, string[] tierIds,
        params string[] names) =>
        names.Select((n, i) => new Armor(
            $"armor_{attr}_{SlotWord[slot]}_{tierIds[i]}", n, slot, line, i + 1)).ToArray();

    private static readonly string[] PlateTiers = { "iron", "steel", "mithral", "dwarven" };
    private static readonly string[] LeatherTiers = { "plain", "hardened", "studded", "reinforced" };
    private static readonly string[] RobeTiers = { "cotton", "silk", "ornate", "humming" };

    // STR — heavy/plate (all four slots). RENAMED 2026-07-03 naming session: the material ladder
    // (Iron → Steel → Mithral → Dwarven Steel) on one plain noun per slot, matching the weapon
    // roster; the prestige names (Barbute/Great Helm/...) are §18-DROPPED.
    private static Armor[] Plate(Stat slot, string noun) => Ladder(ArmorLine.Plate, slot, "str",
        PlateTiers, "Iron " + noun, "Steel " + noun, "Mithral " + noun, "Dwarven Steel " + noun);

    public static readonly IReadOnlyList<Armor> PlateHead = Plate(Stat.Int, "Helm");
    public static readonly IReadOnlyList<Armor> PlateChest = Plate(Stat.Con, "Breastplate");
    public static readonly IReadOnlyList<Armor> PlateArms = Plate(Stat.Str, "Vambraces");
    public static readonly IReadOnlyList<Armor> PlateLegs = Plate(Stat.Dex, "Greaves");

    // DEX — leather (all four slots).
    public static readonly IReadOnlyList<Armor> LeatherHead = Ladder(ArmorLine.Leather, Stat.Int,
        "dex", LeatherTiers, "Leather Cap", "Hardened Cap", "Studded Cap", "Reinforced Hood");
    public static readonly IReadOnlyList<Armor> LeatherChest = Ladder(ArmorLine.Leather, Stat.Con,
        "dex", LeatherTiers, "Padded Armor", "Leather Armor", "Studded Leather", "Reinforced Leather");
    public static readonly IReadOnlyList<Armor> LeatherArms = Ladder(ArmorLine.Leather, Stat.Str,
        "dex", LeatherTiers, "Leather Bracers", "Hardened Bracers", "Studded Bracers", "Reinforced Bracers");
    public static readonly IReadOnlyList<Armor> LeatherLegs = Ladder(ArmorLine.Leather, Stat.Dex,
        "dex", LeatherTiers, "Leather Leggings", "Hardened Leggings", "Studded Leggings", "Reinforced Leggings");

    // INT — robe (Chest + Head ONLY; no arm/leg robe pieces exist).
    public static readonly IReadOnlyList<Armor> RobeChest = Ladder(ArmorLine.Robe, Stat.Con,
        "int", RobeTiers, "Cotton Robe", "Silk Robe", "Ornate Robe", "Humming Robe");
    public static readonly IReadOnlyList<Armor> RobeHead = Ladder(ArmorLine.Robe, Stat.Int,
        "int", RobeTiers, "Cloth Cap", "Silk Hood", "Ornate Circlet", "Humming Circlet");

    public static readonly IReadOnlyList<IReadOnlyList<Armor>> Ladders = new[]
    {
        PlateHead, PlateChest, PlateArms, PlateLegs,
        LeatherHead, LeatherChest, LeatherArms, LeatherLegs,
        RobeChest, RobeHead,
    };

    public static IEnumerable<Armor> All => Ladders.SelectMany(l => l);
}
