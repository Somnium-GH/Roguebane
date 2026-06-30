namespace Roguebane.Core.Layout;

public enum Anchor { TopLeft, Top, TopRight, Left, Center, Right, BottomLeft, Bottom, BottomRight }

public readonly record struct LayoutRect(int X, int Y, int W, int H);

// Resolves a manifest element's design-space rect from its anchor + offset + size. An element's
// OWN same-named corner is pinned to (viewport-anchor point + offset): TopLeft pins the top-left,
// Center pins the centre, BottomRight pins the bottom-right, etc. Output is in the screen's design
// coordinates; mapping design->screen (cover/letterbox + integer stage) is a separate viewport pass.
public static class ScreenLayout
{
    public static LayoutRect Resolve(Screen screen, Element e)
        => Resolve(screen.DesignSize[0], screen.DesignSize[1], e);

    public static LayoutRect Resolve(int designW, int designH, Element e)
    {
        var a = Parse(e.Anchor);
        var (ax, ay) = Point(designW, designH, a);   // anchor point on the viewport
        var (ex, ey) = Point(e.Size[0], e.Size[1], a); // the element's same-named corner, element-local
        return new LayoutRect(ax + e.Offset[0] - ex, ay + e.Offset[1] - ey, e.Size[0], e.Size[1]);
    }

    public static Anchor Parse(string anchor) => anchor switch
    {
        "TopLeft" => Anchor.TopLeft,
        "Top" => Anchor.Top,
        "TopRight" => Anchor.TopRight,
        "Left" => Anchor.Left,
        "Center" => Anchor.Center,
        "Right" => Anchor.Right,
        "BottomLeft" => Anchor.BottomLeft,
        "Bottom" => Anchor.Bottom,
        "BottomRight" => Anchor.BottomRight,
        _ => Anchor.TopLeft,
    };

    private static (int x, int y) Point(int w, int h, Anchor a)
    {
        var x = a switch
        {
            Anchor.TopLeft or Anchor.Left or Anchor.BottomLeft => 0,
            Anchor.Top or Anchor.Center or Anchor.Bottom => w / 2,
            _ => w, // TopRight / Right / BottomRight
        };
        var y = a switch
        {
            Anchor.TopLeft or Anchor.Top or Anchor.TopRight => 0,
            Anchor.Left or Anchor.Center or Anchor.Right => h / 2,
            _ => h, // BottomLeft / Bottom / BottomRight
        };
        return (x, y);
    }
}
