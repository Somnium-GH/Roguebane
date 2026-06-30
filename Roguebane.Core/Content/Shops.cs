namespace Roguebane.Core.Content;

// What a merchant offers beyond the potion/heal services: a small gear stock (weapons + armor) the
// player can buy into the Stash pack and equip later. The set and prices are placeholder-sane (a
// "Needs human" balance touchpoint); the mechanic is what matters here.
public static class Shops
{
    public static readonly Armor Plate = new("plate", Stat.Con, ArmorKind.Plate, 2);     // chest plate: flat protection
    public static readonly Armor Hide = new("hide", Stat.Dex, ArmorKind.Leather, 25);    // leg leather: 25% evasion

    public static readonly IReadOnlyList<Weapon> Weapons = new[] { Armory.Sword, Armory.Dagger };
    public static readonly IReadOnlyList<Armor> Armor = new[] { Plate }; // shop stock (Hide arrives via finds, later)
}
