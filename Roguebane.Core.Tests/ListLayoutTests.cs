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
    public void GridWrapsLeftToRightThenTopToBottom()
    {
        var item = new Item { Template = "coreCard", Flow = "grid", Gap = 10, Size = new[] { 176, 200 } };
        var region = new LayoutRect(0, 0, 400, 600); // stepX 186 -> 2 columns fit
        var cells = ListLayout.Cells(region, item, 5);

        Assert.Equal(5, cells.Count);
        Assert.Equal(new LayoutRect(0, 0, 176, 200), cells[0]);       // col0 row0
        Assert.Equal(186, cells[1].X); Assert.Equal(0, cells[1].Y);   // col1 row0
        Assert.Equal(0, cells[2].X); Assert.Equal(210, cells[2].Y);   // col0 row1 (200 + 10 gap)
        Assert.Equal(186, cells[3].X); Assert.Equal(210, cells[3].Y); // col1 row1
        Assert.Equal(0, cells[4].X); Assert.Equal(420, cells[4].Y);   // col0 row2
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

    [Fact]
    public void LinearFlowsClipCellsThatOverflowTheRegion()
    {
        // Live data can outgrow the authored strip (26 HP pips in a 12-pip region): cells past the
        // region edge DROP (overflow:hidden), never spill into neighbouring elements.
        var strip = new Item { Template = "healPip", Flow = "horizontal", Gap = 2, Size = new[] { 15, 6 } };
        var cells = ListLayout.Cells(new LayoutRect(22, 95, 195, 6), strip, 26);
        Assert.Equal(11, cells.Count);                        // floor((195+2)/17)
        Assert.True(cells[^1].X + 15 <= 22 + 195);            // last cell inside the region
    }

    [Fact]
    public void PadInsetsTheRegionBeforeCellsFlow()
    {
        // item.pad [T,R,B,L] (LAYOUT_CONTRACT §12): cells start inside the padded region, and the
        // grid's column fit uses the padded width — rows stop hugging their panel's edge.
        var padded = new Item { Template = "legendRow", Flow = "grid", Gap = 4,
            Size = new[] { 104, 8 }, Pad = new[] { 2, 4, 2, 4 } };
        var cells = ListLayout.Cells(new LayoutRect(0, 0, 220, 40), padded, 3);
        Assert.Equal(new LayoutRect(4, 2, 104, 8), cells[0]);  // +L, +T
        Assert.Equal(4 + 108, cells[1].X);                     // 2 columns fit the 212px padded width
        Assert.Equal(new LayoutRect(4, 2 + 12, 104, 8), cells[2]); // wraps to row 2
    }
}
