namespace Roguebane.Core;

// A seeded, deterministic PRNG (xorshift64*). Same seed + same call sequence => same numbers, so
// chance effects (evasion, future crits/loot) keep the simulation reproducible: same seed + inputs
// reproduce a run. Integer-only, engine-agnostic — lives in Core like everything else.
public sealed class Rng
{
    private ulong _state;

    public Rng(ulong seed) => _state = seed == 0 ? 0x9E3779B97F4A7C15ul : seed;

    private uint NextBits()
    {
        var x = _state;
        x ^= x >> 12;
        x ^= x << 25;
        x ^= x >> 27;
        _state = x;
        return (uint)((x * 0x2545F4914F6CDD1Dul) >> 32);
    }

    // A roll in [0, maxExclusive). Slight modulo bias is fine at the small ranges combat uses.
    public int Next(int maxExclusive) => maxExclusive <= 0 ? 0 : (int)(NextBits() % (uint)maxExclusive);

    // True with the given percent probability (0 => never, >=100 => always).
    public bool Chance(int percent) => percent > 0 && (percent >= 100 || Next(100) < percent);
}
