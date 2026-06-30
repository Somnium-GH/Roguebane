using Roguebane.Core.Layout;

namespace Roguebane.Core.Tests;

// Pins the layout.json parser against the REAL manifest shipped in Roguebane.Content,
// so schema drift from Claude Design breaks a test rather than the shell at runtime.
public class LayoutManifestTests
{
    private static LayoutManifest Real()
        => LayoutManifest.Parse(File.ReadAllText(LocateManifest()));

    private static string LocateManifest()
    {
        var dir = AppContext.BaseDirectory;
        while (dir is not null)
        {
            var candidate = Path.Combine(dir, "Roguebane.Content", "layout.json");
            if (File.Exists(candidate)) return candidate;
            dir = Path.GetDirectoryName(dir);
        }
        throw new FileNotFoundException("Roguebane.Content/layout.json not found above the test bin");
    }

    [Fact]
    public void ParsesEveryTopLevelSection()
    {
        var m = Real();
        Assert.NotEmpty(m.Figures);
        Assert.NotEmpty(m.Gear);
        Assert.NotEmpty(m.Screens);
        Assert.NotEmpty(m.Style.Palette);
        Assert.NotEmpty(m.Templates);
    }

    [Fact]
    public void FigureCarriesPartsSocketsZAndPivot()
    {
        var grunt = Real().Figures["grunt"];
        Assert.Equal(2, grunt.Size.Length);
        Assert.Equal(2, grunt.Pivot.Length);
        Assert.Contains("torso", grunt.Z);
        Assert.Equal(4, grunt.Parts["torso"].Rect.Length); // x,y,w,h
        Assert.True(grunt.Sockets.ContainsKey("handL"));
        Assert.Equal(2, grunt.Sockets["handL"].Length);
    }

    [Fact]
    public void FigureMountsBindGearToSockets()
    {
        var mounts = Real().Figures["grunt"].Mounts;
        Assert.Contains(mounts, x => x.Gear == "sword" && x.Socket == "handL");
    }

    [Fact]
    public void GearCarriesPivot()
    {
        Assert.Equal(2, Real().Gear["sword"].Pivot.Length);
    }

    [Fact]
    public void ScreenCarriesDesignSizeAndPlacedElements()
    {
        var combat = Real().Screens["combat"];
        Assert.Equal(new[] { 960, 540 }, combat.DesignSize);
        var backdrop = combat.Elements.Single(e => e.Id == "backdrop");
        Assert.Equal(2, backdrop.Offset.Length);
        Assert.Equal(2, backdrop.Size.Length);
        Assert.Equal("encounter.scene", backdrop.Binds);
    }

    [Fact]
    public void PaletteResolvesNamedColors()
    {
        var palette = Real().Style.Palette;
        Assert.StartsWith("#", palette["amber"]);
        Assert.StartsWith("#", palette["ink"]);
    }
}
