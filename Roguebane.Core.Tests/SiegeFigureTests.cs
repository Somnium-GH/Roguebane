using Roguebane.Core.Content;

namespace Roguebane.Core.Tests;

// Single-foe canon: an encounter is ONE enemy, drawn with a shipped creature figure.
public class SiegeFigureTests
{
    [Fact]
    public void AFieldSkirmishIsOneRaiderFigure()
    {
        var enc = Sieges.ArmedPoint("skirmish", 6, 6);
        Assert.Equal("bandit", enc.Enemy!.Figure);
    }

    [Fact]
    public void TheCastleBossIsAHeavyFigure()
    {
        var enc = Sieges.ArmedCastle();
        Assert.Equal("ogre", enc.Enemy!.Figure);
    }
}
