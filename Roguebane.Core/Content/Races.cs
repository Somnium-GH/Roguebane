namespace Roguebane.Core.Content;

// The playable races (design/05, 2026-06-30). Attrs + HP are RACE-ONLY now; a CoreRune adds none.
// Blocks are placeholder — tuning + the race<->core matrix are "Needs human" touchpoints (§7/§17).
public static class Races
{
    // Balanced generalist, the sturdier body. Tag/Blurb are DISPLAY-ONLY flavour (placeholder — tune).
    public static readonly Race Human = new("human", Str: 3, Int: 3, Dex: 3, Con: 3, Hp: 20, Title: "Human",
        Tag: "THE FOUNDER LINE", Blurb: "No innate edge or lack - fits any core it wears.");

    // Dex-leaning and frail — answers before the wall matters, but thin.
    public static readonly Race Elf = new("elf", Str: 2, Int: 3, Dex: 4, Con: 2, Hp: 14, Title: "Elf",
        Tag: "THE KEEN KINDRED", Blurb: "Fleet + accurate, but thin - answers before the wall matters.");

    // Roster order matches design/05's Race column.
    public static readonly IReadOnlyList<Race> Roster = new[] { Human, Elf };
}
