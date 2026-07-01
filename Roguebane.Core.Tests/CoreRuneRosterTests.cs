using Roguebane.Core.Content;

namespace Roguebane.Core.Tests;

// G3: the cores exist as data with distinct ids + display identities, selectable at New Run. Their
// identity is now budget/bays/equipment — attrs are the Race's (§7), so no stat-identity assertions.
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
    public void NewBuildOffersTheWholeRoster()
    {
        Assert.Equal(CoreRunes.Roster.Count, Sessions.NewBuild().CoreRuneCount);
    }
}
