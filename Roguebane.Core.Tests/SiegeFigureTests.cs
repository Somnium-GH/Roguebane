using Roguebane.Core.Content;

namespace Roguebane.Core.Tests;

// Encounters assign creature figures so the shipped foe figure sets get used (variety, not all-ogre).
public class SiegeFigureTests
{
    [Fact]
    public void FieldRaidersRotateAcrossSlots()
    {
        var enc = Sieges.ArmedPoint("skirmish", 6, 6);
        Assert.Equal(new[] { "bandit", "skeleton" }, enc.Foes.Select(f => f.Figure).ToArray());
    }

    [Fact]
    public void CastleDefendersAreHeavyFigures()
    {
        var enc = Sieges.ArmedCastle();
        Assert.Equal(new[] { "ogre", "troll", "ogre" }, enc.Foes.Select(f => f.Figure).ToArray());
    }
}
