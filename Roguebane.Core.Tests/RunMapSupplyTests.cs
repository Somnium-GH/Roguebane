using Roguebane.Core;
using Roguebane.Core.Content;

namespace Roguebane.Core.Tests;

// MaxSupplies pins the starting jump budget so the shell can show remaining/max; it must not move
// as supplies are spent.
public class RunMapSupplyTests
{
    [Fact]
    public void MaxSuppliesHoldsTheStartingBudgetAsSuppliesAreSpent()
    {
        var map = Maps.StandardLeg();
        var start = map.Supplies;
        Assert.Equal(start, map.MaxSupplies);
        Assert.True(start > 0);

        var to = map.Options[0].Id;
        map.MoveTo(to);

        Assert.Equal(start - 1, map.Supplies);
        Assert.Equal(start, map.MaxSupplies); // max is fixed
    }
}
