namespace Roguebane.Core.Content;

public static class Chassrium
{
    // Head=INT, Chest=CON, Arms x2 split STR, Legs x2 split DEX. Low scale: ~20 is huge.
    private static IReadOnlyList<BodyPart> StandardBody(string id, int str, int intel, int dex, int con) => new[]
    {
        new BodyPart($"{id}-head", Stat.Int, intel),
        new BodyPart($"{id}-chest", Stat.Con, con),
        new BodyPart($"{id}-arm-l", Stat.Str, str / 2),
        new BodyPart($"{id}-arm-r", Stat.Str, str - str / 2),
        new BodyPart($"{id}-leg-l", Stat.Dex, dex / 2),
        new BodyPart($"{id}-leg-r", Stat.Dex, dex - dex / 2),
    };

    // Low all-round base, fat budget, cheap runes — built for nothing in particular.
    public static readonly Chassis Grunt = new(
        "grunt",
        StandardBody("grunt", str: 4, intel: 3, dex: 4, con: 4),
        RuneBudget: 24,
        RuneDiscount: 1,
        DefaultLoadout: new[] { Techniques.Jab, Techniques.Brace },
        Archetype: "THE GENERALIST",
        Flavor: "No edge, no hole. A fat budget of cheap runes climbs into any keystone you pay for.");

    // A caster specialist: high INT, tight budget.
    public static readonly Chassis Adept = new(
        "adept",
        StandardBody("adept", str: 4, intel: 10, dex: 4, con: 5),
        RuneBudget: 10,
        RuneDiscount: 0,
        DefaultLoadout: new[] { Techniques.Ember, Techniques.Drain },
        Archetype: "THE SCHOLAR",
        Flavor: "Frail chest, one arm - but a deep INT head for spells and the widest action bar.");

    // The Wall: STR-CON tank built to hold the line — fat chest, modest budget, no bays.
    public static readonly Chassis Warden = new(
        "warden",
        StandardBody("warden", str: 6, intel: 3, dex: 3, con: 9),
        RuneBudget: 12,
        RuneDiscount: 0,
        Bays: 0,
        DefaultLoadout: new[] { Techniques.Cleave, Techniques.Brace },
        Archetype: "THE WALL",
        Flavor: "Armour on every limb, no bay, fewer actions - soaks blows and holds the line.");

    // The Binder: an INT core that fights through summons — three bays, INT funds them all.
    public static readonly Chassis Summoner = new(
        "summoner",
        StandardBody("summoner", str: 3, intel: 9, dex: 4, con: 4),
        RuneBudget: 12,
        RuneDiscount: 0,
        Bays: 3,
        DefaultLoadout: new[] { Techniques.Ember, Techniques.Brace },
        DefaultMinions: new[] { Minions.Skeleton, Minions.Shade }, // a Binder fields summons from the off
        Archetype: "THE BINDER",
        Flavor: "Three bays - fights through a war-party of summons while staying back. INT funds them all.");

    // The Duelist: glass-cannon STR-DEX, thin chest, no bays — ends parts before they answer.
    public static readonly Chassis Reaver = new(
        "reaver",
        StandardBody("reaver", str: 7, intel: 3, dex: 7, con: 3),
        RuneBudget: 12,
        RuneDiscount: 0,
        Bays: 0,
        DefaultLoadout: new[] { Techniques.Lunge, Techniques.Jab },
        Archetype: "THE DUELIST",
        Flavor: "No shield, twin blades. Glass-cannon STR-DEX - ends parts before they answer.");

    // Roster order matches design/05's Choose-Your-Core line-up. Stat/budget values are placeholder
    // (tuning is a "Needs human" touchpoint); slot/bay/action-count shapes wait on those systems.
    public static readonly IReadOnlyList<Chassis> Roster = new[] { Grunt, Warden, Adept, Summoner, Reaver };
}
