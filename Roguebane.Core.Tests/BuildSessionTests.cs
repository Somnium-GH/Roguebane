using Roguebane.Core.Content;

namespace Roguebane.Core.Tests;

public class BuildSessionTests
{
    private static BuildSession New() => Sessions.NewBuild();

    [Fact]
    public void CyclingChassisWrapsAndResetsRunes()
    {
        var build = New();
        Assert.Same(Chassrium.Grunt, build.Chassis);

        build.CycleChassis(1);
        Assert.Same(Chassrium.Adept, build.Chassis);
        Assert.Equal(Chassrium.Adept.RuneBudget, build.Runes.Available); // fresh budget

        build.CycleChassis(1); // wraps back to the first
        Assert.Same(Chassrium.Grunt, build.Chassis);

        build.CycleChassis(-1); // wraps the other way
        Assert.Same(Chassrium.Adept, build.Chassis);
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
        var build = New();
        build.Toggle(Techniques.Drain);
        build.Toggle(Techniques.Jab);

        Assert.True(build.IsSelected(Techniques.Jab));
        Assert.Equal(new[] { "jab", "drain" }, build.Loadout.Select(t => t.Id)); // palette order, not click order

        build.Toggle(Techniques.Jab);
        Assert.False(build.IsSelected(Techniques.Jab));
    }

    [Fact]
    public void LaunchMintsTheChosenBodyIntoARun()
    {
        var build = New();
        build.Climb(Paths.VesselLadder);
        build.Toggle(Techniques.Jab);

        var run = Sieges.StandardRun();
        var session = build.Launch(run);

        Assert.Equal(SessionState.Fighting, session.State);
        Assert.Single(session.Loadout);
        Assert.Equal(3, session.Run.Nodes.Count); // cp1, cp2, castle
    }
}
