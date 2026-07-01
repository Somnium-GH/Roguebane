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
            cells.Add(new LayoutRect(x, y, w, h));
        }
        return cells;
    }
}
