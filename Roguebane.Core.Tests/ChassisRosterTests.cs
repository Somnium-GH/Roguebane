using Roguebane.Core.Content;

namespace Roguebane.Core.Tests;

// G3: the five chassis exist as data with legible, distinct stat identities, selectable at New Run.
public class ChassisRosterTests
{
    [Fact]
    public void RosterHasFiveDistinctChassis()
    {
        var ids = Chassrium.Roster.Select(c => c.Id).ToList();
        Assert.Equal(5, ids.Count);
        Assert.Equal(ids.Count, ids.Distinct().Count());
    }

    [Fact]
    public void EveryChassisCarriesADisplayIdentity()
    {
        // design/05 cards need a title, an archetype tagline, and a flavor pitch per core.
        foreach (var c in Chassrium.Roster)
        {
            Assert.False(string.IsNullOrEmpty(c.Archetype), $"{c.Id} archetype");
            Assert.False(string.IsNullOrEmpty(c.Flavor), $"{c.Id} flavor");
            Assert.Equal(char.ToUpperInvariant(c.Id[0]), c.Title[0]); // "grunt" -> "Grunt"
        }
        Assert.Equal("THE WALL", Chassrium.Warden.Archetype);
        Assert.Equal("Grunt", Chassrium.Grunt.Title);
    }

    [Fact]
    public void EveryChassisMintsAStandardSixPartBody()
    {
        foreach (var chassis in Chassrium.Roster)
        {
            var body = chassis.NewBody();
            Assert.Equal(6, body.Parts.Count); // Head, Chest, Arms x2, Legs x2
            foreach (var stat in new[] { Stat.Str, Stat.Int, Stat.Dex, Stat.Con })
                Assert.True(body.Capacity(stat) > 0, $"{chassis.Id} has no {stat}");
        }
    }

    [Fact]
    public void IdentitiesAreLegibleInTheStatBases()
    {
        var warden = Chassrium.Warden.NewBody();
        var reaver = Chassrium.Reaver.NewBody();
        var summoner = Chassrium.Summoner.NewBody();

        // The Wall out-tanks the glass-cannon Duelist.
        Assert.True(warden.Capacity(Stat.Con) > reaver.Capacity(Stat.Con));
        // The Duelist out-strikes the Wall on both offensive stats.
        Assert.True(reaver.Capacity(Stat.Str) > warden.Capacity(Stat.Str));
        Assert.True(reaver.Capacity(Stat.Dex) > warden.Capacity(Stat.Dex));
        // The Binder is INT-led like a caster.
        Assert.True(summoner.Capacity(Stat.Int) > summoner.Capacity(Stat.Str));
    }

    [Fact]
    public void NewBuildOffersTheWholeRoster()
    {
        Assert.Equal(Chassrium.Roster.Count, Sessions.NewBuild().ChassisCount);
    }
}
