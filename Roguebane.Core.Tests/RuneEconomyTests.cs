using Roguebane.Core.Content;

namespace Roguebane.Core.Tests;

public class RuneEconomyTests
{
    [Fact]
    public void TakingFirstRungChargesItsCost()
    {
        var runes = new RuneLoadout(budget: 20);
        Assert.True(runes.TryTake(Paths.VesselI));
        Assert.Equal(4, runes.Spent);
        Assert.Equal(1, runes.CurrentRank(Paths.Vessel));
    }

    [Fact]
    public void CannotSkipRungs()
    {
        var runes = new RuneLoadout(budget: 20);
        Assert.False(runes.TryTake(Paths.VesselII)); // rank 2 with nothing held
        Assert.False(runes.TryTake(Paths.HollowVessel));
        Assert.Equal(0, runes.Spent);
    }

    [Fact]
    public void ClimbOverwritesAndPartiallyRefundsTheRungBelow()
    {
        var runes = new RuneLoadout(budget: 20);
        runes.TryTake(Paths.VesselI);              // spend 4
        Assert.True(runes.TryTake(Paths.VesselII)); // +6 cost - 2 refund = net 4

        Assert.Equal(8, runes.Spent);
        Assert.Equal(2, runes.CurrentRank(Paths.Vessel));
        Assert.False(runes.Has(Paths.VesselI)); // overwritten
        Assert.True(runes.Has(Paths.VesselII));
    }

    [Fact]
    public void ClimbingToKeystoneCostsLessThanSumOfRungs()
    {
        var runes = new RuneLoadout(budget: 20);
        runes.TryTake(Paths.VesselI);
        runes.TryTake(Paths.VesselII);
        Assert.True(runes.TryTake(Paths.HollowVessel));

        // raw rung sum 4+6+8 = 18; refunds 2+3 reclaimed => 13 net
        Assert.Equal(13, runes.Spent);
        Assert.True(runes.Has(Paths.HollowVessel));
        Assert.True(runes.Held(Paths.Vessel)!.Keystone);
    }

    [Fact]
    public void TightBudgetBlocksTheClimb()
    {
        var runes = new RuneLoadout(budget: 7);
        Assert.True(runes.TryTake(Paths.VesselI));  // spend 4, 3 left
        Assert.False(runes.TryTake(Paths.VesselII)); // net 4 > 3 available
        Assert.Equal(4, runes.Spent);
        Assert.Equal(1, runes.CurrentRank(Paths.Vessel));
    }
}
