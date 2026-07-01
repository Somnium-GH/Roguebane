namespace Roguebane.Core.Content;

// The playable races (design/05, 2026-06-30). Attrs + HP are RACE-ONLY now; a CoreRune adds none.
// Blocks are placeholder — tuning + the race<->core matrix are "Needs human" touchpoints (§7/§17).
public static class Races
{
    // Balanced generalist, the sturdier body.
    public static readonly Race Human = new("human", Str: 4, Int: 4, Dex: 5, Con: 6, Hp: 30, Title: "Human");

    // Dex-leaning and frail — answers before the wall matters, but thin.
    public static readonly Race Elf = new("elf", Str: 3, Int: 4, Dex: 6, Con: 4, Hp: 22, Title: "Elf");

    // Roster order matches design/05's Race column.
    public static readonly IReadOnlyList<Race> Roster = new[] { Human, Elf };
}
