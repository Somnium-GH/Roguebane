namespace Roguebane.Core;

// A chassis is data: the body you socket into. Base attribute capacities, a rune budget,
// and a per-rung rune discount. The thesis lives in the tension between these: a fat-budget,
// cheap-rune chassis can climb to a keystone it was never built around.
public sealed record Chassis(
    string Id,
    IReadOnlyDictionary<Attribute, int> Base,
    int RuneBudget,
    int RuneDiscount = 0)
{
    public Entity NewBody() => new(new AttributePool(Base));

    public RuneLoadout NewLoadout() => new(RuneBudget, RuneDiscount);
}
