using Roguebane.Core.Content;

namespace Roguebane.Core.Tests;

public class BalanceSimTests
{
    [Fact]
    public void RankingIsDeterministic()
    {
        var a = BalanceSim.Run(Builds.Sweep, Sessions.Demo);
        var b = BalanceSim.Run(Builds.Sweep, Sessions.Demo);

        Assert.Equal(
            a.Select(r => (r.Build.Name, r.Won, r.Ticks)),
            b.Select(r => (r.Build.Name, r.Won, r.Ticks)));
    }

    [Fact]
    public void WinnersAreRankedAheadOfLosers()
    {
        var results = BalanceSim.Run(Builds.Sweep, Sessions.Demo);

        var firstLoss = results.ToList().FindIndex(r => !r.Won);
        if (firstLoss >= 0)
            Assert.All(results.Skip(firstLoss), r => Assert.False(r.Won));
    }

    [Fact]
    public void StrongestBuildClearsTheRunFastest()
    {
        var results = BalanceSim.Run(Builds.Sweep, Sessions.Demo);

        Assert.True(results[0].Won);
        Assert.Equal(Builds.AllSix.Name, results[0].Build.Name); // most allocation => most output
    }

    [Fact]
    public void StarvedBuildCannotOutraceTheCastleSupport()
    {
        var results = BalanceSim.Run(Builds.Sweep, Sessions.Demo);
        var glass = results.Single(r => r.Build.Name == Builds.GlassEmber.Name);

        Assert.False(glass.Won); // 1 dmg/tick loses the DPS race at the castle
    }
}
