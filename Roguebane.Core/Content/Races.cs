namespace Roguebane.Core.Content;

// The playable races. Copy is CANON per design/05 v2 (drop #17, 2026-07-03) — ASCII dashes, the
// font lacks the em-dash. Attrs + HP are RACE-ONLY; a CoreRune adds none. Stats await the tuning
// session; the race<->core matrix stays "Needs human" (§7/§17).
public static class Races
{
    // Balanced generalist, the sturdier body.
    // Bumped 2026-07-04 (Debt, placeholder): the cumulative gear-sustain model (SUSTAIN MODEL) sums a
    // core's whole kit against ONE shared pool per stat, not per-item. The old 3/3/3/3 was sized for
    // per-item gating and left several cores (Warden's full plate + Brace, Ranger's blade+bow+leather,
    // Summoner's minions) unable to sustain their OWN starting kit. Raised just enough for every
    // race+core combo to fight and win (CoreCampaignTests) - final numbers still "Needs human" (§7/§17).
    public static readonly Race Human = new("human", Str: 14, Int: 14, Dex: 12, Con: 8, Hp: 20, Title: "Human",
        Tag: "THE FOUNDER LINE", Blurb: "No innate edge or lack - fits any core it can afford.");

    // Dex-leaning and frail (see Human bump note above for why these moved off 2/3/4/2).
    public static readonly Race Elf = new("elf", Str: 14, Int: 12, Dex: 16, Con: 7, Hp: 14, Title: "Elf",
        Tag: "THE KEEN & FLEET", Blurb: "Keen and fleet, but frail - punishes a dropped block.");

    // Roster order matches design/05's Race column.
    public static readonly IReadOnlyList<Race> Roster = new[] { Human, Elf };
}
