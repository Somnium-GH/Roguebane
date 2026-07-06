using Roguebane.Core.Layout;

namespace Roguebane.Core.Tests;

// ScreenLayout turns a manifest element's anchor+offset+size into a design-space rect. These pin
// the anchor-corner convention for every anchor, plus a real-manifest sanity check.
public class ScreenLayoutTests
{
    private static Element El(string anchor, int ox, int oy, int w, int h)
        => new() { Anchor = anchor, Offset = [ox, oy], Size = [w, h] };

    private static Element El(string id, string anchor, int ox, int oy, int w, int h, string? parent = null)
        => new() { Id = id, Anchor = anchor, Offset = [ox, oy], Size = [w, h], Parent = parent };

    private static Screen Scr(int w, int h, params Element[] elements)
        => new() { DesignSize = [w, h], Elements = elements };

    [Fact]
    public void TopLeftPinsTheTopLeftCorner()
    {
        var r = ScreenLayout.Resolve(960, 540, El("TopLeft", 462, 6, 48, 16));
        Assert.Equal(new LayoutRect(462, 6, 48, 16), r);
    }

    [Fact]
    public void CenterPinsTheElementCentre()
    {
        // viewport centre (480,270) + offset(0,-77) = element centre -> top-left (0,28) for a 960x330 box
        var r = ScreenLayout.Resolve(960, 540, El("Center", 0, -77, 960, 330));
        Assert.Equal(new LayoutRect(0, 28, 960, 330), r);
    }

    [Fact]
    public void BottomRightPinsTheBottomRightCorner()
    {
        // bottom-right (960,540) + offset(-10,-10) = element bottom-right -> top-left (960-10-100, 540-10-20)
        var r = ScreenLayout.Resolve(960, 540, El("BottomRight", -10, -10, 100, 20));
        Assert.Equal(new LayoutRect(850, 510, 100, 20), r);
    }

    [Fact]
    public void RightCentresVerticallyAndPinsRightEdge()
    {
        var r = ScreenLayout.Resolve(960, 540, El("Right", 0, 0, 40, 40));
        Assert.Equal(new LayoutRect(920, 250, 40, 40), r); // x=960-40, y=270-20
    }

    [Fact]
    public void ParentedChildResolvesRelativeToTheParentsRectNotTheViewport()
    {
        // parent sits off-origin on the far right (mirrors previewStage in the NewGame bug); the
        // child's small TopLeft offset must land inside the PARENT's box, not the raw viewport TopLeft.
        var parent = El("previewStage", "TopRight", -400, 100, 300, 400);
        var child = El("previewName", "TopLeft", 7, 7, 100, 20, parent: "previewStage");
        var screen = Scr(960, 540, parent, child);

        var parentRect = ScreenLayout.Resolve(screen, parent);
        var childRect = ScreenLayout.Resolve(screen, child);

        Assert.Equal(new LayoutRect(260, 100, 300, 400), parentRect); // TopRight(960,0) + offset(-400,100) - elemTopRight(300,0)
        Assert.Equal(new LayoutRect(parentRect.X + 7, parentRect.Y + 7, 100, 20), childRect);
        Assert.NotEqual(new LayoutRect(7, 7, 100, 20), childRect); // the bug: would land at raw viewport TopLeft
    }

    [Fact]
    public void MissingParentIdFallsBackToTheViewport()
    {
        var child = El("orphan", "TopLeft", 10, 10, 50, 50, parent: "does_not_exist");
        var screen = Scr(960, 540, child);

        var r = ScreenLayout.Resolve(screen, child);

        Assert.Equal(new LayoutRect(10, 10, 50, 50), r); // degrades to plain viewport anchoring
    }

    [Fact]
    public void CyclicParentChainFallsBackToTheViewportInsteadOfOverflowing()
    {
        var a = El("a", "TopLeft", 0, 0, 10, 10, parent: "b");
        var b = El("b", "TopLeft", 0, 0, 10, 10, parent: "a");
        var screen = Scr(960, 540, a, b);

        var r = ScreenLayout.Resolve(screen, a); // must not stack-overflow

        Assert.Equal(new LayoutRect(0, 0, 10, 10), r);
    }

    [Fact]
    public void ResolvesAgainstTheRealManifestScreen()
    {
        var dir = AppContext.BaseDirectory;
        while (dir is not null && !File.Exists(Path.Combine(dir, "Roguebane.Content", "layout.json")))
            dir = Path.GetDirectoryName(dir);
        var m = LayoutManifest.Parse(File.ReadAllText(Path.Combine(dir!, "Roguebane.Content", "layout.json")));

        // Schema check (no literal ids): resolve every placed element of every screen; the anchor math
        // preserves the element's size and yields a rect.
        foreach (var s in m.Screens.Values)
            foreach (var e in s.Elements)
            {
                var r = ScreenLayout.Resolve(s, e);
                Assert.Equal(e.Size[0], r.W);
                Assert.Equal(e.Size[1], r.H);
            }
    }
}
