namespace Roguebane.Core.Content;

// What a merchant offers beyond its HP-heal service: a small gear stock (weapons + armor) the
// player can buy into the Stash pack and equip later. The set and prices are placeholder-sane (a
// "Needs human" balance touchpoint); the mechanic is what matters here.
public static class Shops
{
    // Chest plate: a worn SHIELD SOURCE (§6b/§8) — raises 2 shield layers on the chest group while it
    // stands. Value = shield layers (placeholder).
    public static readonly Armor Plate = new("plate", Stat.Con, ArmorKind.Plate, 2);
    public static readonly Armor Hide = new("hide", Stat.Dex, ArmorKind.Leather, 25);    // leg leather: 25% evasion

    public static readonly IReadOnlyList<Weapon> Weapons = new[] { Armory.Sword, Armory.Dagger };
    public static readonly IReadOnlyList<Armor> Armor = new[] { Plate }; // legacy fixed stock (retiring)
    public static readonly IReadOnlyList<Armor> ArmorPool = new[] { Plate, Hide }; // §12 stock pool
}
