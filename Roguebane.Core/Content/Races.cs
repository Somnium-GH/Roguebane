namespace Roguebane.Core.Content;

// The playable races. Copy is CANON per design/05 v2 (drop #17, 2026-07-03) — ASCII dashes, the
// font lacks the em-dash. Attrs + HP are RACE-ONLY; a CoreRune adds none. Stats await the tuning
// session; the race<->core matrix stays "Needs human" (§7/§17).
public static class Races
{
    // Balanced generalist, the sturdier body.
    public static readonly Race Human = new("human", Str: 3, Int: 3, Dex: 3, Con: 3, Hp: 20, Title: "Human",
        Tag: "THE FOUNDER LINE", Blurb: "No innate edge or lack - fits any core it can afford.");

    // Dex-leaning and frail.
    public static readonly Race Elf = new("elf", Str: 2, Int: 3, Dex: 4, Con: 2, Hp: 14, Title: "Elf",
        Tag: "THE KEEN & FLEET", Blurb: "Keen and fleet, but frail - punishes a dropped block.");

    // Roster order matches design/05's Race column.
    public static readonly IReadOnlyList<Race> Roster = new[] { Human, Elf };
}
