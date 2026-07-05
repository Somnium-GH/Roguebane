namespace Roguebane.Core;

// A CoreRune is data: the LAYOUT you socket a Race's body into. It carries an additive STAT BONUS
// (StrBonus/IntBonus/DexBonus/ConBonus, folded into the Race's body at NewBody below, CORE_RUNES.md) on
// top of the Race's own attrs (§7), plus a rune budget, a per-rung discount, minion capacity, and the
// fixed starting equipment/minions.
// The thesis lives in the tension between budget and discount: a fat-budget, cheap-rune core can climb
// to a keystone it was never built for. DefaultEquipment is the FIXED starting kit (the action bar is
// never empty, no build-time "pick a technique" gate; finds grow the kit mid-run).
public sealed record CoreRune(
    string Id,
    int RuneBudget,
    int RuneDiscount = 0,
    int MinionCap = 1,
    int StrBonus = 0, // additive stat bonus on top of the Race's own attrs (CORE_RUNES.md's table)
    int IntBonus = 0,
    int DexBonus = 0,
    int ConBonus = 0,
    IReadOnlyList<Technique>? DefaultEquipment = null,
    IReadOnlyList<Minion>? DefaultMinions = null,
    IReadOnlyList<Weapon>? DefaultWeapons = null, // wielded at assembly so a consulting verb has a stick
    IReadOnlyList<Armor>? DefaultArmor = null, // worn at assembly, one piece per slot (§7a starting kits)
    string Archetype = "",  // the one-line identity ("THE GENERALIST") shown on the New Run / build cards
    string Flavor = "",     // the card's short pitch (design/05)
    string CoreEffectName = "",   // Core Effect label (the core's signature effect) shown on the cards
    string CoreEffectDesc = "",   // Core Effect blurb; most effects are display-only for now (§11)...
    bool CoreEffectRefundsSummons = false, // one legacy mechanized effect: on Redeploy, surviving
                                           // minions refund their Summons. No v6 core uses this anymore.
    bool CoreEffectFreeSummons = false, // Summoner's Conscription [LOCKED, CORE_RUNES.md]: fielding a
                                        // minion never spends the Summons resource at all (replaces the
                                        // old refund-on-Redeploy Legion effect above — genuinely different).
    string Accent = "",     // colorBind accent (a palette token) for core.accent/preview.accent;
                            // empty keeps the manifest's static chrome — per-core VALUES await design
    string Badge = "")      // the role chip (STARTER/BULWARK/CASTER/SPECIALIST), design/05 v2 `core.badge`
{
    // Display name for the cards ("grunt" -> "Grunt"); the Id stays the lowercase content key.
    public string Title => string.IsNullOrEmpty(Id) ? Id : char.ToUpperInvariant(Id[0]) + Id[1..];

    // The manifest figure key for a race+core pairing: uniform `<race>_<core>` (no bare case).
    public string FigureKey(Race race) => race.Id + "_" + Id;

    public IReadOnlyList<Technique> Kit => DefaultEquipment ?? Array.Empty<Technique>();

    // The minions the core fields from the start (summoned into its minion capacity at assembly), the
    // minion analogue of the fixed technique Kit. Rune grants add more on top, capped by MinionCap.
    public IReadOnlyList<Minion> MinionKit => DefaultMinions ?? Array.Empty<Minion>();

    // The weapons the core wields from the start (a bow for the Marksman) — a consulting verb (Shot)
    // reads its power/cost from these. Wielded at assembly if the body can lift them (stat threshold).
    public IReadOnlyList<Weapon> WeaponKit => DefaultWeapons ?? Array.Empty<Weapon>();

    // The armor the core starts worn in (§7a) — mechanical equip only, no worn-art render yet
    // (LAYOUT_CONTRACT §12a is a separate slice). Gated the same as any other Equip: a piece that
    // doesn't meet its governing attribute simply doesn't go on.
    public IReadOnlyList<Armor> ArmorKit => DefaultArmor ?? Array.Empty<Armor>();

    // The socketed body: the RACE supplies the anatomy + attrs, then each held rune rung sockets its
    // extension parts on top, and the core's starting weapons/armor are equipped.
    public Body NewBody(Race race, RuneLoadout runes)
    {
        var body = race.NewBody(StrBonus, IntBonus, DexBonus, ConBonus);
        foreach (var mark in runes.HeldMarks)
            foreach (var part in mark.Granted)
                body.Add(part);
        // §6d two equip layers: bows/slings mount the RANGED slot, everything else the hands.
        foreach (var weapon in WeaponKit)
            if (!body.EquipRanged(weapon))
                body.Wield(weapon);
        foreach (var piece in ArmorKit)
            body.Equip(piece);
        return body;
    }

    public RuneLoadout NewLoadout() => new(RuneBudget, RuneDiscount);
}
