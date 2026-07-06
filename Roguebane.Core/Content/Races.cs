namespace Roguebane.Core.Content;

// The playable races. Copy is CANON per design/05 v2 (drop #17, 2026-07-03) — ASCII dashes, the
// font lacks the em-dash. Attrs are RACE-ONLY; a CoreRune adds its own bonus on top (RULES_SNAPSHOT
// v6 formula: baseline 4/4/4/4, Human +1 all, each specialist +2 into its one affinity). HP per race
// stays a flagged placeholder (RULES_SNAPSHOT OPEN item) — spread kept CON-correlated like the prior
// 2-race set, final numbers await Doug's tune.
public static class Races
{
    public static readonly Race Human = new("human", Str: 5, Int: 5, Dex: 5, Con: 5, Hp: 16, Title: "Human",
        Tag: "THE FOUNDER LINE", Blurb: "No innate edge or lack - fits any core it can afford.");

    public static readonly Race Elf = new("elf", Str: 4, Int: 6, Dex: 4, Con: 4, Hp: 13, Title: "Elf",
        Tag: "THE DEEP MINDED", Blurb: "A keen head for spellcraft, but frail - punishes a dropped block.");

    public static readonly Race Dwarf = new("dwarf", Str: 4, Int: 4, Dex: 4, Con: 6, Hp: 20, Title: "Dwarf",
        Tag: "THE UNYIELDING", Blurb: "Thick of chest and slow to fall - the line holds where a Dwarf stands.");

    public static readonly Race Halfling = new("halfling", Str: 4, Int: 4, Dex: 6, Con: 4, Hp: 13, Title: "Halfling",
        Tag: "THE QUICK STEP", Blurb: "Fast hands, faster feet - answers before the foe can.");

    public static readonly Race HalfGiant = new("half_giant", Str: 6, Int: 4, Dex: 4, Con: 4, Hp: 17, Title: "Half-Giant",
        Tag: "THE BROKEN GROUND", Blurb: "Strength enough to carry what breaks a smaller back.");

    // Roster order matches design/05's Race column.
    public static readonly IReadOnlyList<Race> Roster = new[] { Human, Elf, Dwarf, Halfling, HalfGiant };
}
