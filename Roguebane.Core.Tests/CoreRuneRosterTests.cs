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
        // design/05 cards need a title, an archetype tagline, a flavor pitch, and DISPLAY-only Core Effect text
        // (name + blurb) per core — the card binds render these (the Core EFFECT is NOT implemented, §11).
        foreach (var c in CoreRunes.Roster)
        {
            Assert.False(string.IsNullOrEmpty(c.Archetype), $"{c.Id} archetype");
            Assert.False(string.IsNullOrEmpty(c.Flavor), $"{c.Id} flavor");
            Assert.False(string.IsNullOrEmpty(c.CoreEffectName), $"{c.Id} Core Effect name");
            Assert.False(string.IsNullOrEmpty(c.CoreEffectDesc), $"{c.Id} Core Effect desc");
            Assert.Equal(char.ToUpperInvariant(c.Id[0]), c.Title[0]); // "grunt" -> "Grunt"
        }
        Assert.Equal("THE WALL", CoreRunes.Warden.Archetype);
        Assert.Equal("Hollow Vessel", CoreRunes.Grunt.CoreEffectName); // the POC keystone (§11)
    }

    [Fact]
    public void NewBuildOffersTheWholeRoster()
    {
        Assert.Equal(CoreRunes.Roster.Count, Sessions.NewBuild().CoreRuneCount);
    }

    [Fact]
    public void EveryTechniqueAndMinionCarriesDisplayCopy()
    {
        // Card DESCRIPTIONS are display data (design/01) like the Core Effect copy: every palette technique
        // (+ the opt-in shield/heal content) and every minion must ship copy, and {power} must resolve
        // so the rendered text never contradicts the data.
        var techs = Techniques.All.Concat(new[] { Techniques.Bandage, Techniques.Stoneskin });
        foreach (var t in techs)
        {
            Assert.False(string.IsNullOrEmpty(t.Desc), $"{t.Id} desc");
            Assert.DoesNotContain("{", t.DescText);
        }
        foreach (var m in Minions.All)
        {
            Assert.False(string.IsNullOrEmpty(m.Desc), $"{m.Id} desc");
            Assert.DoesNotContain("{", m.DescText);
        }
    }

    [Fact]
    public void SummonerCoreEffectRefundsSummonsForSurvivorsOnRedeploy()
    {
        // The first REAL Core Effect [LOCKED §11]: on Redeploy each SURVIVING minion refunds its
        // Summons; other cores refund nothing. The kit's two summons spend at assembly.
        var summoner = Forge.Embark(Races.Human, CoreRunes.Summoner, CoreRunes.Summoner.NewLoadout(),
            CoreRunes.Summoner.Kit, Maps.StandardLeg());
        var spentAtStart = summoner.MaxSummons - summoner.Summons;
        Assert.Equal(summoner.MinionCount, spentAtStart); // each fielded kit minion spent one Summons
        Assert.True(spentAtStart >= 1);                   // the Binder fields at least the Skeleton

        foreach (var t in summoner.Equipment) summoner.Toggle(t);
        summoner.SetAuto(true);
        summoner.Enter("a2");
        var guard = 0;
        while (summoner.State == ExpeditionState.Fighting && guard++ < 10000)
        {
            if (summoner.Enemy is { } foe) foreach (var t in summoner.Equipment) if (summoner.IsActive(t)) summoner.Aim(t, foe);
            summoner.Tick();
        }
        Assert.Equal(ExpeditionState.Cleared, summoner.State);
        var before = summoner.Summons;
        var survivors = summoner.MinionCount;
        summoner.Redeploy();
        Assert.Equal(Math.Min(summoner.MaxSummons, before + survivors), summoner.Summons); // refunded, capped

        // A non-summoner core refunds nothing on the same path.
        Assert.False(CoreRunes.Grunt.CoreEffectRefundsSummons);
    }
}
