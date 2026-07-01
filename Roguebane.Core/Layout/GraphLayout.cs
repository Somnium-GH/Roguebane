namespace Roguebane.Core.Layout;

// Places a graph container's nodes (manifest `type:"graph"`) inside its resolved region. Node grid
// coords (Col = depth, Row = lane) spread to FILL the region so the chart is viewport-independent --
// col 0 pins to the left edge, the deepest col to the right; a single col/row centres. Pure + headless-
// testable: the shell resolves the region from the manifest, then blits each node at Cell(...).
public static class GraphLayout
{
    // The cell rect for one node. `cols`/`rows` are the grid extents (max index + 1); `cellW`/`cellH`
    // the node footprint. The node sits at its grid fraction of the leftover space, so extremes touch
    // the region edges and interior nodes distribute evenly.
    public static LayoutRect Cell(LayoutRect region, int cols, int rows, int col, int row, int cellW, int cellH)
    {
        var x = region.X + Spread(col, cols, region.W - cellW);
        var y = region.Y + Spread(row, rows, region.H - cellH);
        return new LayoutRect(x, y, cellW, cellH);
    }

    // A single index's offset within `span`: 0..span across count-1 steps; centred when count <= 1.
    private static int Spread(int index, int count, int span)
    {
        if (span <= 0) return Math.Max(0, span) / 2;
        if (count <= 1) return span / 2;
        return (int)Math.Round((double)index / (count - 1) * span);
    }
}
