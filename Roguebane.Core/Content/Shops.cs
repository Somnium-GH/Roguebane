namespace Roguebane.Core.Content;

// What a merchant offers beyond its HP-heal service: a small gear stock (weapons + armor) the
// player can buy into the Stash pack and equip later. The set and prices are placeholder-sane (a
// "Needs human" balance touchpoint); the mechanic is what matters here.
public static class Shops
{
    // Merchant staples off the §6c ladders (rung-1 pieces; deeper rungs join the stock with the
    // economy/rarity tune). "Plate"/"Hide" now NAME canon §6c pieces — the old bespoke shield-plate
    // and 25%-hide are retired with the worn-shield role.
    public static readonly Armor Plate = ArmorLines.PlateChest[0];   // Breastplate (STR line, chest)
    public static readonly Armor Hide = ArmorLines.LeatherLegs[0];   // Leather Leggings (DEX line, legs)

    public static readonly IReadOnlyList<Weapon> Weapons = new[] { Armory.Sword, Armory.Dagger };
    public static readonly IReadOnlyList<Armor> ArmorPool = new[] { Plate, Hide }; // §12 stock pool
}
