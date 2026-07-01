using Roguebane.Core.Layout;

namespace Roguebane.Core.Tests;

// A list container stamps its item template per bound datum, stepped along the flow by cell + gap.
public class ListLayoutTests
{
    private static readonly LayoutRect Region = new(20, 75, 920, 423);
    private static Item Horizontal => new() { Template = "coreCard", Flow = "horizontal", Gap = 10, Size = new[] { 176, 423 } };

    [Fact]
    public void HorizontalStepsByCellWidthPlusGap()
    {
        var cells = ListLayout.Cells(Region, Horizontal, 5);
        Assert.Equal(5, cells.Count);
        Assert.Equal(new LayoutRect(20, 75, 176, 423), cells[0]);
        Assert.Equal(20 + 186, cells[1].X);          // 176 + 10 gap
        Assert.Equal(20 + 4 * 186, cells[4].X);
        Assert.All(cells, c => Assert.Equal(75, c.Y)); // no vertical drift
    }

    [Fact]
    public void VerticalStepsByCellHeightPlusGap()
    {
        var item = new Item { Template = "row", Flow = "vertical", Gap = 4, Size = new[] { 100, 20 } };
        var cells = ListLayout.Cells(Region, item, 3);
        Assert.All(cells, c => Assert.Equal(20, c.X));
        Assert.Equal(75, cells[0].Y);
        Assert.Equal(75 + 24, cells[1].Y); // 20 + 4 gap
        Assert.Equal(75 + 2 * 24, cells[2].Y);
    }

    [Fact]
    public void CellsCarryTheItemFootprint()
    {
        var c = ListLayout.Cells(Region, Horizontal, 1)[0];
        Assert.Equal(176, c.W);
        Assert.Equal(423, c.H);
    }

    [Fact]
    public void ZeroCountIsEmpty()
    {
        Assert.Empty(ListLayout.Cells(Region, Horizontal, 0));
    }

    [Fact]
    public void FallsBackToTheTemplateSizeWhenTheItemOmitsIt()
    {
        // A terse list item (no own size) that reuses its template's footprint: the caller passes the
        // template Size as fallback, so cells are sized instead of collapsing to 0x0.
        var terse = new Item { Template = "loadoutCard", Flow = "horizontal", Gap = 6 }; // Size unset
        var c = ListLayout.Cells(Region, terse, 1, fallbackSize: new[] { 131, 89 })[0];
        Assert.Equal(131, c.W);
        Assert.Equal(89, c.H);
    }
}
