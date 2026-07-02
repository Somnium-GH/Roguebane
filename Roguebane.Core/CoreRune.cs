namespace Roguebane.Core;

// A CoreRune is data: the LAYOUT you socket a Race's body into. It carries NO attrs (those are the
// Race's, §7) — only a rune budget, a per-rung discount, bays, and the fixed starting equipment/minions.
// The thesis lives in the tension between budget and discount: a fat-budget, cheap-rune core can climb
// to a keystone it was never built for. DefaultEquipment is the FIXED starting kit (the action bar is
// never empty, no build-time "pick a technique" gate; finds grow the kit mid-run).
public sealed record CoreRune(
    string Id,
    int RuneBudget,
    int RuneDiscount = 0,
    int Bays = 1,
    IReadOnlyList<Technique>? DefaultEquipment = null,
    IReadOnlyList<Minion>? DefaultMinions = null,
    IReadOnlyList<Weapon>? DefaultWeapons = null, // wielded at assembly so a consulting verb has a stick
    string Archetype = "",  // the one-line identity ("THE GENERALIST") shown on the New Run / build cards
    string Flavor = "",     // the card's short pitch (design/05)
    string CoreEffectName = "",   // Core Effect label (the core's signature effect) shown on the cards
    string CoreEffectDesc = "",   // Core Effect blurb; most effects are display-only for now (§11)...
    bool CoreEffectRefundsSummons = false, // ...EXCEPT this one [LOCKED §11]: on Redeploy, surviving
                                           // minions refund their Summons (the Summoner's real effect)
    string Accent = "")     // colorBind accent (a palette token) for core.accent/preview.accent;
                            // empty keeps the manifest's static chrome — per-core VALUES await design
{
    // Display name for the cards ("grunt" -> "Grunt"); the Id stays the lowercase content key.
    public string Title => string.IsNullOrEmpty(Id) ? Id : char.ToUpperInvariant(Id[0]) + Id[1..];

    // The manifest figure key for a race+core pairing: uniform `<race>_<core>` (no bare case).
    public string FigureKey(Race race) => race.Id + "_" + Id;

    public IReadOnlyList<Technique> Kit => DefaultEquipment ?? Array.Empty<Technique>();

    // The minions the core fields from the start (summoned into its bays at assembly), the minion
    // analogue of the fixed technique Kit. Rune grants add more on top, capped by Bays.
    public IReadOnlyList<Minion> MinionKit => DefaultMinions ?? Array.Empty<Minion>();

    // The weapons the core wields from the start (a bow for the Marksman) — a consulting verb (Shot)
    // reads its power/cost from these. Wielded at assembly if the body can lift them (stat threshold).
    public IReadOnlyList<Weapon> WeaponKit => DefaultWeapons ?? Array.Empty<Weapon>();

    // The socketed body: the RACE supplies the anatomy + attrs, then each held rune rung sockets its
    // extension parts on top, and the core's starting weapons are wielded into its hands.
    public Body NewBody(Race race, RuneLoadout runes)
    {
        var body = race.NewBody();
        foreach (var mark in runes.HeldMarks)
            foreach (var part in mark.Granted)
                body.Add(part);
        foreach (var weapon in WeaponKit) body.Wield(weapon);
        return body;
    }

    public RuneLoadout NewLoadout() => new(RuneBudget, RuneDiscount);
}
