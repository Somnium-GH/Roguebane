namespace Roguebane.Core;

// A chassis is data: the body you socket into. Its parts (Head, Chest, Arms x2, Legs x2) carry
// the stat shares, plus a rune budget and a per-rung discount. The thesis lives in the tension
// between these: a fat-budget, cheap-rune chassis can climb to a keystone it was never built for.
// DefaultEquipment is the FIXED starting kit it ships with — the action bar is never empty and there
// is no build-time "pick a technique" gate; finds grow the kit mid-run.
public sealed record Chassis(
    string Id,
    IReadOnlyList<BodyPart> BodyParts,
    int RuneBudget,
    int RuneDiscount = 0,
    int Bays = 1,
    IReadOnlyList<Technique>? DefaultEquipment = null,
    IReadOnlyList<Minion>? DefaultMinions = null,
    string Archetype = "",  // the one-line identity ("THE GENERALIST") shown on the New Run / build cards
    string Flavor = "")     // the card's short pitch (design/05)
{
    // Display name for the cards ("grunt" -> "Grunt"); the Id stays the lowercase content key.
    public string Title => string.IsNullOrEmpty(Id) ? Id : char.ToUpperInvariant(Id[0]) + Id[1..];

    // The manifest figure key. Figures are uniform `<race>_<core>` (no bare "unprefixed = human" case);
    // Race isn't a built axis yet, so default to human_<core>. When Race lands, thread the race here.
    public string FigureKey => "human_" + Id;

    public IReadOnlyList<Technique> Kit => DefaultEquipment ?? Array.Empty<Technique>();

    // The minions the chassis fields from the start (summoned into its bays at assembly), the minion
    // analogue of the fixed technique Kit. Rune grants add more on top, capped by Bays.
    public IReadOnlyList<Minion> MinionKit => DefaultMinions ?? Array.Empty<Minion>();

    public Body NewBody()
    {
        var body = new Body();
        foreach (var part in BodyParts) body.Add(part);
        return body;
    }

    // The socketed body including everything the allocated runes grant: chassis parts first, then
    // each held rung's extension parts. Climbing a chassis-extending keystone widens the live pool.
    public Body NewBody(RuneLoadout runes)
    {
        var body = NewBody();
        foreach (var mark in runes.HeldMarks)
            foreach (var part in mark.Granted)
                body.Add(part);
        return body;
    }

    public RuneLoadout NewLoadout() => new(RuneBudget, RuneDiscount);
}
