namespace Roguebane.Core.Tests;

// Phase 3 shields: a regenerating pool of 1-damage layers, the outermost mitigation. Foundation for
// the block/stoneskin/parry sources that replace the old flat CON block.
public class ShieldPoolTests
{
    [Fact]
    public void StartsFullAtItsLayerCount()
    {
        var pool = new ShieldPool(layers: 3, regenEvery: 10);
        Assert.Equal(3, pool.Points);
        Assert.Equal(3, pool.Layers);
    }

    [Fact]
    public void EachLayerAbsorbsOneDamageAndIsConsumed()
    {
        var pool = new ShieldPool(3, 10);
        Assert.Equal(0, pool.Absorb(2)); // 2 of 3 layers eat it, nothing spills
        Assert.Equal(1, pool.Points);
    }

    [Fact]
    public void DamagePastThePoolSpillsTheRemainder()
    {
        var pool = new ShieldPool(2, 10);
        Assert.Equal(3, pool.Absorb(5)); // 2 absorbed, 3 spills onward
        Assert.Equal(0, pool.Points);
    }

    [Fact]
    public void RegeneratesOneLayerEveryCadenceUpToTheCount()
    {
        var pool = new ShieldPool(2, regenEvery: 5);
        pool.Absorb(2); // drained to 0
        for (var i = 0; i < 4; i++) pool.Tick();
        Assert.Equal(0, pool.Points);  // not yet
        pool.Tick();                    // 5th tick -> +1
        Assert.Equal(1, pool.Points);
        for (var i = 0; i < 5; i++) pool.Tick();
        Assert.Equal(2, pool.Points);  // back to full
        for (var i = 0; i < 10; i++) pool.Tick();
        Assert.Equal(2, pool.Points);  // capped at the layer count
    }

    [Fact]
    public void RegenEveryZeroNeverRegenerates()
    {
        var pool = new ShieldPool(2, regenEvery: 0);
        pool.Absorb(2);
        for (var i = 0; i < 50; i++) pool.Tick();
        Assert.Equal(0, pool.Points);
    }

    [Fact]
    public void RefillRestoresToFull()
    {
        var pool = new ShieldPool(3, 10);
        pool.Absorb(3);
        pool.Refill();
        Assert.Equal(3, pool.Points);
    }
}
