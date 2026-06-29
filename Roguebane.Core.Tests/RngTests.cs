namespace Roguebane.Core.Tests;

// The seeded PRNG underpins determinism: same seed + same call sequence => same numbers, so any
// chance effect (evasion now, more later) keeps "same seed + inputs => same run" intact.
public class RngTests
{
    [Fact]
    public void SameSeedYieldsTheSameSequence()
    {
        var a = new Rng(12345);
        var b = new Rng(12345);
        for (var i = 0; i < 100; i++) Assert.Equal(a.Next(1000), b.Next(1000));
    }

    [Fact]
    public void DifferentSeedsDiverge()
    {
        var a = new Rng(1);
        var b = new Rng(2);
        var same = 0;
        for (var i = 0; i < 50; i++) if (a.Next(1000) == b.Next(1000)) same++;
        Assert.True(same < 10); // overwhelmingly different
    }

    [Fact]
    public void ChanceHonoursTheBounds()
    {
        var rng = new Rng(7);
        for (var i = 0; i < 20; i++)
        {
            Assert.False(rng.Chance(0));   // never
            Assert.True(rng.Chance(100));  // always
        }
    }

    [Fact]
    public void ChanceIsRoughlyCalibrated()
    {
        var rng = new Rng(99);
        var hits = 0;
        for (var i = 0; i < 10000; i++) if (rng.Chance(25)) hits++;
        Assert.InRange(hits, 2000, 3000); // ~25%
    }
}
