namespace Roguebane.Core.Layout;

// Lays out a list container's cells (manifest `item` with flow horizontal|vertical): the consumer
// stamps the item's template at each returned cell. Pure + headless-testable; graph containers use
// GraphLayout instead. Cells start at the region origin and step by cell size + gap along the flow.
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
        var vertical = item.Flow == "vertical";

        var cells = new List<LayoutRect>(Math.Max(0, count));
        for (var i = 0; i < count; i++)
        {
            var x = region.X + (vertical ? 0 : i * (w + item.Gap));
            var y = region.Y + (vertical ? i * (h + item.Gap) : 0);
            cells.Add(new LayoutRect(x, y, w, h));
        }
        return cells;
    }
}
