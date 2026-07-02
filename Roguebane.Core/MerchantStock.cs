namespace Roguebane.Core;

// The §12 merchant GEAR stock, rolled once per merchant node from a seed so the run stays
// reproducible: random but SCARCE across five categories — Armor / Weapons / Techniques / Minions /
// Runes. The spec locks the shape (always 3 picks per non-technique section, techniques always 5
// when present and 2nd-rarest, runes extremely rare with keystones NEVER stocked, at most 4 of the
// 5 sections per visit); the exact weighted-shuffle distribution is OPEN (§17), so the section
// weights below are placeholder-sane numbers the tuning pass owns.
public sealed class MerchantStock
{
    public IReadOnlyList<Weapon> Weapons { get; }
    public IReadOnlyList<Armor> Armor { get; }
    public IReadOnlyList<Technique> Techniques { get; }
    public IReadOnlyList<Minion> Minions { get; }
    public IReadOnlyList<Mark> Marks { get; }

    private MerchantStock(IReadOnlyList<Weapon> weapons, IReadOnlyList<Armor> armor,
        IReadOnlyList<Technique> techniques, IReadOnlyList<Minion> minions, IReadOnlyList<Mark> marks)
    {
        Weapons = weapons;
        Armor = armor;
        Techniques = techniques;
        Minions = minions;
        Marks = marks;
    }

    private const int SectionPicks = 3;   // non-technique sections stock exactly 3 (pool-capped)
    private const int TechniquePicks = 5; // techniques stock exactly 5 when the section shows

    // Placeholder section-presence weights out of 100 (OPEN §17 — most visits read Armor/Weapons/
    // Minions, techniques 2nd-rarest, runes extremely rare).
    private const int CommonWeight = 80, TechniqueWeight = 25, RuneWeight = 8;

    public static MerchantStock Roll(ulong seed,
        IReadOnlyList<Weapon> weaponPool, IReadOnlyList<Armor> armorPool,
        IReadOnlyList<Technique> techniquePool, IReadOnlyList<Minion> minionPool,
        IReadOnlyList<Mark> markPool)
    {
        var rng = new Rng(seed == 0 ? 1 : seed);

        // Which sections appear this visit (independent placeholder rolls), capped at 4 of 5 —
        // the rarest sections drop first when the cap trims.
        var armor = rng.Next(100) < CommonWeight;
        var weapons = rng.Next(100) < CommonWeight;
        var minions = rng.Next(100) < CommonWeight;
        var techniques = rng.Next(100) < TechniqueWeight;
        var runes = rng.Next(100) < RuneWeight;
        var open = (armor ? 1 : 0) + (weapons ? 1 : 0) + (minions ? 1 : 0)
                 + (techniques ? 1 : 0) + (runes ? 1 : 0);
        if (open > 4) runes = false;

        // Keystones NEVER stock (drop-only); rank filters double as the "good runes rarer" knob —
        // a rank-2 mark must survive an extra roll to appear.
        var buyableMarks = markPool.Where(m => !m.Keystone)
            .Where(m => m.Rank <= 1 || rng.Next(100) < 33).ToList();

        return new MerchantStock(
            weapons ? Pick(rng, weaponPool, SectionPicks) : System.Array.Empty<Weapon>(),
            armor ? Pick(rng, armorPool, SectionPicks) : System.Array.Empty<Armor>(),
            techniques ? Pick(rng, techniquePool, TechniquePicks) : System.Array.Empty<Technique>(),
            minions ? Pick(rng, minionPool, SectionPicks) : System.Array.Empty<Minion>(),
            runes ? Pick(rng, buyableMarks, SectionPicks) : System.Array.Empty<Mark>());
    }

    public int SectionCount =>
        (Weapons.Count > 0 ? 1 : 0) + (Armor.Count > 0 ? 1 : 0) + (Techniques.Count > 0 ? 1 : 0)
        + (Minions.Count > 0 ? 1 : 0) + (Marks.Count > 0 ? 1 : 0);

    // N distinct seeded picks (pool-capped): a partial Fisher-Yates over a copy of the pool.
    private static IReadOnlyList<T> Pick<T>(Rng rng, IReadOnlyList<T> pool, int n)
    {
        var items = pool.ToList();
        var take = System.Math.Min(n, items.Count);
        for (var i = 0; i < take; i++)
        {
            var j = i + rng.Next(items.Count - i);
            (items[i], items[j]) = (items[j], items[i]);
        }
        return items.Take(take).ToList();
    }
}
