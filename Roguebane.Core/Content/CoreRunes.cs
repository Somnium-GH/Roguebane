namespace Roguebane.Core.Content;

// The core LAYOUTS (design/05, v6 RULES_SNAPSHOT). A core's stat bonus, budget, action-bar size,
// minion capacity, starting kit, and Core Effect identity are ALL content — this is the one place
// that data lives. Values are placeholder-blessed (tuning is a "Needs human" touchpoint) except
// where RULES_SNAPSHOT flags an item OPEN (Sacrifice's heal numbers).
public static class CoreRunes
{
    // §7a starting armor: all-four-STR-slot plate (Grunt/Warden/Barbarian), chest+head robe
    // (Adept/Summoner), all-four-DEX-slot leather (Reaver/Ranger) — every piece tier 1, mechanical
    // equip only.
    private static readonly IReadOnlyList<Armor> PlateKitT1 =
        new[] { ArmorLines.PlateHead[0], ArmorLines.PlateChest[0], ArmorLines.PlateArms[0], ArmorLines.PlateLegs[0] };
    private static readonly IReadOnlyList<Armor> RobeKitT1 =
        new[] { ArmorLines.RobeChest[0], ArmorLines.RobeHead[0] };
    private static readonly IReadOnlyList<Armor> LeatherKitT1 =
        new[] { ArmorLines.LeatherHead[0], ArmorLines.LeatherChest[0], ArmorLines.LeatherArms[0], ArmorLines.LeatherLegs[0] };

    // Fat budget, cheap runes — built for nothing in particular, climbs into any keystone you pay for.
    public static readonly CoreRune Grunt = new(
        "grunt",
        RuneBudget: 20,
        ActionSlots: 4,
        MinionCap: 2,
        DefaultEquipment: new[] { Techniques.Jab, Techniques.Brace, Techniques.Bandage },
        DefaultWeapons: new[] { Armory.Longswords[0], Armory.Shields[0] }, // Iron Longsword + Wooden Shield
        DefaultArmor: PlateKitT1,
        StrBonus: 1, IntBonus: 1, DexBonus: 1, ConBonus: 1,
        Archetype: "THE GENERALIST",
        Flavor: "No edge, no hole. A fat budget of cheap runes climbs into any keystone you pay for.",
        Badge: "STARTER",
        CoreEffectName: "Jack of All Trades",
        CoreEffectDesc: "Every attribute cost you pay is reduced by 1.",
        Effect: CoreEffectKind.JackOfAllTrades,
        Accent: "amber"); // CHUNK C item 2 stopgap: splits Grunt off the str line as the generalist.
        // Reuses the manifest's OWN palette token (layout.json style.palette) rather than inventing a
        // value — B20 still owns the canonical per-core token if CD wants to change it later.

    // A caster specialist: tight budget, the widest action bar.
    public static readonly CoreRune Adept = new(
        "adept",
        RuneBudget: 16,
        ActionSlots: 4,
        MinionCap: 1, // capacity 1, starts empty — matches CORE_RUNES.md ("none (capacity 1)") and
                      // core-kits.js (bayCap 1, bays:[]); the old 0 was same-day drift (Doug 2026-07-05,
                      // no documented reason), reconciled 2026-07-12 per Doug's 7-core audit.
        // Jab added 2026-07-12 (Roguebane_Balance (14).xlsx): with the Staff now STR-gated it becomes a
        // free backup attack, giving Adept a real STR pressure (Demand tab: STR 3 = Staff 2 + Jab 1)
        // alongside its INT spell suite. Fills the 4th action slot.
        DefaultEquipment: new[] { Techniques.Ember, Techniques.Siphon, Techniques.Stoneskin, Techniques.Jab },
        DefaultWeapons: new[] { Armory.Staffs[0] }, // Wooden Staff
        DefaultArmor: RobeKitT1,
        IntBonus: 5,
        Archetype: "THE SCHOLAR",
        Flavor: "Frail chest, one arm - but a deep INT head for spells and the widest action bar.",
        Badge: "CASTER",
        CoreEffectName: "Resonance",
        CoreEffectDesc: "Each targeted spell that hits reduces its next charge time by 2%, stacking up to 5 times.",
        Effect: CoreEffectKind.Resonance,
        Accent: "int"); // CHUNK C item 2 stopgap: base int-line color (robe kit).

    // The Wall: built to hold the line — modest budget, no minion capacity.
    public static readonly CoreRune Warden = new(
        "warden",
        RuneBudget: 18,
        ActionSlots: 4,
        MinionCap: 1,
        DefaultEquipment: new[] { Techniques.Jab, Techniques.Brace, Techniques.Bandage },
        DefaultWeapons: new[] { Armory.Longswords[0], Armory.Shields[1] }, // Iron Longsword + Iron Buckler
        DefaultArmor: PlateKitT1,
        ConBonus: 5,
        Archetype: "THE WALL",
        Flavor: "Armour on every limb, no bay, fewer actions - soaks blows and holds the line.",
        Badge: "BULWARK",
        CoreEffectName: "Fortified",
        CoreEffectDesc: "Plate armor is paid in CON at 1 less per tier.",
        Effect: CoreEffectKind.Fortified,
        Accent: "gold"); // CHUNK C item 2 stopgap: splits Warden off the str line, away from Barbarian.

    // The Binder: fights through summons — three minion slots, INT funds them all.
    public static readonly CoreRune Summoner = new(
        "summoner",
        RuneBudget: 17,
        ActionSlots: 3,
        MinionCap: 3,
        DefaultEquipment: new[] { Techniques.Ember, Techniques.Sacrifice, Techniques.Barkskin },
        DefaultWeapons: new[] { Armory.Wands[0], Armory.Charms[0] }, // Adept Wand + Wooden Charm
        DefaultArmor: RobeKitT1,
        DefaultMinions: new[] { Minions.Skeleton, Minions.IronGolem }, // a Binder fields summons from the off
        IntBonus: 3, ConBonus: 2,
        Archetype: "THE BINDER",
        Flavor: "Three bays - fights through a war-party of summons while staying back. INT funds them all.",
        Badge: "SPECIALIST",
        CoreEffectName: "Conscription",
        CoreEffectDesc: "Minions do not consume Summons when activated.",
        Effect: CoreEffectKind.Conscription,
        CoreEffectFreeSummons: true, // §11 LOCKED; CD reconciles the card copy
        Accent: "teal"); // CHUNK C item 2 stopgap: splits Summoner off the int line, away from Adept.

    // The Duelist: glass-cannon, no minion capacity — twin blades end parts before they answer.
    // Frenzy/Flurry are the real Task #3 kit (TECHNIQUES.md/CORE_RUNES.md LOCKED 2026-07-05): both
    // are stat-flexible (AltStat: Stat.Dex), so Reaver's twin DEX daggers consult them directly.
    // Bandage restored 2026-07-05 (Doug + balance spreadsheet Kits/Demand tabs — CON 2 demand): Reaver
    // carries the flat CON part-heal like every core bar Adept (Siphon) / Summoner (Sacrifice); the
    // earlier "no heal glass cannon" interim was a loop artifact of the pre-Bandage healing map.
    public static readonly CoreRune Reaver = new(
        "reaver",
        RuneBudget: 19,
        ActionSlots: 4,
        MinionCap: 0,
        DefaultEquipment: new[] { Armory.Frenzy, Armory.Flurry, Techniques.Bandage },
        DefaultWeapons: new[] { Armory.Daggers[0], Armory.Daggers[0] }, // twin Iron Daggers
        DefaultArmor: LeatherKitT1,
        DexBonus: 5,
        Archetype: "THE DUELIST",
        Flavor: "No shield, twin blades. Glass-cannon STR-DEX - ends parts before they answer.",
        Badge: "SPECIALIST",
        CoreEffectName: "Finesse",
        CoreEffectDesc: "Techniques requiring two weapons cost 1 less to activate.",
        Effect: CoreEffectKind.Finesse,
        Accent: "dex"); // CHUNK C item 2 stopgap: base dex-line color (shared with Ranger — STATUS's
        // "a darker cut if needed" named no concrete second token, so this stays unsplit rather than
        // inventing one; B20 owns the real split if CD wants it).

    // The Marksman: ranged core. Its signature is the shield-piercing BOW (charge #4) — Aimed Shot
    // bypasses shields for Charge. But Charge is scarce (INT-pooled, no mid-fight refill), so a pure-bow
    // build runs dry and stalls; the kit pairs Aimed Shot with a CHARGE-FREE DEX melee (Lunge) for
    // sustained damage — the bow is the pierce finisher, the blade is bread-and-butter.
    public static readonly CoreRune Ranger = new(
        "ranger",
        RuneBudget: 18,
        ActionSlots: 4,
        MinionCap: 2,
        DefaultEquipment: new[] { Armory.AimedShot, Techniques.Lunge, Techniques.Bandage },
        DefaultWeapons: new[] { Armory.Daggers[0], Armory.Bow }, // Iron Dagger + Short Bow
        DefaultArmor: LeatherKitT1,
        DefaultMinions: new[] { Minions.Hound }, // §7a: the Marksman's tracker pet
        DexBonus: 4, ConBonus: 1,
        Archetype: "THE MARKSMAN",
        Flavor: "Strikes from range with a shield-piercing bow; high DEX, thin armour - answers first.",
        Badge: "SPECIALIST",
        CoreEffectName: "Fletcher's Luck",
        CoreEffectDesc: "Bow techniques have a 20% chance to consume no charge when fired, and bows cost 1 less per tier to equip.",
        Effect: CoreEffectKind.FletcherLuck,
        Accent: "dex"); // CHUNK C item 2 stopgap: keeps dex-green per STATUS's explicit instruction.

    // The Warlord: Half-Giant's exact-fit home — a two-handed claymore and STR plate, built to spend
    // everything the body has on hitting hard and standing in it.
    public static readonly CoreRune Barbarian = new(
        "barbarian",
        RuneBudget: 14,
        ActionSlots: 3,
        MinionCap: 1,
        DefaultEquipment: new[] { Techniques.Cleave, Techniques.Bind, Techniques.Bandage },
        DefaultWeapons: new[] { Armory.Claymores[0] }, // Iron Claymore (2H)
        DefaultArmor: PlateKitT1,
        StrBonus: 4, ConBonus: 1,
        Archetype: "THE WARLORD",
        Flavor: "One great blade, plate on every limb - spends everything the body has on the swing.",
        Badge: "SPECIALIST",
        CoreEffectName: "Warlord's Might",
        CoreEffectDesc: "Two-handed swords cost 3 less strength to equip; STR plate costs 1 less strength per piece to equip.",
        Effect: CoreEffectKind.WarlordMight,
        Accent: "str"); // CHUNK C item 2 stopgap: base str-line color (Grunt/Warden split off amber/gold).

    // Roster order matches design/05's Choose-Your-Core line-up.
    public static readonly IReadOnlyList<CoreRune> Roster =
        new[] { Grunt, Warden, Adept, Summoner, Reaver, Ranger, Barbarian };
}
