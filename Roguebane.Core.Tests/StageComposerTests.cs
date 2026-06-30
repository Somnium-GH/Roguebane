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

    [Fact]
    public void PartsComeOutInFigureZOrderWithManifestRects()
    {
        var m = Manifest();
        var fig = m.Figures["grunt"];
        var sc = new StageComposer(m);

        var placed = sc.ComposeFigure("grunt", _ => PartCondition.Healthy, _ => false);

        // Z entries that are real parts, in order; z slots without a part (frontGear) are dropped.
        var expected = fig.Z.Where(z => fig.Parts.ContainsKey(z)).ToArray();
        Assert.Equal(expected, placed.Select(p => p.Part).ToArray());
        Assert.True(placed.Select(p => p.Z).SequenceEqual(placed.Select(p => p.Z).OrderBy(z => z)));
        var torso = placed.Single(p => p.Part == "torso");
        Assert.Equal(fig.Parts["torso"].Rect, torso.Rect);
    }

    [Fact]
    public void ConditionAndArmorPickTheStateKeyedSprite()
    {
        var sc = new StageComposer(Manifest());

        var armored = sc.ComposeFigure("grunt", _ => PartCondition.Damaged, _ => false);
        Assert.Equal("sprites/body/grunt/torso_damaged", armored.Single(p => p.Part == "torso").SpriteKey);

        var bareBroken = sc.ComposeFigure("grunt", _ => PartCondition.Broken, _ => true);
        Assert.Equal("sprites/body/grunt/armL_barebroken", bareBroken.Single(p => p.Part == "armL").SpriteKey);
    }

    [Fact]
    public void GearMountsAnchorAtTheirSocket()
    {
        var m = Manifest();
        var sc = new StageComposer(m);
        var fig = m.Figures["grunt"];

        var gear = sc.ComposeGear("grunt");

        var sword = gear.Single(g => g.Gear == "sword");
        Assert.Equal("handL", sword.Socket);
        Assert.Equal(fig.Sockets["handL"], sword.Anchor);
        Assert.Equal(Array.IndexOf(fig.Z, "frontGear"), sword.Z);
    }
}
