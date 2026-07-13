namespace Roguebane.Core.Layout;

// Pure geometry for "which anatomy stat is under this screen point" — factored out of the shell
// (Game1.FoePartAt) so the Z-order tie-break (frontmost-part-wins on a rect overlap, §8 targeting)
// is headless-testable instead of only verifiable by a live click.
public static class FigureHitTest
{
    // boxX/Y/W/H: the on-screen box the figure is drawn into (feet at bottom-centre, scaled to boxH,
    // matching DrawHumanoid's SX/SY). px,py: the screen point to test (e.g. mouse cursor).
    // Returns BOTH the stat clicked AND the visual PAIR INDEX (0/1 for a paired limb like armL/armR,
    // -1 for an unpaired part) so a caller can resolve WHICH physical limb was hit, not just the stat
    // (Doug bug: aiming either arm always resolved to the same part because only the stat was reported).
    public static (Stat Stat, int PairIndex)? StatAt(Figure fig, int boxX, int boxY, int boxW, int boxH, int px, int py)
    {
        var f = (float)boxH / fig.Size[1];
        int cx = boxX + boxW / 2, cy = boxY + boxH;
        var pivotX = fig.Pivot[0]; var pivotY = fig.Pivot[1];
        (Stat Stat, int PairIndex)? hit = null;
        foreach (var name in fig.Z) // back -> front; the last match under the point is frontmost
        {
            if (!fig.Parts.TryGetValue(name, out var part) || FigureBinding.StatOf(name) is not { } stat) continue;
            int rx = cx + (int)((part.Rect[0] - pivotX) * f);
            int ry = cy + (int)((part.Rect[1] - pivotY) * f);
            int rw = (int)(part.Rect[2] * f);
            int rh = (int)(part.Rect[3] * f);
            if (px < rx || px >= rx + rw || py < ry || py >= ry + rh) continue;
            hit = (stat, FigureBinding.PairIndexOf(name));
        }
        return hit;
    }
}
