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
