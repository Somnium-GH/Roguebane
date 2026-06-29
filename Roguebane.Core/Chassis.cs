namespace Roguebane.Core;

// A chassis is data: the body you socket into. Its parts (Head, Chest, Arms x2, Legs x2) carry
// the stat shares, plus a rune budget and a per-rung discount. The thesis lives in the tension
// between these: a fat-budget, cheap-rune chassis can climb to a keystone it was never built for.
public sealed record Chassis(
    string Id,
    IReadOnlyList<BodyPart> BodyParts,
    int RuneBudget,
    int RuneDiscount = 0)
{
    public Body NewBody()
    {
        var body = new Body();
        foreach (var part in BodyParts) body.Add(part);
        return body;
    }

    public RuneLoadout NewLoadout() => new(RuneBudget, RuneDiscount);
}
