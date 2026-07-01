namespace Roguebane.Core.Layout;

// Lays out a list container's cells (manifest `item` with flow horizontal|vertical): the consumer
// stamps the item's template at each returned cell. Pure + headless-testable; graph containers use
// GraphLayout instead. Cells start at the region origin and step by cell size + gap along the flow.
public static class ListLayout
{
    public static IReadOnlyList<LayoutRect> Cells(LayoutRect region, Item item, int count)
    {
        var w = item.Size.Length > 0 ? item.Size[0] : 0;
        var h = item.Size.Length > 1 ? item.Size[1] : 0;
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
