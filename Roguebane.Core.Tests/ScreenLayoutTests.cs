using Roguebane.Core.Layout;

namespace Roguebane.Core.Tests;

// ScreenLayout turns a manifest element's anchor+offset+size into a design-space rect. These pin
// the anchor-corner convention for every anchor, plus a real-manifest sanity check.
public class ScreenLayoutTests
{
    private static Element El(string anchor, int ox, int oy, int w, int h)
        => new() { Anchor = anchor, Offset = [ox, oy], Size = [w, h] };

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
    public void ResolvesAgainstTheRealManifestScreen()
    {
        var dir = AppContext.BaseDirectory;
        while (dir is not null && !File.Exists(Path.Combine(dir, "Roguebane.Content", "layout.json")))
            dir = Path.GetDirectoryName(dir);
        var m = LayoutManifest.Parse(File.ReadAllText(Path.Combine(dir!, "Roguebane.Content", "layout.json")));

        var combat = m.Screens["combat"];
        var backdrop = combat.Elements.Single(e => e.Id == "backdrop");
        var r = ScreenLayout.Resolve(combat, backdrop);
        Assert.Equal(combat.DesignSize[0], r.W); // full-width band
        Assert.True(r.Y >= 0 && r.Y + r.H <= combat.DesignSize[1]); // stays on screen
    }
}
