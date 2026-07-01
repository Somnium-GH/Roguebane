namespace Roguebane.Core.Content;

// The playable races (design/05, 2026-06-30). Attrs + HP are RACE-ONLY now; a CoreRune adds none.
// Blocks are placeholder — tuning + the race<->core matrix are "Needs human" touchpoints (§7/§17).
public static class Races
{
    // Balanced generalist, the sturdier body.
    public static readonly Race Human = new("human", Str: 3, Int: 3, Dex: 3, Con: 3, Hp: 20, Title: "Human");

    // Dex-leaning and frail — answers before the wall matters, but thin.
    public static readonly Race Elf = new("elf", Str: 2, Int: 3, Dex: 4, Con: 2, Hp: 14, Title: "Elf");

    // Roster order matches design/05's Race column.
    public static readonly IReadOnlyList<Race> Roster = new[] { Human, Elf };
}
