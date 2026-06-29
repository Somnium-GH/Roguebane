namespace Roguebane.Core.Tests;

public class AttributePoolTests
{
    private static AttributePool Pool(int power = 10) =>
        new(new Dictionary<Attribute, int> { [Attribute.Power] = power });

    [Fact]
    public void AvailableIsCapacityMinusAllocated()
    {
        var pool = Pool(10);
        Assert.True(pool.TryAllocate(Attribute.Power, 4));
        Assert.Equal(6, pool.Available(Attribute.Power));
        Assert.Equal(4, pool.Allocated(Attribute.Power));
    }

    [Fact]
    public void AllocateFailsWhenInsufficientAndLeavesPoolUntouched()
    {
        var pool = Pool(10);
        pool.TryAllocate(Attribute.Power, 8);
        Assert.False(pool.TryAllocate(Attribute.Power, 3));
        Assert.Equal(2, pool.Available(Attribute.Power));
    }

    [Fact]
    public void ReleaseReturnsToPool()
    {
        var pool = Pool(10);
        pool.TryAllocate(Attribute.Power, 7);
        pool.Release(Attribute.Power, 5);
        Assert.Equal(8, pool.Available(Attribute.Power));
    }

    [Fact]
    public void ReleaseBeyondAllocationThrows()
    {
        var pool = Pool(10);
        pool.TryAllocate(Attribute.Power, 3);
        Assert.Throws<InvalidOperationException>(() => pool.Release(Attribute.Power, 4));
    }

    [Fact]
    public void UnknownAttributeReadsAsZero()
    {
        var pool = Pool(10);
        Assert.Equal(0, pool.Capacity(Attribute.Vigor));
        Assert.Equal(0, pool.Available(Attribute.Vigor));
    }
}
