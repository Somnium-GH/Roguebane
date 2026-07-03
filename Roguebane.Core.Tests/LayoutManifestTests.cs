using Roguebane.Core.Layout;

namespace Roguebane.Core.Tests;

// Pins the layout.json CONTRACT / SCHEMA, NOT Claude Design's literal keys -- CD owns that file's
// contents, so a figure/screen/template/element RENAME must never break a test; only a real schema
// violation should. Assertions quantify over whatever CD authored ("every figure ...", "every item's
// template resolves ...") rather than naming specific ids.
public class LayoutManifestTests
{
    private static LayoutManifest Real() => LayoutManifest.Parse(File.ReadAllText(LocateManifest()));

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
    public void ParsesFramesAnimationAndNestedPartLists()
    {
        // §12 schema (test-owned fixture, not CD content): element `frames` is an ordered asset
        // list cycled on the fixed tick; a template part may carry its OWN nested `list`.
        var m = LayoutManifest.Parse("""
        {
          "screens": { "s": { "designSize": [960,540], "elements": [
            { "id": "ret", "type": "icon", "anchor": "TopLeft", "offset": [0,0], "size": [64,64],
              "z": 1, "frames": ["ui/reticle/focus_p0","ui/reticle/focus_p1"] } ] } },
          "templates": { "row": { "size": [300,16], "parts": [
            { "rect": [10,3,200,10], "binds": "pool.attr.cells",
              "list": { "template": "pip", "flow": "horizontal", "gap": 2, "size": [16,10] } } ] },
            "pip": { "size": [16,10], "binds": "cell.state", "imageBind": "ui/pip/{cell.asset}" } }
        }
        """);
        var el = m.Screens["s"].Elements[0];
        Assert.Equal(new[] { "ui/reticle/focus_p0", "ui/reticle/focus_p1" }, el.Frames);
        var part = m.Templates["row"].Parts[0];
        Assert.NotNull(part.List);
        Assert.Equal("pip", part.List!.Template);
        Assert.Equal("ui/pip/{cell.asset}", m.Templates["pip"].ImageBind);
        var placed = CardTemplate.Place(m.Templates["row"], 100, 50);
        Assert.Equal("pip", placed[0].List!.Template); // the nested list survives placement
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
    public void EveryFigureCarriesSizePivotZPartsAndSockets()
    {
        Assert.All(Real().Figures.Values, f =>
        {
            Assert.Equal(2, f.Size.Length);
            Assert.Equal(2, f.Pivot.Length);
            Assert.NotEmpty(f.Z);
            Assert.All(f.Parts.Values, p => Assert.Equal(4, p.Rect.Length)); // x,y,w,h
            Assert.All(f.Sockets.Values, s => Assert.Equal(2, s.Length));     // x,y
        });
    }

    [Fact]
    public void EveryFigureMountBindsGearToASocket()
    {
        Assert.All(Real().Figures.Values.SelectMany(f => f.Mounts), m =>
        {
            Assert.False(string.IsNullOrEmpty(m.Gear));
            Assert.False(string.IsNullOrEmpty(m.Socket));
        });
    }

    [Fact]
    public void EveryGearCarriesAPivot()
    {
        Assert.All(Real().Gear.Values, g => Assert.Equal(2, g.Pivot.Length));
    }

    [Fact]
    public void EveryScreenHasADesignSizeAndPlacedElements()
    {
        Assert.All(Real().Screens.Values, s =>
        {
            Assert.Equal(2, s.DesignSize.Length);
            Assert.NotEmpty(s.Elements);
            Assert.All(s.Elements, e =>
            {
                Assert.Equal(2, e.Offset.Length);
                Assert.Equal(2, e.Size.Length);
            });
        });
    }

    [Fact]
    public void EveryItemContainerResolvesToARealTemplateWithASizedCell()
    {
        var templates = Real().Templates.Keys.ToHashSet();
        var items = Real().Screens.Values.SelectMany(s => s.Elements).Where(e => e.Item is not null).ToList();
        Assert.NotEmpty(items); // the manifest drives at least one list/graph from run data
        var real = Real();
        Assert.All(items, e =>
        {
            Assert.Contains(e.Item!.Template, templates); // the stamped template exists
            // A sized cell must be RESOLVABLE — from the item's own size, or (terse form) the template's.
            var cell = e.Item.Size.Length == 2 ? e.Item.Size : real.Templates[e.Item.Template].Size;
            Assert.Equal(2, cell.Length);
        });
    }

    [Fact]
    public void EveryTemplatePartCarriesARect()
    {
        Assert.All(Real().Templates.Values.SelectMany(t => t.Parts),
            p => Assert.Equal(4, p.Rect.Length));
    }

    [Fact]
    public void TemplatePartChromeParses()
    {
        // Part-level fill/border (attr swatches, slot backgrounds): any part carrying a fill must give
        // the renderer a token or a from/to gradient; any part border must carry a colour. The manifest
        // drives at least one such part (quantified — no CD ids pinned).
        var parts = Real().Templates.Values.SelectMany(t => t.Parts).ToList();
        var filled = parts.Where(p => p.Fill is not null).ToList();
        Assert.NotEmpty(filled);
        Assert.All(filled, p => Assert.True(
            !string.IsNullOrEmpty(p.Fill!.Token) ||
            (!string.IsNullOrEmpty(p.Fill.From) && !string.IsNullOrEmpty(p.Fill.To))));
        Assert.All(parts.Where(p => p.Border is not null),
            p => Assert.False(string.IsNullOrEmpty(p.Border!.Color)));
    }

    [Fact]
    public void ImageBindPathsResolveToContentPaths()
    {
        // imageBind (CD #15): a Content path resolved per bound item — either a {bind} template or a
        // STATIC path (a fixed icon in a bound slot; resolves to itself). Every one must be non-empty
        // with balanced braces, and the manifest must exercise the placeholder form at least once.
        var bound = Real().Templates.Values.SelectMany(t => t.Parts)
            .Where(pp => pp.ImageBind is not null).ToList();
        Assert.NotEmpty(bound);
        Assert.All(bound, pp =>
        {
            Assert.False(string.IsNullOrEmpty(pp.ImageBind));
            Assert.Equal(pp.ImageBind!.Contains('{'), pp.ImageBind.Contains('}'));
        });
        Assert.Contains(bound, pp => pp.ImageBind!.Contains('{'));
    }

    [Fact]
    public void BorderSidesNameRealEdges()
    {
        // border.sides: a border may restrict itself to named edges (an accent rule, not a full box).
        // Whatever CD authors, every named side must be a real edge; the drop exercises the form.
        var borders = Real().Screens.Values.SelectMany(s => s.Elements).Select(e => e.Border)
            .Concat(Real().Templates.Values.SelectMany(t => t.Parts).Select(p => p.Border))
            .Where(b => b?.Sides is { Length: > 0 }).ToList();
        Assert.NotEmpty(borders);
        Assert.All(borders, b => Assert.All(b!.Sides!,
            s => Assert.Contains(s, new[] { "top", "bottom", "left", "right" })));
    }

    [Fact]
    public void PaletteValuesAreHexColors()
    {
        Assert.NotEmpty(Real().Style.Palette);
        Assert.All(Real().Style.Palette.Values, v => Assert.StartsWith("#", v));
    }

    [Fact]
    public void EveryFrameCarriesAnAssetAndFourSliceMargins()
    {
        // §10 nine-slice: the style frame library + any element frame must give the blitter an asset
        // path and 4 slice margins [L,T,R,B]. (Empty is fine -- the game just skips framing then.)
        var elementFrames = Real().Screens.Values
            .SelectMany(s => s.Elements).Select(e => e.Frame).Where(f => f is not null)!;
        foreach (var f in Real().Style.Frames.Values.Concat(elementFrames!))
        {
            Assert.False(string.IsNullOrEmpty(f!.Asset));
            Assert.Equal(4, f.Slice.Length);
        }
    }

    [Fact]
    public void EveryElementShadowParsesWithSaneFields()
    {
        // §10 drop shadow: any element shadow must give the renderer usable numbers -- a non-negative
        // blur and an opacity in [0,1]. The manifest carries at least one (a titled text shadow).
        var shadows = Real().Screens.Values.SelectMany(s => s.Elements)
            .Select(e => e.Shadow).Where(sh => sh is not null).ToList();
        Assert.NotEmpty(shadows);
        Assert.All(shadows, sh =>
        {
            Assert.True(sh!.Blur >= 0);
            Assert.InRange(sh.Opacity, 0.0, 1.0);
        });
    }
}
