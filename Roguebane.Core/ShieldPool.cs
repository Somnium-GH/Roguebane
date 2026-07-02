namespace Roguebane.Core;

// The Phase 3 SHIELDS model (supersedes the old flat CON block): a shield SOURCE maintains a pool of
// SHIELD POINTS (FTL layers). Each point absorbs 1 incoming damage and is CONSUMED on hit; points
// REGENERATE one at a time on a timer up to the source's layer count. Pure + engine-agnostic: the
// source sets the layer count and regen cadence (CON/rune scaling is folded into regenEvery by the
// caller, so this stays a plain counter), and the combat tick drives Tick()/Absorb().
public sealed class ShieldPool
{
    private readonly int _max;
    private readonly int _regenEvery; // ticks between regenerating one layer (<=0 => never regen)
    private int _points;
    private int _tick;

    public ShieldPool(int layers, int regenEvery, int start = -1)
    {
        if (layers < 0) throw new ArgumentOutOfRangeException(nameof(layers));
        _max = layers;
        _regenEvery = regenEvery;
        _points = start < 0 ? layers : Math.Min(start, layers);
    }

    public int Points => _points;
    public int Layers => _max;

    // Progress toward the NEXT regenerated layer, 0..1 (0 when full or the source never regens) —
    // the shield bar's per-pip regen readout.
    public float RegenProgress => _regenEvery <= 0 || _points >= _max
        ? 0f : (float)(_tick % _regenEvery) / _regenEvery;

    // Absorb incoming damage: each standing point eats 1, and is consumed. Returns the UNABSORBED
    // remainder to spill onto armor / the part / HP (the shield is the outermost mitigation layer).
    public int Absorb(int damage)
    {
        if (damage < 0) throw new ArgumentOutOfRangeException(nameof(damage));
        var eaten = Math.Min(_points, damage);
        _points -= eaten;
        return damage - eaten;
    }

    // One combat tick: regenerate a single layer every regenEvery ticks, up to the source's count.
    public void Tick()
    {
        if (_regenEvery <= 0 || _points >= _max) return;
        if (++_tick % _regenEvery == 0) _points = Math.Min(_max, _points + 1);
    }

    // Refill to full and clear the regen timer (e.g. raising the source, or an out-of-combat reset).
    public void Refill()
    {
        _points = _max;
        _tick = 0;
    }
}
