using Roguebane.Core;
using Roguebane.Core.Content;

namespace Roguebane.Core.Tests;

// The Stash is the persistent run economy (gold / gear pack). These pin its invariants directly —
// the spend guards and the carry-pack add/remove — independent of the Expedition.
public class StashTests
{
    [Fact]
    public void TrySpendDeductsOnlyWhenAffordable()
    {
        var s = new Stash(gold: 5);
        Assert.True(s.TrySpend(3));
        Assert.Equal(2, s.Gold);
        Assert.False(s.TrySpend(5)); // can't afford
        Assert.Equal(2, s.Gold);     // unchanged on failure
    }

    [Fact]
    public void AddGoldAndTrySpendRejectNegatives()
    {
        var s = new Stash();
        Assert.Throws<ArgumentOutOfRangeException>(() => s.AddGold(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => s.TrySpend(-1));
    }

    [Fact]
    public void GearPackCarriesAndRemoves()
    {
        var s = new Stash();
        s.AddWeapon(Armory.Dagger);
        s.AddArmor(Shops.Plate);
        Assert.True(s.HasWeapon(Armory.Dagger));
        Assert.True(s.HasArmor(Shops.Plate));

        Assert.True(s.RemoveWeapon(Armory.Dagger));
        Assert.False(s.HasWeapon(Armory.Dagger));
        Assert.False(s.RemoveWeapon(Armory.Dagger)); // already gone
    }
}
