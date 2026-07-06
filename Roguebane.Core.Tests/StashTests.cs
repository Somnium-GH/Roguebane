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

    // HIGH PRIORITY bug #1 fix: the GEAR tab's identity roster is a SEPARATE ledger from the pack
    // (_weapons/_armor above) so a piece's position never depends on which collection currently
    // holds it. Removing from the pack (equipping) must NOT remove it from the roster.
    [Fact]
    public void RosterOrderSurvivesLeavingThePack()
    {
        var s = new Stash();
        s.AddWeapon(Armory.Dagger);
        s.AddWeapon(Armory.Sword);
        Assert.Equal(new[] { Armory.Dagger, Armory.Sword }, s.WeaponRoster);

        s.RemoveWeapon(Armory.Dagger); // e.g. Gearing equipping it onto the Body
        Assert.Equal(new[] { Armory.Dagger, Armory.Sword }, s.WeaponRoster); // roster unchanged
        Assert.False(s.HasWeapon(Armory.Dagger)); // but it did leave the pack
    }

    // Records compare by VALUE, so two otherwise-identical pieces (the seeded duplicate-armor case)
    // must still occupy two distinct roster slots, not collapse into one.
    [Fact]
    public void RosterTracksEqualValuedDuplicatesAsDistinctEntries()
    {
        var s = new Stash();
        var plateCopy = Shops.Plate with { }; // same values, different instance
        s.AddArmor(Shops.Plate);
        s.AddArmor(plateCopy);

        Assert.Equal(2, s.ArmorRoster.Count);
        Assert.Same(Shops.Plate, s.ArmorRoster[0]);
        Assert.Same(plateCopy, s.ArmorRoster[1]);
    }

    [Fact]
    public void TrackOwnedIsIdempotentByReference()
    {
        var s = new Stash();
        s.TrackOwned(Armory.Dagger);
        s.TrackOwned(Armory.Dagger);
        s.AddWeapon(Armory.Dagger); // AddWeapon also tracks -- must not add a second roster entry
        Assert.Single(s.WeaponRoster);
    }
}
