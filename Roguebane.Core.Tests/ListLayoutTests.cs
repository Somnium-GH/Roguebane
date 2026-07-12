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
    public void StretchCellsFillTheFullRegionWidthRegardlessOfCount()
    {
        // Attr pip strips (Doug #9): N cells span the whole region so a 5-pip bar reads as long as a
        // 7-pip one. Last cell lands flush against the region's right edge for any N — no gap.
        var region = new LayoutRect(59, 2, 332, 9);
        foreach (var n in new[] { 4, 5, 6, 7 })
        {
            var cells = ListLayout.StretchCells(region, n, gap: 2, cellHeight: 9);
            Assert.Equal(n, cells.Count);
            Assert.Equal(region.X, cells[0].X);                          // flush left
            Assert.Equal(region.X + region.W, cells[^1].X + cells[^1].W); // flush right, no trailing gap
            Assert.All(cells, c => Assert.Equal(9, c.H));
            Assert.All(cells, c => Assert.True(c.W >= 1));
        }
    }

    [Fact]
    public void StretchCellsGivesUniformGapsAndEvenWidths()
    {
        // B32 (Doug playtest): the realized gap between EVERY neighbouring pair must be exactly `gap`
        // (an occasional gap-1/gap+1 seam read as "gaps... like CON"), and cell widths within one row
        // may differ by at most the unavoidable 1px of integer rounding ("borders too stretched" was a
        // fixed stroke on an unevenly-narrow cell). Swept across widths/counts/gaps that force fractions.
        foreach (var (w, gap) in new[] { (332, 2), (327, 2), (200, 3), (101, 1), (55, 0), (918, 5) })
            foreach (var n in new[] { 2, 3, 4, 5, 6, 7, 8 })
            {
                var region = new LayoutRect(17, 4, w, 9);
                var cells = ListLayout.StretchCells(region, n, gap, cellHeight: 9);
                for (var i = 0; i + 1 < cells.Count; i++)
                    Assert.Equal(gap, cells[i + 1].X - (cells[i].X + cells[i].W)); // realized gap == gap, exactly
                var widths = cells.Select(c => c.W).ToList();
                Assert.True(widths.Max() - widths.Min() <= 1,
                    $"width spread {widths.Max() - widths.Min()} > 1 for w={w} n={n} gap={gap}");
                Assert.Equal(region.X + region.W, cells[^1].X + cells[^1].W); // still flush right
            }
    }

    [Fact]
    public void StretchCellsEmptyForNonPositiveCount()
        => Assert.Empty(ListLayout.StretchCells(new LayoutRect(0, 0, 100, 10), 0, 2, 10));

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

    [Fact]
    public void GridCapacityMultipliesColsByRowsForNewGamesCoreGrid()
    {
        // NewGame's coreCards region/item exactly (layout.json corePanel.coreCards): 3 cols x 1 row
        // fit -> a pager sized off this seats 3 cores/page, matching CD's own NewGame.dc.html PER=3.
        var item = new Item { Template = "coreCard", Flow = "grid", Gap = 10, Size = new[] { 152, 395 } };
        Assert.Equal(3, ListLayout.GridCapacity(new LayoutRect(0, 0, 476, 404), item));
    }

    [Fact]
    public void GridCapacityHonestlyReportsAOnePixelColumnShortfall()
    {
        // Equipment's invItems region/item exactly (layout.json inventory.invItems): the authored
        // "cols":2 hint doesn't quite fit (2*199+6 = 404 > 403) -- GridCapacity derives what actually
        // fits (1 col) rather than trusting the inert hint, so a pager sized off it never over-seats.
        var item = new Item { Template = "invCard", Flow = "grid", Gap = 6, Size = new[] { 199, 44 } };
        Assert.Equal(3, ListLayout.GridCapacity(new LayoutRect(0, 0, 403, 183), item)); // 1 col x 3 rows
    }

    [Fact]
    public void RaceCardsSeatAllFiveRacesWithNoSilentDrop()
    {
        // CHUNK C item 1's MEASURE step: NewGame's raceCards region/item exactly (layout.json
        // racePanel.raceCards), vertical flow -- 5 races * 79 + 4 gaps * 7 = 423, exactly the region
        // height, so all 5 seat with room to spare. Unlike coreCards (7 cores, 3/page grid), the race
        // list needs no pager -- this pins that fact so a future CD re-drop that shrinks the panel
        // trips this test instead of silently dropping a race card (Cells' overflow:hidden clip).
        var item = new Item { Template = "raceCard", Flow = "vertical", Gap = 7, Size = new[] { 209, 79 } };
        Assert.Equal(5, ListLayout.Cells(new LayoutRect(0, 0, 209, 423), item, 5).Count);
    }
}
