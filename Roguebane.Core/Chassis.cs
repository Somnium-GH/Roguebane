namespace Roguebane.Core;

// A chassis is data: the body you socket into. Its parts (Head, Chest, Arms x2, Legs x2) carry
// the stat shares, plus a rune budget and a per-rung discount. The thesis lives in the tension
// between these: a fat-budget, cheap-rune chassis can climb to a keystone it was never built for.
// DefaultLoadout is the FIXED starting kit it ships with — the action bar is never empty and there
// is no build-time "pick a technique" gate; finds grow the kit mid-run.
public sealed record Chassis(
    string Id,
    IReadOnlyList<BodyPart> BodyParts,
    int RuneBudget,
    int RuneDiscount = 0,
    int Bays = 1,
    IReadOnlyList<Technique>? DefaultLoadout = null,
    IReadOnlyList<Minion>? DefaultMinions = null)
{
    public IReadOnlyList<Technique> Kit => DefaultLoadout ?? Array.Empty<Technique>();

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
