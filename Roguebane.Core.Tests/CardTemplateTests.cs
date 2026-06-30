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
        var templates = Manifest().Templates;
        Assert.Contains("techCard", templates.Keys);
        foreach (var (name, t) in templates)
        {
            Assert.True(t.Size.Length == 2, $"{name} size");
            Assert.NotEmpty(t.Parts);
            foreach (var p in t.Parts)
            {
                Assert.Equal(4, p.Rect.Length);
                Assert.False(string.IsNullOrEmpty(p.Sample), $"{name} part sample");
            }
        }
    }

    [Fact]
    public void PlaceTranslatesPartsByTheOrigin()
    {
        var tech = Manifest().Templates["techCard"];
        var first = tech.Parts[0];

        var placed = CardTemplate.Place(tech, 100, 200);

        Assert.Equal(tech.Parts.Length, placed.Count);
        Assert.Equal(new LayoutRect(100 + first.Rect[0], 200 + first.Rect[1], first.Rect[2], first.Rect[3]),
            placed[0].Rect);
        Assert.Equal(first.Sample, placed[0].Sample);
        Assert.Equal(first.Color, placed[0].Color);
    }
}
