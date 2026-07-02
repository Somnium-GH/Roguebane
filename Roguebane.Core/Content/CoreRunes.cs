namespace Roguebane.Core.Content;

// The core LAYOUTS (design/05). A core carries NO attrs — STR/INT/DEX/CON + HP are the Race's (§7).
// Cores differ by budget / discount / bays / starting equipment / Core Effect identity only. Values are
// placeholder (tuning is a "Needs human" touchpoint).
public static class CoreRunes
{
    // Fat budget, cheap runes — built for nothing in particular, climbs into any keystone you pay for.
    public static readonly CoreRune Grunt = new(
        "grunt",
        RuneBudget: 24,
        RuneDiscount: 1,
        DefaultEquipment: new[] { Techniques.Jab, Techniques.Brace, Techniques.Bandage },
        Archetype: "THE GENERALIST",
        Flavor: "No edge, no hole. A fat budget of cheap runes climbs into any keystone you pay for.",
        CoreEffectName: "Hollow Vessel",
        CoreEffectDesc: "Unspent rune budget converts live into a regenerating resource.");

    // A caster specialist: tight budget, the widest action bar.
    public static readonly CoreRune Adept = new(
        "adept",
        RuneBudget: 10,
        RuneDiscount: 0,
        DefaultEquipment: new[] { Techniques.Ember, Techniques.Drain, Techniques.Bandage },
        Archetype: "THE SCHOLAR",
        Flavor: "Frail chest, one arm - but a deep INT head for spells and the widest action bar.",
        CoreEffectName: "Overchannel",
        CoreEffectDesc: "Spells hit far harder while the head holds - but burn you as they cast.");

    // The Wall: built to hold the line — modest budget, no bays.
    public static readonly CoreRune Warden = new(
        "warden",
        RuneBudget: 12,
        RuneDiscount: 0,
        Bays: 0,
        DefaultEquipment: new[] { Techniques.Cleave, Techniques.Brace, Techniques.Bandage },
        Archetype: "THE WALL",
        Flavor: "Armour on every limb, no bay, fewer actions - soaks blows and holds the line.",
        CoreEffectName: "Unbroken Aegis",
        CoreEffectDesc: "The held shield never fully breaks while the chest still stands.");

    // The Binder: fights through summons — three bays, INT funds them all.
    public static readonly CoreRune Summoner = new(
        "summoner",
        RuneBudget: 12,
        RuneDiscount: 0,
        Bays: 3,
        DefaultEquipment: new[] { Techniques.Ember, Techniques.Brace, Techniques.Bandage },
        DefaultMinions: new[] { Minions.Skeleton, Minions.Shade }, // a Binder fields summons from the off
        Archetype: "THE BINDER",
        Flavor: "Three bays - fights through a war-party of summons while staying back. INT funds them all.",
        CoreEffectName: "Legion",
        CoreEffectDesc: "On redeploy, surviving minions refund their Summons.",
        CoreEffectRefundsSummons: true); // the first REAL Core Effect [LOCKED §11]; CD reconciles the card copy

    // The Duelist: glass-cannon, no bays — ends parts before they answer.
    public static readonly CoreRune Reaver = new(
        "reaver",
        RuneBudget: 12,
        RuneDiscount: 0,
        Bays: 0,
        DefaultEquipment: new[] { Techniques.Lunge, Techniques.Jab, Techniques.Bandage },
        Archetype: "THE DUELIST",
        Flavor: "No shield, twin blades. Glass-cannon STR-DEX - ends parts before they answer.",
        CoreEffectName: "Bloodrush",
        CoreEffectDesc: "Each part you break quickens your next strike.");

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
        DefaultWeapons: new[] { Armory.Bow },
        Archetype: "THE MARKSMAN",
        Flavor: "Strikes from range with a shield-piercing bow; high DEX, thin armour - answers first.",
        CoreEffectName: "Piercing Focus",
        CoreEffectDesc: "A held aim sharpens every shot into the same wound.");

    // Roster order matches design/05's Choose-Your-Core line-up.
    public static readonly IReadOnlyList<CoreRune> Roster =
        new[] { Grunt, Warden, Adept, Summoner, Reaver, Ranger };
}
