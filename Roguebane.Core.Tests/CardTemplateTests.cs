using Roguebane.Core.Layout;

namespace Roguebane.Core.Tests;

// Templates parse into a typed model and stamp at an absolute origin (card-local rects translated,
// style + sample carried). Pinned against the real manifest so card schema drift fails a test.
public class CardTemplateTests
{
    private static LayoutManifest Manifest()
    {
        var dir = AppContext.BaseDirectory;
        while (dir is not null && !File.Exists(Path.Combine(dir, "Roguebane.Content", "layout.json")))
            dir = Path.GetDirectoryName(dir);
        return LayoutManifest.Parse(File.ReadAllText(Path.Combine(dir!, "Roguebane.Content", "layout.json")));
    }

    [Fact]
    public void EveryTemplateParsesWithSizeAndStyledParts()
    {
        // Schema, not literal keys: whatever templates CD ships, each is sized and every part fills its
        // slot with content (sample/image/imageBind/binds) OR visible chrome (fill/border) — the v4 drop
        // authors chrome-only card parts (dividers, slot backgrounds). A template with NO parts must be a
        // self-styled leaf (its own binds/fill carry the visual — e.g. a shield pip), never a blank box.
        var templates = Manifest().Templates;
        Assert.NotEmpty(templates);
        foreach (var (name, t) in templates)
        {
            Assert.True(t.Size.Length == 2, $"{name} size");
            if (t.Parts.Length == 0)
                Assert.False(string.IsNullOrEmpty(t.Binds) && t.Fill is null,
                    $"{name} has no parts and no self-style — a blank template");
            foreach (var p in t.Parts)
            {
                Assert.Equal(4, p.Rect.Length);
                Assert.False(string.IsNullOrEmpty(p.Sample) && string.IsNullOrEmpty(p.Image)
                    && string.IsNullOrEmpty(p.Binds) && string.IsNullOrEmpty(p.ImageBind)
                    && p.Fill is null && p.Border is null,
                    $"{name} part needs content (sample/image/imageBind/binds) or chrome (fill/border)");
            }
        }
    }

    [Fact]
    public void PlaceCarriesImageAndPerPartBinds()
    {
        // A card part can be an image slot or a text slot, and names the live datum it binds at render.
        var t = new Template
        {
            Size = new[] { 100, 40 },
            Parts = new[]
            {
                new TemplatePart { Rect = new[] { 2, 3, 20, 20 }, Image = "fig.png", Binds = "figure" },
                new TemplatePart { Rect = new[] { 30, 4, 60, 8 }, Sample = "Name", Binds = "title" },
            },
        };

        var placed = CardTemplate.Place(t, 100, 200);

        Assert.Equal(new LayoutRect(102, 203, 20, 20), placed[0].Rect);
        Assert.Equal("fig.png", placed[0].Image);
        Assert.Equal("figure", placed[0].Binds);
        Assert.Equal("title", placed[1].Binds);
        Assert.Null(placed[1].Image);
    }

    [Fact]
    public void PlaceTranslatesPartsByTheOrigin()
    {
        var tech = Manifest().Templates.Values.First(t => t.Parts.Length > 0); // any real parts-carrying template
        var first = tech.Parts[0];

        var placed = CardTemplate.Place(tech, 100, 200);

        Assert.Equal(tech.Parts.Length, placed.Count);
        Assert.Equal(new LayoutRect(100 + first.Rect[0], 200 + first.Rect[1], first.Rect[2], first.Rect[3]),
            placed[0].Rect);
        Assert.Equal(first.Sample, placed[0].Sample);
        Assert.Equal(first.Color, placed[0].Color);
    }
}
