namespace Roguebane.Core.Layout;

// A template part stamped at an absolute screen position: its card-local rect translated to the
// card's origin, carrying the style (colour/font/size) and which datum (sample) fills the slot.
public readonly record struct PlacedPart(
    LayoutRect Rect, string Color, string Font, double FontPx, string Sample,
    string? Image = null, string? Binds = null, Fill? Fill = null, Border? Border = null);

public static class CardTemplate
{
    // Stamp a card at (x,y): every sub-part's local rect is offset by the origin; size is preserved.
    public static IReadOnlyList<PlacedPart> Place(Template t, int x, int y)
    {
        var parts = new List<PlacedPart>(t.Parts.Length);
        foreach (var p in t.Parts)
            parts.Add(new PlacedPart(
                new LayoutRect(x + p.Rect[0], y + p.Rect[1], p.Rect[2], p.Rect[3]),
                p.Color, p.Font, p.FontPx, p.Sample, p.Image, p.Binds, p.Fill, p.Border));
        return parts;
    }
}
