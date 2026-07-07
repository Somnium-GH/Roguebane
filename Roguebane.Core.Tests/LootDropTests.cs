using Roguebane.Core.Content;

namespace Roguebane.Core.Tests;

// STATUS.md "Loot backlog" (2026-07-07, Doug-unblocked): three INDEPENDENT node-clear rolls --
// gear/technique/rune (8%), supplies (35%), summons (20%). Placeholder-blessed percentages, not
// final -- these tests pin the CURRENT odds/shape (same convention as MerchantStockTests) so a
// future tune is a deliberate, visible change, not silent drift.
public class LootDropTests
{
    private static readonly IReadOnlyList<Mark> AllMarks =
        Paths.VesselLadder.Concat(Paths.ResonanceLadder).Concat(Paths.TempestLadder)
            .Concat(Paths.ConclaveLadder).ToList();

    private static LootDrop.Result Roll(ulong seed) => LootDrop.Roll(new Rng(seed),
        Armory.All, new[] { Shops.Plate, Shops.Hide }, Techniques.All, AllMarks, Minions.All);

    [Fact]
    public void RollIsSeededAndReproducible()
    {
        for (ulong seed = 1; seed <= 20; seed++)
        {
            var a = Roll(seed);
            var b = Roll(seed);
            Assert.Equal(a.Weapon?.Id, b.Weapon?.Id);
            Assert.Equal(a.Armor?.Name, b.Armor?.Name);
            Assert.Equal(a.Technique?.Id, b.Technique?.Id);
            Assert.Equal(a.Mark?.Name, b.Mark?.Name);
            Assert.Equal(a.Supplies, b.Supplies);
            Assert.Equal(a.Summon?.Id, b.Summon?.Id);
        }
    }

    [Fact]
    public void AtMostOneGearKindDropsPerRollAndItComesFromItsOwnPool()
    {
        for (ulong seed = 1; seed <= 400; seed++)
        {
            var r = Roll(seed);
            var gearKinds = new object?[] { r.Weapon, r.Armor, r.Technique, r.Mark }.Count(x => x is not null);
            Assert.InRange(gearKinds, 0, 1); // one shared gear/technique/rune slot, not stackable

            if (r.Weapon is { } w) Assert.Contains(w, Armory.All);
            if (r.Armor is { } a) Assert.Contains(a, new[] { Shops.Plate, Shops.Hide });
            if (r.Technique is { } t) Assert.Contains(t, Techniques.All);
            if (r.Mark is { } m) Assert.Contains(m, AllMarks);
            if (r.Summon is { } s) Assert.Contains(s, Minions.All);
        }
    }

    // Odds pinned over a 400-seed sweep with a wide-enough tolerance band to survive an unrelated
    // RNG-order change while still catching a real drift in the placeholder-blessed percentages.
    [Fact]
    public void DropRatesMatchThePlaceholderBlessedPercentages()
    {
        const int seeds = 400;
        int gearHits = 0, suppliesHits = 0, summonHits = 0;
        for (ulong seed = 1; seed <= seeds; seed++)
        {
            var r = Roll(seed);
            if (r.Weapon is not null || r.Armor is not null || r.Technique is not null || r.Mark is not null) gearHits++;
            if (r.Supplies) suppliesHits++;
            if (r.Summon is not null) summonHits++;
        }

        Assert.InRange(gearHits, 15, 55);       // ~8% of 400
        Assert.InRange(suppliesHits, 105, 175); // ~35% of 400
        Assert.InRange(summonHits, 55, 115);    // ~20% of 400
    }
}
