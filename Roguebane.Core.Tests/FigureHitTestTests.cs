using Roguebane.Core.Layout;

namespace Roguebane.Core.Tests;

// P1 (2026-07-05, STATUS): a prior fix's closing note claimed "arms sit behind the chest" and that
// claim was backwards — armL/armR paint AFTER torso in every current figure's Z list, so the
// frontmost-wins tie-break should already favor the arm on any rect overlap. This pins that as a
// property over whatever CD authors (not specific figure names/rects), so a future Z-order or
// tie-break regression is caught headlessly instead of needing another live click to notice.
public class FigureHitTestTests
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

    // Figure-space (fx,fy) -> screen point, replicating StatAt/DrawHumanoid's own SX/SY transform
    // for an identity box (boxX=0, boxY=0, boxW/H=fig.Size — i.e. 1:1 scale, no offset).
    private static (int x, int y) ToScreen(Figure fig, int fx, int fy)
    {
        int cx = fig.Size[0] / 2, cy = fig.Size[1];
        return (cx + (fx - fig.Pivot[0]), cy + (fy - fig.Pivot[1]));
    }

    [Fact]
    public void ArmWinsOverTorsoWhereverTheirRectsOverlapOnScreen()
    {
        var m = Real();
        var checkedAnyOverlap = false;
        foreach (var (name, fig) in m.Figures)
        {
            if (!fig.Parts.TryGetValue("torso", out var torso)) continue;
            foreach (var armName in new[] { "armL", "armR" })
            {
                if (!fig.Parts.TryGetValue(armName, out var arm)) continue;
                var overlapX0 = Math.Max(torso.Rect[0], arm.Rect[0]);
                var overlapX1 = Math.Min(torso.Rect[0] + torso.Rect[2], arm.Rect[0] + arm.Rect[2]);
                var overlapY0 = Math.Max(torso.Rect[1], arm.Rect[1]);
                var overlapY1 = Math.Min(torso.Rect[1] + torso.Rect[3], arm.Rect[1] + arm.Rect[3]);
                if (overlapX1 <= overlapX0 || overlapY1 <= overlapY0) continue; // this figure's arm/torso don't overlap

                checkedAnyOverlap = true;
                var (sx, sy) = ToScreen(fig, (overlapX0 + overlapX1) / 2, (overlapY0 + overlapY1) / 2);
                var stat = FigureHitTest.StatAt(fig, 0, 0, fig.Size[0], fig.Size[1], sx, sy);
                Assert.True(stat == Stat.Str,
                    $"{name}.{armName} overlaps torso at figure-space ({(overlapX0 + overlapX1) / 2},{(overlapY0 + overlapY1) / 2}) but hit-test resolved to {stat}, not Str — Z tie-break no longer favors the arm.");
            }
        }
        Assert.True(checkedAnyOverlap, "fixture has no figure with an arm/torso rect overlap to test against");
    }

    [Fact]
    public void ArmRectAreaOutsideTorsoAlwaysResolvesToTheArmRegardlessOfZOrder()
    {
        var m = Real();
        var checkedAny = false;
        foreach (var (name, fig) in m.Figures)
        {
            if (!fig.Parts.TryGetValue("torso", out var torso)) continue;
            foreach (var armName in new[] { "armL", "armR" })
            {
                if (!fig.Parts.TryGetValue(armName, out var arm)) continue;
                // A point inside the arm's rect but outside torso's rect (e.g. the outer edge) can only
                // be the arm — no Z tie-break involved. Probe the arm's outermost 1px column.
                var probeFx = arm.Rect[0] < torso.Rect[0] ? arm.Rect[0] : arm.Rect[0] + arm.Rect[2] - 1;
                var probeFy = arm.Rect[1] + arm.Rect[3] / 2;
                if (probeFx >= torso.Rect[0] && probeFx < torso.Rect[0] + torso.Rect[2]) continue; // fully nested arm rect; skip

                checkedAny = true;
                var (sx, sy) = ToScreen(fig, probeFx, probeFy);
                var stat = FigureHitTest.StatAt(fig, 0, 0, fig.Size[0], fig.Size[1], sx, sy);
                Assert.True(stat == Stat.Str,
                    $"{name}.{armName}'s own outer edge (figure-space {probeFx},{probeFy}) resolved to {stat}, not Str.");
            }
        }
        Assert.True(checkedAny, "fixture has no figure with an arm rect extending outside torso to test against");
    }
}
