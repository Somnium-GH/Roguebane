using Roguebane.Core.Layout;

namespace Roguebane.Core.Tests;

// PaletteColor parses the manifest's hex palette into engine-agnostic RGBA and resolves named
// entries with a safe fallback.
public class PaletteColorTests
{
    [Fact]
    public void ParsesSixDigitHex()
    {
        Assert.Equal(new Rgba(0xec, 0xe0, 0xcb), PaletteColor.Parse("#ece0cb"));
    }

    [Fact]
    public void ParsesShorthandAndAlpha()
    {
        Assert.Equal(new Rgba(0xff, 0x00, 0x00), PaletteColor.Parse("#f00"));
        Assert.Equal(new Rgba(0x11, 0x22, 0x33, 0x80), PaletteColor.Parse("#11223380"));
    }

    [Fact]
    public void BadInputFailsGracefully()
    {
        Assert.False(PaletteColor.TryParse("nope", out _));
        Assert.False(PaletteColor.TryParse("", out _));
        Assert.False(PaletteColor.TryParse(null, out _));
    }

    [Fact]
    public void NamedFallsBackOnUnknownKey()
    {
        var style = new Style { Palette = { ["amber"] = "#d9a441" } };
        var miss = new Rgba(1, 2, 3);
        Assert.Equal(new Rgba(0xd9, 0xa4, 0x41), PaletteColor.Named(style, "amber", miss));
        Assert.Equal(miss, PaletteColor.Named(style, "nonexistent", miss));
    }

    [Fact]
    public void EveryRealPaletteEntryParses()
    {
        var dir = AppContext.BaseDirectory;
        while (dir is not null && !File.Exists(Path.Combine(dir, "Roguebane.Content", "layout.json")))
            dir = Path.GetDirectoryName(dir);
        var m = LayoutManifest.Parse(File.ReadAllText(Path.Combine(dir!, "Roguebane.Content", "layout.json")));

        Assert.NotEmpty(m.Style.Palette);
        foreach (var (name, hex) in m.Style.Palette)
            Assert.True(PaletteColor.TryParse(hex, out _), $"palette[{name}]={hex} did not parse");
    }
}
