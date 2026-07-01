using Roguebane.Core.Content;

namespace Roguebane.Core.Tests;

public class BuildSessionTests
{
    private static BuildSession New() => Sessions.NewBuild();

    [Fact]
    public void RosterExposesTheWholeLineUpForTheCoreGrid()
    {
        var build = New();
        Assert.Equal(CoreRunes.Roster.Count, build.Roster.Count);
        Assert.Same(build.CoreRune, build.Roster[build.CoreRuneIndex]);
    }

    [Fact]
    public void CyclingCoreRuneWrapsAndResetsRunes()
    {
        var build = New();
        var roster = CoreRunes.Roster;
        Assert.Same(roster[0], build.CoreRune);

        build.CycleCoreRune(1);
        Assert.Same(roster[1], build.CoreRune);
        Assert.Equal(roster[1].RuneBudget, build.Runes.Available); // fresh budget

        build.CycleCoreRune(-1); // back to the first
        Assert.Same(roster[0], build.CoreRune);

        build.CycleCoreRune(-1); // wraps to the last
        Assert.Same(roster[^1], build.CoreRune);
    }

    [Fact]
    public void CyclingRaceSwapsBodyAttrsAndKeepsTheCoreBudget()
    {
        var build = New();
        var races = Races.Roster;
        Assert.Same(races[0], build.Race);               // Human first
        var budgetBefore = build.Runes.Available;
        var conBefore = build.Preview().Capacity(Stat.Con);

        build.CycleRace(1);
        Assert.Same(races[1], build.Race);               // Elf
        Assert.Equal(build.Runes.Available, budgetBefore);        // core budget untouched by a race swap
        Assert.NotEqual(conBefore, build.Preview().Capacity(Stat.Con)); // Elf con 2 vs Human con 3

        build.CycleRace(-1); // wraps back to Human
        Assert.Same(races[0], build.Race);
    }

    [Fact]
    public void ClimbingAPathSpendsBudgetAndStopsAtTheKeystone()
    {
        var build = New(); // Grunt: budget 24, discount 1
        Assert.True(build.Climb(Paths.VesselLadder));  // VesselI
        Assert.True(build.Climb(Paths.VesselLadder));  // VesselII
        Assert.True(build.Climb(Paths.VesselLadder));  // Hollow Vessel
        Assert.False(build.Climb(Paths.VesselLadder)); // ladder exhausted

        Assert.True(build.Runes.Has(Paths.HollowVessel));
    }

    [Fact]
    public void PreviewFoldsRuneGrantsIntoTheBody()
    {
        var build = New();
        var baseCon = build.Preview().Capacity(Stat.Con);
        foreach (var _ in Paths.VesselLadder) build.Climb(Paths.VesselLadder);

        Assert.Equal(baseCon + 6, build.Preview().Capacity(Stat.Con));
    }

    [Fact]
    public void ToggleBuildsTheLoadoutInPaletteOrder()
    {
        var build = New(); // Grunt kit pre-seeds jab + brace
        build.Toggle(Techniques.Drain);
        build.Toggle(Techniques.Lunge);

        Assert.True(build.IsSelected(Techniques.Jab)); // kit
        // palette order (jab, cleave, lunge, ember, drain, brace, bandage); the Grunt kit adds bandage
        Assert.Equal(new[] { "jab", "lunge", "drain", "brace", "bandage" }, build.Equipment.Select(t => t.Id));

        build.Toggle(Techniques.Jab); // a kit item can still be dropped
        Assert.False(build.IsSelected(Techniques.Jab));
    }

    [Fact]
    public void LaunchMintsTheChosenBodyIntoARun()
    {
        var build = New(); // kit: jab, brace
        build.Climb(Paths.VesselLadder);
        build.Toggle(Techniques.Lunge);

        var run = Sieges.StandardRun();
        var session = build.Launch(run);

        Assert.Equal(SessionState.Fighting, session.State);
        Assert.Equal(4, session.Equipment.Count); // kit (jab, brace, bandage) + lunge
        Assert.Equal(3, session.Run.Nodes.Count); // cp1, cp2, castle
    }

    [Fact]
    public void EmbarkDropsTheChosenBodyIntoTheRealLoop()
    {
        var build = New();

        var exp = build.Embark(Maps.StandardLeg(autoResolveCastle: false));

        Assert.Equal(ExpeditionState.Choosing, exp.State);
        Assert.Contains(exp.Equipment, t => t.Id == "jab"); // shipped in the kit, no pick needed
        Assert.Equal("camp", exp.Map.CurrentId);
    }

    [Fact]
    public void RedeploySendsTheChosenBodyDownTheCampaignSpine()
    {
        var build = New();

        var campaign = build.Redeploy(Maps.StandardLegs(3));

        Assert.Equal(CampaignState.Redeploying, campaign.State);
        Assert.Equal(3, campaign.LegCount);
        Assert.Equal(0, campaign.LegIndex);
        Assert.Contains(campaign.Current.Equipment, t => t.Id == "jab"); // kit
    }

    // The launch gate is gone: every chassis ships a non-empty fixed kit, so the bar is never empty
    // and Launch is never blocked.
    [Fact]
    public void EveryCoreRuneShipsANonEmptyKitThatLandsInTheLoadout()
    {
        var build = New();
        for (var i = 0; i < build.CoreRuneCount; i++)
        {
            Assert.NotEmpty(build.CoreRune.Kit);
            Assert.NotEmpty(build.Equipment); // seeded, no manual pick
            build.CycleCoreRune(1);
        }
    }

    [Fact]
    public void CyclingCoreRuneReseedsTheKit()
    {
        var build = New(); // Grunt: jab, brace, bandage
        Assert.Equal(new[] { "jab", "brace", "bandage" }, build.Equipment.Select(t => t.Id));

        build.CycleCoreRune(1); // Warden: cleave, brace, bandage
        Assert.Equal(new[] { "cleave", "brace", "bandage" }, build.Equipment.Select(t => t.Id));
    }
}
