namespace Roguebane.Core.Content;

// The core LAYOUTS (design/05). A core carries NO attrs — STR/INT/DEX/CON + HP are the Race's (§7).
// Cores differ by budget / discount / bays / starting equipment / Core Effect identity only. Values are
// placeholder (tuning is a "Needs human" touchpoint).
public static class CoreRunes
{
    // §7a starting armor: all-four-STR-slot plate (Grunt/Warden), chest+head robe (Adept/Summoner),
    // all-four-DEX-slot leather (Reaver/Ranger) — every piece tier 1, mechanical equip only.
    private static readonly IReadOnlyList<Armor> PlateKitT1 =
        new[] { ArmorLines.PlateHead[0], ArmorLines.PlateChest[0], ArmorLines.PlateArms[0], ArmorLines.PlateLegs[0] };
    private static readonly IReadOnlyList<Armor> RobeKitT1 =
        new[] { ArmorLines.RobeChest[0], ArmorLines.RobeHead[0] };
    private static readonly IReadOnlyList<Armor> LeatherKitT1 =
        new[] { ArmorLines.LeatherHead[0], ArmorLines.LeatherChest[0], ArmorLines.LeatherArms[0], ArmorLines.LeatherLegs[0] };

    // Fat budget, cheap runes — built for nothing in particular, climbs into any keystone you pay for.
    public static readonly CoreRune Grunt = new(
        "grunt",
        RuneBudget: 24,
        RuneDiscount: 1,
        DefaultEquipment: new[] { Techniques.Jab, Techniques.Brace, Techniques.Bandage },
        DefaultWeapons: new[] { Armory.Longswords[0], Armory.Shields[0] }, // Iron Longsword + Wooden Shield
        DefaultArmor: PlateKitT1,
        Archetype: "THE GENERALIST",
        Flavor: "No edge, no hole. A fat budget of cheap runes climbs into any keystone you pay for.",
        Badge: "STARTER",
        CoreEffectName: "Hollow Vessel",
        CoreEffectDesc: "Healed for unspent budget points after each encounter.");

    // A caster specialist: tight budget, the widest action bar.
    public static readonly CoreRune Adept = new(
        "adept",
        RuneBudget: 10,
        RuneDiscount: 0,
        DefaultEquipment: new[] { Techniques.Ember, Techniques.Drain, Techniques.Bandage },
        DefaultWeapons: new[] { Armory.Staffs[0] }, // Wooden Staff
        DefaultArmor: RobeKitT1,
        DefaultMinions: new[] { Minions.Skeleton }, // §7a: the Scholar fields one Skeleton from the off
        Archetype: "THE SCHOLAR",
        Flavor: "Frail chest, one arm - but a deep INT head for spells and the widest action bar.",
        Badge: "CASTER",
        CoreEffectName: "Overchannel",
        CoreEffectDesc: "Spells reserve no INT while the head stays above three-quarters.");

    // The Wall: built to hold the line — modest budget, no bays.
    public static readonly CoreRune Warden = new(
        "warden",
        RuneBudget: 12,
        RuneDiscount: 0,
        Bays: 0,
        DefaultEquipment: new[] { Techniques.Cleave, Techniques.Brace, Techniques.Bandage },
        DefaultWeapons: new[] { Armory.Longswords[0], Armory.Shields[1] }, // Iron Longsword + Iron Buckler
        DefaultArmor: PlateKitT1,
        Archetype: "THE WALL",
        Flavor: "Armour on every limb, no bay, fewer actions - soaks blows and holds the line.",
        Badge: "BULWARK",
        CoreEffectName: "Unbroken Aegis",
        CoreEffectDesc: "Shield points regenerate at twice their CON-scaled rate.");

    // The Binder: fights through summons — three bays, INT funds them all.
    public static readonly CoreRune Summoner = new(
        "summoner",
        RuneBudget: 12,
        RuneDiscount: 0,
        Bays: 3,
        DefaultEquipment: new[] { Techniques.Ember, Techniques.Brace, Techniques.Bandage },
        DefaultWeapons: new[] { Armory.Wands[0], Armory.Charms[0] }, // Adept Wand + Wooden Charm
        DefaultArmor: RobeKitT1,
        DefaultMinions: new[] { Minions.Skeleton, Minions.Shade }, // a Binder fields summons from the off
        Archetype: "THE BINDER",
        Flavor: "Three bays - fights through a war-party of summons while staying back. INT funds them all.",
        Badge: "SPECIALIST",
        CoreEffectName: "Legion",
        CoreEffectDesc: "Surviving minions' Summons are refunded on Redeploy.",
        CoreEffectRefundsSummons: true); // the first REAL Core Effect [LOCKED §11]; CD reconciles the card copy

    // The Duelist: glass-cannon, no bays — ends parts before they answer.
    public static readonly CoreRune Reaver = new(
        "reaver",
        RuneBudget: 12,
        RuneDiscount: 0,
        Bays: 0,
        DefaultEquipment: new[] { Techniques.Lunge, Techniques.Jab, Techniques.Bandage },
        DefaultWeapons: new[] { Armory.Daggers[0], Armory.Daggers[0] }, // twin Iron Daggers
        DefaultArmor: LeatherKitT1,
        Archetype: "THE DUELIST",
        Flavor: "No shield, twin blades. Glass-cannon STR-DEX - ends parts before they answer.",
        Badge: "SPECIALIST",
        CoreEffectName: "Bloodrush",
        CoreEffectDesc: "Every part you break refunds a charging technique.");

    // The Marksman: ranged core. Its signature is the shield-piercing BOW (charge #4) — Shot bypasses
    // shields for Charge. But Charge is scarce (INT-pooled, no mid-fight refill), so a pure-bow build
    // runs dry and stalls; the kit pairs Shot with a CHARGE-FREE DEX melee (Lunge) for sustained damage —
    // the bow is the pierce finisher, the blade is bread-and-butter. Verified winnable (CoreCampaignTests).
    public static readonly CoreRune Ranger = new(
        "ranger",
        RuneBudget: 12,
        RuneDiscount: 0,
        Bays: 0,
        DefaultEquipment: new[] { Armory.Shot, Techniques.Lunge, Techniques.Brace, Techniques.Bandage },
        DefaultWeapons: new[] { Armory.ShortSwords[0], Armory.Bow }, // Iron Short Sword + Short Bow
        DefaultArmor: LeatherKitT1,
        Archetype: "THE MARKSMAN",
        Flavor: "Strikes from range with a shield-piercing bow; high DEX, thin armour - answers first.",
        Badge: "SPECIALIST",
        CoreEffectName: "Called Shot",
        CoreEffectDesc: "Ranged techniques ignore the foe's shield and cover bonuses.");

    // Roster order matches design/05's Choose-Your-Core line-up.
    public static readonly IReadOnlyList<CoreRune> Roster =
        new[] { Grunt, Warden, Adept, Summoner, Reaver, Ranger };
}
