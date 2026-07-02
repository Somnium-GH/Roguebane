using Roguebane.Core.Content;

namespace Roguebane.Core.Tests;

// G4 (map) + G5 (war-party race): branching traversal, supplies, fog, support banking, two-way race.
public class CityMapTests
{
    [Fact]
    public void MovingSpendsASupplyAndAdvancesTheWarParty()
    {
        var map = Maps.StandardLeg();
        var supplies = map.Supplies;
        var march = map.WarPartyDistance;

        Assert.True(map.MoveTo("a1"));

        Assert.Equal("a1", map.CurrentId);
        Assert.Equal(supplies - 1, map.Supplies);
        Assert.Equal(march - 1, map.WarPartyDistance);
    }

    [Fact]
    public void NodesCarryChartCoordsForTheGraphRender()
    {
        var map = Maps.StandardLeg();
        Assert.Equal(7, map.Nodes.Count); // the whole chart is exposed, in declared order

        var camp = map.Node("camp");
        var castle = map.Node("castle");
        Assert.Equal(0, camp.Col);   // camp is the chart's left edge
        Assert.True(castle.Col > camp.Col); // the castle sits deeper along the march
    }

    [Fact]
    public void MarchLengthIsTheConstantWarPartyTrackScale()
    {
        var map = Maps.StandardLeg();
        var march = map.MarchLength;
        Assert.Equal(march, map.WarPartyDistance); // starts full

        map.MoveTo("a1");
        Assert.Equal(march, map.MarchLength);          // the track scale is fixed
        Assert.Equal(march - 1, map.WarPartyDistance); // the marker has closed one step
    }

    [Fact]
    public void OnlyChartedNeighboursAreReachable()
    {
        var map = Maps.StandardLeg();
        Assert.False(map.MoveTo("castle")); // not linked from camp
        Assert.False(map.MoveTo("b"));      // two jumps away
        Assert.True(map.MoveTo("a2"));      // a charted link
    }

    [Fact]
    public void MovementIsAnyDirectionAlongTheEdges()
    {
        var map = Maps.StandardLeg();
        Assert.True(map.MoveTo("a2"));        // camp -> a2 (forward)
        Assert.Contains(map.Options, n => n.Id == "camp"); // camp is offered again (backtrack)
        Assert.True(map.MoveTo("camp"));      // a2 -> camp (backward along the same edge)
        Assert.Equal("camp", map.CurrentId);
    }

    [Fact]
    public void ResourceHoldsBankSupportForTheCastle()
    {
        var map = Maps.StandardLeg();
        Assert.Equal(0, map.SupportBank);
        map.MoveTo("a1"); // a ResourceHold
        Assert.Equal(1, map.SupportBank);
    }

    [Fact]
    public void FogShowsHoldsAndCastleAfarButKeepsDistantSkirmishesHidden()
    {
        var map = Maps.StandardLeg();
        // From camp: a1 is a hold (visible), a2 is an adjacent skirmish (adjacency resolves it).
        Assert.Equal(NodeType.ResourceHold, map.Sees(map.Node("a1")));
        Assert.Equal(NodeType.Skirmish, map.Sees(map.Node("a2")));
        // The castle reads from afar; a distant merchant stays fogged until one jump out.
        Assert.Equal(NodeType.Castle, map.Sees(map.Node("castle")));
        Assert.Equal(NodeType.Unknown, map.Sees(map.Node("b")));
    }

    [Fact]
    public void MerchantResolvesOneJumpOut()
    {
        var map = Maps.StandardLeg();
        map.MoveTo("a2"); // now adjacent to b (a Merchant)
        Assert.Equal(NodeType.Merchant, map.Sees(map.Node("b")));
    }

    [Fact]
    public void ReachingTheCastleCracksItAndWinsTheLeg()
    {
        var map = Maps.StandardLeg();
        Assert.True(map.MoveTo("a2"));
        Assert.True(map.MoveTo("b"));
        Assert.True(map.MoveTo("c1"));
        Assert.True(map.MoveTo("castle"));
        Assert.Equal(CityMapOutcome.CastleCracked, map.Outcome);
        Assert.False(map.MoveTo("castle")); // the march is over
    }

    [Fact]
    public void TheWarPartyReachingCampOverrunsTheRun()
    {
        // marchLength 2: the war party arrives before the player can crack the castle.
        var nodes = new[]
        {
            new MapNode("camp", NodeType.Skirmish, "x"),
            new MapNode("x", NodeType.Skirmish, "castle"),
            new MapNode("castle", NodeType.Castle),
        };
        var map = new CityMap(nodes, "camp", supplies: 8, marchLength: 2);

        Assert.True(map.MoveTo("x"));        // war party 2 -> 1
        Assert.Equal(CityMapOutcome.Marching, map.Outcome);
        Assert.True(map.MoveTo("castle"));   // war party 1 -> 0 first: overrun wins the race
        Assert.Equal(CityMapOutcome.Overrun, map.Outcome);
    }

    [Fact]
    public void RunningOutOfSuppliesShortOfTheCastleIsALoss()
    {
        var nodes = new[]
        {
            new MapNode("camp", NodeType.Skirmish, "x"),
            new MapNode("x", NodeType.Skirmish, "castle"),
            new MapNode("castle", NodeType.Castle),
        };
        var map = new CityMap(nodes, "camp", supplies: 1, marchLength: 9);

        Assert.True(map.MoveTo("x")); // last supply spent, not at the castle
        Assert.Equal(CityMapOutcome.Overrun, map.Outcome);
    }

    // 2026-07-02 directive: the origin is CAMP — safe ground, always known, never a fight.
    [Fact]
    public void CampIsItsOwnTypeAlwaysSeenAndNeverAFight()
    {
        var map = Maps.StandardLeg();
        Assert.Equal(NodeType.Camp, map.Node("camp").Type);
        Assert.Equal(NodeType.Camp, map.Sees(map.Node("camp"))); // never fogged, never "skirmish"

        var exp = Sessions.Expedition();
        foreach (var t in exp.Equipment) exp.Toggle(t); // arm the kit (no auto-arm by design)
        exp.SetAuto(true);
        exp.Enter("a1");                       // out to a fight...
        var guard = 0;
        while (exp.State == ExpeditionState.Fighting && guard++ < 10000)
        {
            if (exp.Enemy is { } foe) foreach (var t in exp.Equipment) if (exp.IsActive(t)) exp.Aim(t, foe);
            exp.Tick();
        }
        exp.Redeploy();
        Assert.True(exp.Enter("camp"));        // ...and back home
        Assert.Equal(ExpeditionState.Choosing, exp.State); // no battle spawned on camp
    }
}
