using Roguebane.Core;
using Roguebane.Core.Content;

namespace Roguebane.Core.Tests;

// A foe carries a creature FIGURE key (for the shell to compose) distinct from its encounter-role Id.
public class FoeFigureTests
{
    [Fact]
    public void ArmedFoeDefaultsToTheOgreFigure()
    {
        var foe = Foes.Armed("gate", 12);
        Assert.Equal("ogre", foe.Figure);
        Assert.Equal("gate", foe.Id); // id stays the role tag
    }

    [Fact]
    public void FigureIsSettablePerContent()
    {
        Assert.Equal("troll", Foes.Armed("boss", 20, figure: "troll").Figure);
    }
}
