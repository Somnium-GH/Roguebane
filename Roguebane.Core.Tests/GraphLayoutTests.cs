using Roguebane.Core.Layout;

namespace Roguebane.Core.Tests;

// Graph nodes spread to FILL their manifest region (viewport-independent chart), extremes on the edges.
public class GraphLayoutTests
{
    private static readonly LayoutRect Region = new(100, 200, 900, 400);

    [Fact]
    public void FirstColumnPinsLeftLastColumnPinsRight()
    {
        var left = GraphLayout.Cell(Region, cols: 5, rows: 3, col: 0, row: 0, cellW: 28, cellH: 28);
        var right = GraphLayout.Cell(Region, cols: 5, rows: 3, col: 4, row: 0, cellW: 28, cellH: 28);

        Assert.Equal(Region.X, left.X);                       // col 0 -> left edge
        Assert.Equal(Region.X + Region.W - 28, right.X);      // last col -> right edge (minus cell)
    }

    [Fact]
    public void FirstRowPinsTopLastRowPinsBottom()
    {
        var top = GraphLayout.Cell(Region, 5, 3, 0, 0, 28, 28);
        var bottom = GraphLayout.Cell(Region, 5, 3, 0, 2, 28, 28);

        Assert.Equal(Region.Y, top.Y);
        Assert.Equal(Region.Y + Region.H - 28, bottom.Y);
    }

    [Fact]
    public void InteriorNodeDistributesEvenly()
    {
        var mid = GraphLayout.Cell(Region, 5, 3, 2, 1, 28, 28); // middle col + middle row
        Assert.Equal(Region.X + (Region.W - 28) / 2, mid.X);
        Assert.Equal(Region.Y + (Region.H - 28) / 2, mid.Y);
    }

    [Fact]
    public void SingleColumnOrRowCentres()
    {
        var only = GraphLayout.Cell(Region, cols: 1, rows: 1, col: 0, row: 0, cellW: 28, cellH: 28);
        Assert.Equal(Region.X + (Region.W - 28) / 2, only.X);
        Assert.Equal(Region.Y + (Region.H - 28) / 2, only.Y);
    }

    [Fact]
    public void CellKeepsTheGivenFootprint()
    {
        var c = GraphLayout.Cell(Region, 5, 3, 3, 2, 28, 28);
        Assert.Equal(28, c.W);
        Assert.Equal(28, c.H);
    }
}
