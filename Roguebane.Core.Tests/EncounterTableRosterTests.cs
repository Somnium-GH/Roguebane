using Roguebane.Core.Content;

namespace Roguebane.Core.Tests;

// CHUNK D item 3 (STATUS.md): skirmish/resource-hold now pull the encounter's foe from the real
// FOES.md T1 roster (Wraith/Ogre, gear + Foe Effects wired) via a seeded pick, instead of the generic
// Foes.Armed stand-in (FoeArmingTests/SiegeFigureTests still cover that helper directly -- it's a bare
// arming fixture, not content). Which-foe-where is deliberately unpinned (FLAGGED in Sieges.cs) --
// these tests prove the pick is REPRODUCIBLE and drawn from the roster, not any specific ordering.
public class EncounterTableRosterTests
{
    [Fact]
    public void SkirmishAlwaysPicksARosterFoeNeverTheGenericStandIn()
    {
        for (ulong seed = 0; seed < 20; seed++)
        {
            var enc = Sieges.SkirmishPoint("node", seed);
            Assert.True(enc.Enemy!.Figure is "wraith" or "ogre");
        }
    }

    [Fact]
    public void TheSameSeedAlwaysPicksTheSameFoe()
    {
        var a = Sieges.SkirmishPoint("node", seed: 42);
        var b = Sieges.SkirmishPoint("node", seed: 42);
        Assert.Equal(a.Enemy!.Figure, b.Enemy!.Figure);
        Assert.Equal(a.Enemy!.MaxHp, b.Enemy!.MaxHp);
    }

    [Fact]
    public void DifferentSeedsCanPickDifferentFoes()
    {
        var figures = new HashSet<string>();
        for (ulong seed = 0; seed < 20; seed++)
            figures.Add(Sieges.SkirmishPoint("node", seed).Enemy!.Figure);

        Assert.True(figures.Count > 1); // the pool has 2 entries -- 20 draws should hit both
    }

    [Fact]
    public void ResourceHoldPicksTheSameRosterAtHigherHpThanSkirmish()
    {
        var skirmish = Sieges.SkirmishPoint("node", seed: 7);
        var hold = Sieges.ResourceHoldPoint("node", seed: 7);

        Assert.Equal(skirmish.Enemy!.Figure, hold.Enemy!.Figure); // same seed -> same pool slot
        Assert.True(hold.Enemy!.MaxHp > skirmish.Enemy!.MaxHp); // "tougher" -- T2 isn't built, so this is the honest stand-in
    }

    [Fact]
    public void MapsEncounterForRoutesSkirmishAndResourceHoldThroughTheRosterPool()
    {
        var skirmishNode = new MapNode("a2", NodeType.Skirmish, "b").At(1, 2);
        var holdNode = new MapNode("a1", NodeType.ResourceHold, "b").At(1, 0);

        var skirmish = Maps.EncounterFor(skirmishNode, supportBank: 0, seed: 7);
        var hold = Maps.EncounterFor(holdNode, supportBank: 0, seed: 7);

        Assert.True(skirmish.Enemy!.Figure is "wraith" or "ogre");
        Assert.True(hold.Enemy!.MaxHp > skirmish.Enemy!.MaxHp);
    }

    [Fact]
    public void CastleStaysTheFixedBossUnaffectedByTheRosterPool()
    {
        var castleNode = new MapNode("castle", NodeType.Castle).At(4, 1);
        var enc = Maps.EncounterFor(castleNode, supportBank: 2, seed: 7);

        Assert.Equal("ogre", enc.Enemy!.Figure);
        Assert.Equal(40, enc.Enemy!.MaxHp);
    }
}
