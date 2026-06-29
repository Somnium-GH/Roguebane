namespace Roguebane.Core;

// The rune economy: a fixed budget, and at most one Mark held per Path. You climb a
// Path one rung at a time (prereq ladder). Taking the next rung overwrites the current
// one and reclaims its Refund, so the marginal cost of a climb is Cost - Refund(below).
public sealed class RuneLoadout
{
    public int Budget { get; }
    public int Spent { get; private set; }

    private readonly Dictionary<string, Mark> _held = new();

    public RuneLoadout(int budget)
    {
        if (budget < 0) throw new ArgumentOutOfRangeException(nameof(budget));
        Budget = budget;
    }

    public int Available => Budget - Spent;

    public int CurrentRank(string path) => _held.GetValueOrDefault(path)?.Rank ?? 0;

    public Mark? Held(string path) => _held.GetValueOrDefault(path);

    public bool Has(Mark mark) => _held.GetValueOrDefault(mark.Path) == mark;

    public bool TryTake(Mark mark)
    {
        var below = _held.GetValueOrDefault(mark.Path);
        if (mark.Rank != CurrentRank(mark.Path) + 1) return false; // ladder: no skips, no prereq gaps

        var refund = below?.Refund ?? 0;
        var net = mark.Cost - refund;
        if (net > Available) return false;

        Spent += net;
        _held[mark.Path] = mark;
        return true;
    }
}
