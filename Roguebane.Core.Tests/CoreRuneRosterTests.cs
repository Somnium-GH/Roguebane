using Roguebane.Core.Content;

namespace Roguebane.Core.Tests;

// G3: the five chassis exist as data with legible, distinct stat identities, selectable at New Run.
public class CoreRuneRosterTests
{
    [Fact]
    public void RosterCoresAreDistinct()
    {
        var ids = CoreRunes.Roster.Select(c => c.Id).ToList();
        Assert.True(ids.Count >= 6);                            // grunt/warden/adept/summoner/reaver/ranger
        Assert.Equal(ids.Count, ids.Distinct().Count());       // no dupes
        Assert.Contains("ranger", ids);                        // the data-only 6th core
    }

    [Fact]
    public void EveryCoreRuneCarriesADisplayIdentity()
    {
        // design/05 cards need a title, an archetype tagline, and a flavor pitch per core.
        foreach (var c in CoreRunes.Roster)
        {
            Assert.False(string.IsNullOrEmpty(c.Archetype), $"{c.Id} archetype");
            Assert.False(string.IsNullOrEmpty(c.Flavor), $"{c.Id} flavor");
            Assert.Equal(char.ToUpperInvariant(c.Id[0]), c.Title[0]); // "grunt" -> "Grunt"
        }
        Assert.Equal("THE WALL", CoreRunes.Warden.Archetype);
        Assert.Equal("Grunt", CoreRunes.Grunt.Title);
    }

    [Fact]
    public void EveryCoreRuneMintsAStandardSixPartBody()
    {
        foreach (var chassis in CoreRunes.Roster)
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
        var warden = CoreRunes.Warden.NewBody();
        var reaver = CoreRunes.Reaver.NewBody();
        var summoner = CoreRunes.Summoner.NewBody();

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
        Assert.Equal(CoreRunes.Roster.Count, Sessions.NewBuild().CoreRuneCount);
    }
}
