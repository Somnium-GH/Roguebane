namespace Roguebane.Core.Content;

// The §6c armor ladders — names + structure are CANON (locked 2026-07-03); tier-bonus numbers are
// blessed-initial and live on the Armor record. Slots key to the §6 anatomy: Head=INT, Chest=CON,
// Arms=STR, Legs=DEX (a plate HELM covers the Head slot while its LINE — and sustain — is STR).
// The CON shield-object ladder (Wooden Shield → Tower Shield) is a hand-config item and ships
// with the §6d wield-model build, not here.
public static class ArmorLines
{
    private static Armor[] Ladder(ArmorLine line, Stat slot, string prefix, params string[] names) =>
        names.Select((n, i) => new Armor(
            prefix + "-" + (i + 1), n, slot, line, i + 1)).ToArray();

    // STR — heavy/plate (all four slots).
    public static readonly IReadOnlyList<Armor> PlateHead =
        Ladder(ArmorLine.Plate, Stat.Int, "plate-head", "Skull Cap", "Barbute", "Great Helm", "Crowned Helm");
    public static readonly IReadOnlyList<Armor> PlateChest =
        Ladder(ArmorLine.Plate, Stat.Con, "plate-chest", "Breastplate", "Splint Mail", "Half Plate", "Full Plate");
    public static readonly IReadOnlyList<Armor> PlateArms =
        Ladder(ArmorLine.Plate, Stat.Str, "plate-arms", "Vambraces", "Splint Vambraces", "Banded Gauntlets", "Plate Gauntlets");
    public static readonly IReadOnlyList<Armor> PlateLegs =
        Ladder(ArmorLine.Plate, Stat.Dex, "plate-legs", "Greaves", "Splint Greaves", "Half-Plate Legs", "Full Plate Legs");

    // DEX — leather (all four slots).
    public static readonly IReadOnlyList<Armor> LeatherHead =
        Ladder(ArmorLine.Leather, Stat.Int, "leather-head", "Leather Cap", "Hardened Cap", "Studded Cap", "Reinforced Hood");
    public static readonly IReadOnlyList<Armor> LeatherChest =
        Ladder(ArmorLine.Leather, Stat.Con, "leather-chest", "Padded Armor", "Leather Armor", "Studded Leather", "Reinforced Leather");
    public static readonly IReadOnlyList<Armor> LeatherArms =
        Ladder(ArmorLine.Leather, Stat.Str, "leather-arms", "Leather Bracers", "Hardened Bracers", "Studded Bracers", "Reinforced Bracers");
    public static readonly IReadOnlyList<Armor> LeatherLegs =
        Ladder(ArmorLine.Leather, Stat.Dex, "leather-legs", "Leather Leggings", "Hardened Leggings", "Studded Leggings", "Reinforced Leggings");

    // INT — robe (Chest + Head ONLY; no arm/leg robe pieces exist).
    public static readonly IReadOnlyList<Armor> RobeChest =
        Ladder(ArmorLine.Robe, Stat.Con, "robe-chest", "Cotton Robe", "Silk Robe", "Ornate Robe", "Humming Robe");
    public static readonly IReadOnlyList<Armor> RobeHead =
        Ladder(ArmorLine.Robe, Stat.Int, "robe-head", "Cloth Cap", "Silk Hood", "Ornate Circlet", "Humming Circlet");

    public static readonly IReadOnlyList<IReadOnlyList<Armor>> Ladders = new[]
    {
        PlateHead, PlateChest, PlateArms, PlateLegs,
        LeatherHead, LeatherChest, LeatherArms, LeatherLegs,
        RobeChest, RobeHead,
    };

    public static IEnumerable<Armor> All => Ladders.SelectMany(l => l);
}
