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
    public void ParsesElementParts()
    {
        // §12 schema (test-owned fixture, not CD content): an element may carry named value/label
        // sub-parts with element-local rects — the parts carry the text, the element keeps chrome.
        var m = LayoutManifest.Parse("""
        {
          "screens": { "s": { "designSize": [960,540], "elements": [
            { "id": "tile", "type": "text", "anchor": "TopLeft", "offset": [0,0], "size": [41,35],
              "z": 1, "binds": "x.v", "align": "center", "parts": [
                { "part": "value", "rect": [1,12,38,10], "color": "gold", "font": "mono",
                  "fontPx": 10, "align": "center", "sample": "20", "binds": "x.v" },
                { "part": "label", "rect": [1,24,38,6], "color": "mutedDim", "font": "mono",
                  "fontPx": 4.5, "align": "center", "content": "BASE HP" } ] } ] } }
        }
        """);
        var el = m.Screens["s"].Elements[0];
        Assert.Equal("center", el.Align);
        Assert.Equal(2, el.Parts.Length);
        Assert.Equal("value", el.Parts[0].Part);
        Assert.Equal("x.v", el.Parts[0].Binds);
        Assert.Equal("20", el.Parts[0].Sample);
        Assert.Equal("BASE HP", el.Parts[1].Content);
        Assert.Equal(4.5, el.Parts[1].FontPx);
    }

    [Fact]
    public void ParsesElementImageBindIncludingStaticPatternPaths()
    {
        // §12 (test-owned fixture): an element-level imageBind is either {bind}-templated (per-datum
        // icon) or a STATIC path — static means TILE that PNG across the element rect (patterns).
        var m = LayoutManifest.Parse("""
        {
          "screens": { "s": { "designSize": [960,540], "elements": [
            { "id": "stripes", "type": "text", "anchor": "TopLeft", "offset": [0,0], "size": [257,19],
              "z": 1, "imageBind": "ui/pattern/doom_stripe" },
            { "id": "icon", "type": "icon", "anchor": "TopLeft", "offset": [0,0], "size": [20,20],
              "z": 2, "binds": "node", "imageBind": "icons/node/{node.type}" } ] } }
        }
        """);
        var els = m.Screens["s"].Elements;
        Assert.Equal("ui/pattern/doom_stripe", els[0].ImageBind);
        Assert.DoesNotContain('{', els[0].ImageBind!); // static = pattern-tile semantics
        Assert.Contains('{', els[1].ImageBind!);       // templated = per-datum icon
    }

    [Fact]
    public void ParsesPerStatePartLabelsAndTheySurvivePlacement()
    {
        // §12 (test-owned fixture): a template part may restyle per state INCLUDING its label
        // text (selection chips: CHOOSE vs the chosen check) — and placement carries the states.
        var m = LayoutManifest.Parse("""
        {
          "templates": { "card": { "size": [200,100], "parts": [
            { "rect": [10,80,60,12], "font": "mono", "fontPx": 5, "binds": "x.selection",
              "sample": "CHOSEN",
              "states": { "chosen": { "fill": "amber", "color": "ground", "label": "CHOSEN" },
                          "idle": { "color": "mutedDim", "border": "borderDim", "label": "CHOOSE" } } } ] } }
        }
        """);
        var p = m.Templates["card"].Parts[0];
        Assert.Equal(System.Text.Json.JsonValueKind.Object, p.States.ValueKind);
        Assert.Equal("CHOOSE", p.States.GetProperty("idle").GetProperty("label").GetString());
        var placed = CardTemplate.Place(m.Templates["card"], 5, 5);
        Assert.Equal("CHOOSE", placed[0].States.GetProperty("idle").GetProperty("label").GetString());
    }

    [Fact]
    public void EveryElementPartIsAWellFormedTextRun()
    {
        // Quantifies over whatever CD authored: every element part names itself, sits INSIDE its
        // element (4-int element-local rect), carries a text source (content/binds/sample), a
        // positive fontPx, and a known align.
        var m = Real();
        foreach (var s in m.Screens.Values)
            foreach (var e in s.Elements)
                foreach (var p in e.Parts)
                {
                    Assert.False(string.IsNullOrEmpty(p.Part));
                    Assert.Equal(4, p.Rect.Length);
                    Assert.True(p.Rect[0] >= 0 && p.Rect[1] >= 0
                        && p.Rect[0] + p.Rect[2] <= e.Size[0] && p.Rect[1] + p.Rect[3] <= e.Size[1],
                        $"part '{p.Part}' rect escapes its element ({e.Id})");
                    Assert.False(string.IsNullOrEmpty(p.Content) && string.IsNullOrEmpty(p.Binds)
                        && string.IsNullOrEmpty(p.Sample), $"part '{p.Part}' of {e.Id} has no text source");
                    Assert.True(p.FontPx > 0, $"part '{p.Part}' of {e.Id} has no fontPx");
                    if (p.Align is not null)
                        Assert.Contains(p.Align, new[] { "left", "center", "right" });
                }
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
    public void FullBarStretchElementsExistInEveryScreenThatDeclaresThem()
    {
        // Game1.ManifestRenderer's FullBarIds ("statusStrip","footer") hardcodes which element ids
        // get full-width-stretch treatment on maximize/resize -- a real code dependency on these
        // literal ids, not incidental CD content. A silent CD rename would drop the stretch behavior
        // with no red test (the fragility this pins down). Intentionally named ids, unlike the rest
        // of this file's contract-only assertions -- tripwire for that one dependency, not a general
        // content pin.
        var fullBarIds = new[] { "statusStrip", "footer" };
        var allIds = Real().Screens.Values.SelectMany(s => s.Elements).Select(e => e.Id).ToHashSet();
        Assert.All(fullBarIds, id => Assert.Contains(id, allIds));
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
    public void ParsesThePulsePrimitiveFromAFixture()
    {
        // CD #30 (LOCKED 2026-07-04, test-owned fixture): ONE fixed-tick primitive drives border-
        // alpha breathe, glow ring+halo breathe, and whole-element ("self") alpha breathe.
        var m = LayoutManifest.Parse("""
        {
          "style": { "pulse": { "periodMs": 1800, "easing": "easeInOut", "clock": "fixedTick",
            "border": { "alphaLo": 0.45, "alphaHi": 1 },
            "glow": { "ring": { "w": 1.5, "alphaLo": 0.4, "alphaHi": 0.73 },
                      "halo": { "blur": 11, "alphaLo": 0, "alphaHi": 0.33 } },
            "self": { "alphaLo": 0.45, "alphaHi": 1 } } }
        }
        """);
        var p = m.Style.Pulse;
        Assert.Equal(1800, p.PeriodMs);
        Assert.Equal(0.45, p.Border.AlphaLo);
        Assert.Equal(1, p.Border.AlphaHi);
        Assert.Equal(1.5, p.Glow.Ring.W);
        Assert.Equal(0.4, p.Glow.Ring.AlphaLo);
        Assert.Equal(0.73, p.Glow.Ring.AlphaHi);
        Assert.Equal(11, p.Glow.Halo.Blur);
        Assert.Equal(0.33, p.Glow.Halo.AlphaHi);
        Assert.Equal(0.45, p.Self.AlphaLo);
    }

    [Fact]
    public void RealManifestCarriesASanePulsePrimitiveAndAtLeastOneFlaggedState()
    {
        // Contract-only: whatever periodMs/alpha values CD tunes, they must stay in sane ranges, and
        // at least one template state must actually opt into pulse/glow (else the primitive is dead
        // code) -- without pinning WHICH template/state does so.
        var m = Real();
        Assert.True(m.Style.Pulse.PeriodMs > 0);
        Assert.InRange(m.Style.Pulse.Border.AlphaLo, 0.0, 1.0);
        Assert.InRange(m.Style.Pulse.Border.AlphaHi, 0.0, 1.0);
        var flagged = m.Templates.Values
            .Where(t => t.States.ValueKind == System.Text.Json.JsonValueKind.Object)
            .SelectMany(t => t.States.EnumerateObject())
            .Where(st => st.Value.ValueKind == System.Text.Json.JsonValueKind.Object)
            .Count(st => st.Value.TryGetProperty("pulse", out _) || st.Value.TryGetProperty("glow", out _));
        Assert.True(flagged > 0, "no template state opts into pulse/glow -- CD #30 primitive would be unused");
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

    [Fact]
    public void CountWidthParsesAndComputesTheReflowFormula()
    {
        // B27 (CD_STATUS #38): a minion column's width is DERIVED from the core's minion capacity, not
        // its authored size, and it vanishes at zero. Test-owned fixture pins the SCHEMA parse; the
        // formula is asserted with the real minionGroup params (item 78, gap 6, pad 8) -- note count 2
        // gives 170, which is exactly the authored minionGroup size, i.e. the authored box is the 2-slot
        // case and countWidth generalizes it.
        var m = LayoutManifest.Parse("""
        {
          "screens": { "s": { "designSize": [960,540], "elements": [
            { "id": "col", "type": "panel", "anchor": "Right", "offset": [0,0], "size": [170,146],
              "z": 1, "countWidth": { "bind": "minions.cap", "item": 78, "gap": 6, "pad": 8,
                "hideAtZero": true } } ] } }
        }
        """);
        var cw = m.Screens["s"].Elements[0].CountWidth;
        Assert.NotNull(cw);
        Assert.Equal("minions.cap", cw!.Bind);
        Assert.True(cw.HideAtZero);
        // count*item + (count-1)*gap + pad; zero => pad alone (no negative gap); one => no gaps.
        Assert.Equal(8, cw.WidthFor(0));            // pad only
        Assert.Equal(86, cw.WidthFor(1));           // 78 + 0 + 8
        Assert.Equal(170, cw.WidthFor(2));          // 156 + 6 + 8  == authored size
        Assert.Equal(254, cw.WidthFor(3));          // 234 + 12 + 8
        Assert.Equal(8, cw.WidthFor(-1));           // negative clamps to zero-count
        // hideAtZero collapses only the empty case.
        Assert.False(cw.VisibleAt(0));
        Assert.True(cw.VisibleAt(1));
        Assert.True(cw.VisibleAt(2));
    }

    [Fact]
    public void NodeConditionalPopupIdsMatchTheirGatePrefix()
    {
        // Cross-layer CONTRACT, not CD content (Doug 2026-07-12: an empty "QUEST" box floated over a
        // live fight). The quest/camp popups are flat sibling clusters with no parent link, so the Game
        // renderer hides each cluster by id-PREFIX (NodeGateBindFor) off the panel's bind. This pins the
        // assumption that gate depends on: whatever CD names them, an `encounter.quest`-bound element's
        // id must start "quest" and an `encounter.camp`-bound one "campMarker" -- otherwise the prefix
        // gate would miss it and the box would leak back onto fight nodes. Tolerant: if CD drops the
        // binds entirely there is nothing to check; only a prefix MISMATCH reddens -- which is exactly
        // when the Game-side gate needs updating in lockstep.
        foreach (var s in Real().Screens.Values)
            foreach (var e in s.Elements)
            {
                if (e.Binds == "encounter.quest")
                    Assert.StartsWith("quest", e.Id, StringComparison.Ordinal);
                if (e.Binds == "encounter.camp")
                    Assert.StartsWith("campMarker", e.Id, StringComparison.Ordinal);
            }
    }
}
