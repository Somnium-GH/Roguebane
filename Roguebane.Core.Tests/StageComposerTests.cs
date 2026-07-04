using Roguebane.Core.Layout;

namespace Roguebane.Core.Tests;

// The stage composer turns figure geometry + Core condition into ordered, state-keyed
// placements. Pinned against the real manifest so the figure assembly survives drops.
public class StageComposerTests
{
    private static LayoutManifest Manifest()
    {
        var dir = AppContext.BaseDirectory;
        while (dir is not null)
        {
            var c = Path.Combine(dir, "Roguebane.Content", "layout.json");
            if (File.Exists(c)) return LayoutManifest.Parse(File.ReadAllText(c));
            dir = Path.GetDirectoryName(dir);
        }
        throw new FileNotFoundException("layout.json");
    }

    // Schema, not literal keys (CD owns figure ids): quantify over whatever figures CD ships.
    [Fact]
    public void PartsComeOutInFigureZOrderWithManifestRects()
    {
        var m = Manifest();
        var (name, fig) = m.Figures.First();
        var sc = new StageComposer(m);

        var placed = sc.ComposeFigure(name, _ => PartCondition.Healthy, _ => false);

        // Z entries that are real parts, in order; z slots without a part (frontGear) are dropped.
        var expected = fig.Z.Where(z => fig.Parts.ContainsKey(z)).ToArray();
        Assert.Equal(expected, placed.Select(p => p.Part).ToArray());
        Assert.True(placed.Select(p => p.Z).SequenceEqual(placed.Select(p => p.Z).OrderBy(z => z)));
        Assert.All(placed, p => Assert.Equal(fig.Parts[p.Part].Rect, p.Rect));
    }

    [Fact]
    public void ConditionPicksTheStateKeyedSprite()
    {
        var m = Manifest();
        var (name, fig) = m.Figures.First();
        var part = fig.Z.First(z => fig.Parts.ContainsKey(z)); // a real, drawn part
        var sc = new StageComposer(m);

        // SpriteKey convention: sprites/body/<figure>/<part>_<state> (+ bare variant when armour is off).
        var damaged = sc.ComposeFigure(name, _ => PartCondition.Damaged, _ => false).Single(p => p.Part == part);
        Assert.Equal($"sprites/body/{name}/{part}_damaged", damaged.SpriteKey);

        var broken = sc.ComposeFigure(name, _ => PartCondition.Broken, _ => true).Single(p => p.Part == part);
        Assert.Contains("broken", broken.SpriteKey);
    }

    [Fact]
    public void OptionalSpriteRowsCarryOrderedFallbackKeys()
    {
        // Bare art is optional per figure and a condition row may be absent — the composer emits
        // ordered substitutes (bare -> armored same-condition -> armored healthy) so the shell
        // never draws the null-texture gap box for a merely-optional row.
        var m = Manifest();
        var (name, fig) = m.Figures.First();
        var part = fig.Z.First(z => fig.Parts.ContainsKey(z));
        var sc = new StageComposer(m);

        var bareDamaged = sc.ComposeFigure(name, _ => PartCondition.Damaged, _ => true).Single(p => p.Part == part);
        Assert.Equal(new[]
        {
            $"sprites/body/{name}/{part}_damaged",
            $"sprites/body/{name}/{part}_healthy",
        }, bareDamaged.Fallbacks);

        var armoredHealthy = sc.ComposeFigure(name, _ => PartCondition.Healthy, _ => false).Single(p => p.Part == part);
        Assert.Empty(armoredHealthy.Fallbacks); // the base row IS the ask; a miss there is a real gap
    }

    [Fact]
    public void GearMountsAnchorAtTheirSocket()
    {
        var m = Manifest();
        var sc = new StageComposer(m);
        var (name, fig) = m.Figures.First(kv => kv.Value.Mounts.Length > 0); // a figure that wields gear

        var gear = sc.ComposeGear(name);

        Assert.NotEmpty(gear);
        Assert.All(gear, g =>
        {
            Assert.True(fig.Sockets.ContainsKey(g.Socket));
            Assert.Equal(fig.Sockets[g.Socket], g.Anchor);
        });
    }
}
