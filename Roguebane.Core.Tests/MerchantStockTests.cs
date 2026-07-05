using Roguebane.Core.Content;

namespace Roguebane.Core.Tests;

// The §12 merchant gear stock: seeded + reproducible, at most 4 of 5 sections, 3 picks per
// non-technique section (pool-capped), 5 techniques when present, and keystones NEVER stocked.
// Section-presence weights are LOCKED [2026-07-03, Doug] too (80% Armor/Weapons/Minions, 25%
// Techniques, 8% Runes) — these tests pin both the SHAPE and the odds.
public class MerchantStockTests
{
    private static readonly IReadOnlyList<Mark> AllMarks =
        Paths.VesselLadder.Concat(Paths.ResonanceLadder).Concat(Paths.TempestLadder)
            .Concat(Paths.ConclaveLadder).ToList();

    private static MerchantStock Roll(ulong seed) => MerchantStock.Roll(seed,
        Armory.All, new[] { Shops.Plate, Shops.Hide }, Techniques.All, Minions.All, AllMarks);

    [Fact]
    public void StockIsSeededAndReproducible()
    {
        for (ulong seed = 1; seed <= 20; seed++)
        {
            var a = Roll(seed);
            var b = Roll(seed);
            Assert.Equal(a.Weapons.Select(w => w.Id), b.Weapons.Select(w => w.Id));
            Assert.Equal(a.Techniques.Select(t => t.Id), b.Techniques.Select(t => t.Id));
            Assert.Equal(a.Marks.Select(m => m.Name), b.Marks.Select(m => m.Name));
        }
    }

    [Fact]
    public void ShapeHoldsAcrossManySeeds()
    {
        var sawTechniques = false;
        var sawRunes = false;
        for (ulong seed = 1; seed <= 400; seed++)
        {
            var s = Roll(seed);
            Assert.InRange(s.SectionCount, 0, 4);                       // at most 4 of the 5 sections
            if (s.Weapons.Count > 0) Assert.InRange(s.Weapons.Count, 1, 3);
            if (s.Armor.Count > 0) Assert.InRange(s.Armor.Count, 1, 3);
            if (s.Minions.Count > 0) Assert.InRange(s.Minions.Count, 1, 3);
            if (s.Techniques.Count > 0)
            {
                sawTechniques = true;
                Assert.Equal(5, s.Techniques.Count);                    // always 5 when present
                Assert.Equal(s.Techniques.Count, s.Techniques.Select(t => t.Id).Distinct().Count());
            }
            if (s.Marks.Count > 0)
            {
                sawRunes = true;
                Assert.All(s.Marks, m => Assert.False(m.Keystone));     // keystones NEVER stock
            }
        }
        Assert.True(sawTechniques); // both rare sections do occur across the sweep
        Assert.True(sawRunes);
    }

    // §12 presence-roll weights [LOCKED 2026-07-03]: 80% Armor/Weapons/Minions, 25% Techniques,
    // 8% Runes. Pins the ODDS themselves (not just that each section CAN appear), over the same
    // 400-seed sweep -- deterministic (seeded RNG), so these bands aren't a flaky statistical test,
    // just a wide-enough tolerance around the locked weights to survive an unrelated RNG-order change.
    [Fact]
    public void SectionPresenceRateMatchesTheLockedWeights()
    {
        const int seeds = 400;
        int armorHits = 0, weaponsHits = 0, minionsHits = 0, techniquesHits = 0, runesHits = 0;
        for (ulong seed = 1; seed <= seeds; seed++)
        {
            var s = Roll(seed);
            if (s.Armor.Count > 0) armorHits++;
            if (s.Weapons.Count > 0) weaponsHits++;
            if (s.Minions.Count > 0) minionsHits++;
            if (s.Techniques.Count > 0) techniquesHits++;
            if (s.Marks.Count > 0) runesHits++;
        }

        Assert.InRange(armorHits, 280, 360);      // ~80% of 400
        Assert.InRange(weaponsHits, 280, 360);     // ~80% of 400
        Assert.InRange(minionsHits, 280, 360);     // ~80% of 400
        Assert.InRange(techniquesHits, 70, 130);   // ~25% of 400
        Assert.InRange(runesHits, 15, 50);         // ~8% of 400
    }
}
