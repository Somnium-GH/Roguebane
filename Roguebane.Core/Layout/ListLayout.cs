namespace Roguebane.Core.Layout;

// Lays out a list container's cells (manifest `item` with flow horizontal|vertical|grid): the consumer
// stamps the item's template at each returned cell. Pure + headless-testable; graph containers use
// GraphLayout instead. horizontal/vertical step by cell size + gap along one axis; grid wraps left→right
// then top→bottom, fitting as many columns as the region width allows (min 1).
public static class ListLayout
{
    // The cell size comes from the item's `size`; when the manifest omits it (a terse list that reuses
    // its template's footprint), the caller passes the template's Size as `fallbackSize`.
    public static IReadOnlyList<LayoutRect> Cells(LayoutRect region, Item item, int count,
        int[]? fallbackSize = null)
    {
        // item.pad (LAYOUT_CONTRACT §12): CSS-order [T,R,B,L] inner padding — cells flow inside the
        // padded region (fixes rows hugging their panel's edge, e.g. the citymap legend).
        if (item.Pad.Length == 4)
            region = new LayoutRect(region.X + item.Pad[3], region.Y + item.Pad[0],
                Math.Max(0, region.W - item.Pad[1] - item.Pad[3]),
                Math.Max(0, region.H - item.Pad[0] - item.Pad[2]));
        var size = item.Size.Length >= 2 ? item.Size : fallbackSize ?? item.Size;
        var w = size.Length > 0 ? size[0] : 0;
        var h = size.Length > 1 ? size[1] : 0;

        var cells = new List<LayoutRect>(Math.Max(0, count));
        if (item.Flow == "grid")
        {
            var stepX = w + item.Gap;
            var cols = Math.Max(1, stepX > 0 ? (region.W + item.Gap) / stepX : 1);
            for (var i = 0; i < count; i++)
            {
                var (col, row) = (i % cols, i / cols);
                cells.Add(new LayoutRect(region.X + col * stepX, region.Y + row * (h + item.Gap), w, h));
            }
            return cells;
        }

        var vertical = item.Flow == "vertical";
        for (var i = 0; i < count; i++)
        {
            var x = region.X + (vertical ? 0 : i * (w + item.Gap));
            var y = region.Y + (vertical ? i * (h + item.Gap) : 0);
            // overflow:hidden semantics — live data can outgrow the authored strip (26 HP pips in a
            // 12-pip region); cells past the region edge drop instead of spilling into neighbours.
            if (x + w > region.X + region.W || y + h > region.Y + region.H) break;
            cells.Add(new LayoutRect(x, y, w, h));
        }
        return cells;
    }

    // Stretch `count` cells to fill the region's WIDTH (horizontal only): each cell is
    // (region.W - (count-1)*gap) / count wide, so N cells always span the full region regardless of N.
    // Used by the attribute pip strips so every stat's bar maxes its own available width instead of
    // all bars sharing one fixed pip size (a 5-pip bar then reads shorter than a 7-pip one) — Doug #9.
    // Also removes the fixed-size overflow-drop for these strips (a stat whose pip count barely exceeds
    // the authored fit no longer loses its last pip). Height comes from `cellHeight` (the pip template's).
    public static IReadOnlyList<LayoutRect> StretchCells(LayoutRect region, int count, int gap, int cellHeight)
    {
        var cells = new List<LayoutRect>(Math.Max(0, count));
        if (count <= 0) return cells;
        var inner = region.W - (count - 1) * gap;
        for (var i = 0; i < count; i++)
        {
            // Round each cell's left edge from the exact fractional position so rounding never
            // accumulates a gap on the right — the last cell lands flush against the region edge.
            var x0 = region.X + (int)Math.Round(i * (inner / (double)count + gap));
            var x1 = region.X + (int)Math.Round(i * (inner / (double)count + gap) + inner / (double)count);
            cells.Add(new LayoutRect(x0, region.Y, Math.Max(1, x1 - x0), cellHeight));
        }
        return cells;
    }

    // How many grid cells actually FIT the region (cols * rows), for callers that need to page a grid
    // list to what's visible (Cells itself never clips grid overflow — see GridWrapsLeftToRightThen-
    // TopToBottom — so a pager derives its page size from this instead of a hand-picked constant).
    public static int GridCapacity(LayoutRect region, Item item, int[]? fallbackSize = null)
    {
        if (item.Pad.Length == 4)
            region = new LayoutRect(region.X + item.Pad[3], region.Y + item.Pad[0],
                Math.Max(0, region.W - item.Pad[1] - item.Pad[3]),
                Math.Max(0, region.H - item.Pad[0] - item.Pad[2]));
        var size = item.Size.Length >= 2 ? item.Size : fallbackSize ?? item.Size;
        var w = size.Length > 0 ? size[0] : 0;
        var h = size.Length > 1 ? size[1] : 0;

        var stepX = w + item.Gap;
        var stepY = h + item.Gap;
        var cols = Math.Max(1, stepX > 0 ? (region.W + item.Gap) / stepX : 1);
        var rows = Math.Max(1, stepY > 0 ? (region.H + item.Gap) / stepY : 1);
        return cols * rows;
    }
}
