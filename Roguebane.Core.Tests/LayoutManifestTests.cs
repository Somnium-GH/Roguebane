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
    public void GraphContainerCarriesItsItemTemplate()
    {
        // The runmap chart is a graph container: the consumer stamps beaconNode per map node.
        var chart = Real().Screens["runmap"].Elements.Single(e => e.Id == "chart");
        Assert.Equal("graph", chart.Type);
        Assert.Equal("map", chart.Binds);
        Assert.NotNull(chart.Item);
        Assert.Equal("beaconNode", chart.Item!.Template);
        Assert.Equal("graph", chart.Item.Flow);
    }

    [Fact]
    public void ListContainerCarriesFlowGapAndCellSize()
    {
        // The build action bar stamps a techCard per loadout technique, laid out horizontally.
        var lists = Real().Screens["build"].Elements.Where(e => e.Item is { Flow: "horizontal" }).ToList();
        Assert.NotEmpty(lists);
        Assert.All(lists, e => Assert.False(string.IsNullOrEmpty(e.Item!.Template)));
        Assert.Contains(lists, e => e.Item!.Size.Length == 2); // most cells are sized
        Assert.Contains(lists, e => e.Item!.Gap > 0);
    }

    [Fact]
    public void NewRunStampsACoreCard()
    {
        var cores = Real().Screens["newrun"].Elements.Single(e => e.Item is { Template: "coreCard" });
        Assert.Equal(2, cores.Item!.Size.Length); // a sized cell to stamp per core
    }

    [Fact]
    public void LiteralTextElementsCarryContent()
    {
        // A `content` element is fixed copy (no data binding) — e.g. the runmap castle note.
        Assert.Contains(Real().Screens["runmap"].Elements, e => !string.IsNullOrEmpty(e.Content));
    }

    [Fact]
    public void PaletteResolvesNamedColors()
    {
        var palette = Real().Style.Palette;
        Assert.StartsWith("#", palette["amber"]);
        Assert.StartsWith("#", palette["ink"]);
    }
}
