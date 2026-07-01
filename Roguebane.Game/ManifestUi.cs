using System.Linq;
using Microsoft.Xna.Framework;
using Roguebane.Core.Layout;

namespace Roguebane.Game;

// Bridge from the typed Core layout manifest to MonoGame draw types: resolves a screen element's
// design-space rect (via ScreenLayout) and palette colours (via PaletteColor). All lookups are
// tolerant — a missing manifest/element/colour returns null/fallback so the shell degrades to its
// legacy magic-number layout rather than crashing or drawing nothing.
public sealed class ManifestUi
{
    private readonly LayoutRegistry _layout;
    public ManifestUi(LayoutRegistry layout) => _layout = layout;

    public bool Has => _layout.Manifest is not null;

    // The typed screen definition (its elements + design size), or null if absent — for the generic
    // manifest-driven renderer to iterate.
    public Screen? ScreenDef(string screen) =>
        _layout.Manifest is { } m && m.Screens.TryGetValue(screen, out var s) ? s : null;

    // Resolve an element's design-space rect within its screen.
    public static Rectangle Rect(Screen screen, Element e)
    {
        var r = ScreenLayout.Resolve(screen, e);
        return new Rectangle(r.X, r.Y, r.W, r.H);
    }

    public Rectangle? ElementRect(string screen, string id)
    {
        var m = _layout.Manifest;
        if (m is null || !m.Screens.TryGetValue(screen, out var s)) return null;
        var e = s.Elements.FirstOrDefault(x => x.Id == id);
        if (e is null) return null;
        var r = ScreenLayout.Resolve(s, e);
        return new Rectangle(r.X, r.Y, r.W, r.H);
    }

    // The cell rects for a list container (its `item` stamped `count` times), or null if the manifest,
    // screen, element, or item is missing — so the caller falls back to its legacy layout.
    public System.Collections.Generic.IReadOnlyList<Rectangle>? ListCells(string screen, string id, int count)
    {
        var m = _layout.Manifest;
        if (m is null || !m.Screens.TryGetValue(screen, out var s)) return null;
        var e = s.Elements.FirstOrDefault(x => x.Id == id);
        if (e?.Item is null) return null;
        var region = ScreenLayout.Resolve(s, e);
        var tmplSize = m.Templates.TryGetValue(e.Item.Template, out var tmpl) ? tmpl.Size : null;
        return ListLayout.Cells(region, e.Item, count, tmplSize)
            .Select(r => new Rectangle(r.X, r.Y, r.W, r.H)).ToList();
    }

    // The literal `content` string of a text element, or null if the manifest/element is missing.
    public string? ElementContent(string screen, string id)
    {
        var m = _layout.Manifest;
        if (m is null || !m.Screens.TryGetValue(screen, out var s)) return null;
        return s.Elements.FirstOrDefault(x => x.Id == id)?.Content;
    }

    public Color Color(string name, Color fallback)
    {
        var style = _layout.Manifest?.Style;
        if (style is null) return fallback;
        var c = PaletteColor.Named(style, name, new Rgba(fallback.R, fallback.G, fallback.B, fallback.A));
        return new Color(c.R, c.G, c.B, c.A);
    }
}
