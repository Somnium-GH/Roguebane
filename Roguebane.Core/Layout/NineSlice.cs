namespace Roguebane.Core.Layout;

// One patch of a nine-slice blit: copy the source-texture rect Src onto the destination rect Dst
// (the consumer stretches Src->Dst). Corners keep their size; edges + centre stretch.
public readonly record struct NinePatch(LayoutRect Src, LayoutRect Dst);

// Nine-slice frame geometry (§10), pure + headless. Given the source texture size, the slice margins
// [L,T,R,B], and a destination rect, it returns up to 9 source->dest patch pairs: 4 fixed corners, 4
// edges stretched along their axis, and the stretched centre — so ONE painted frame wraps any size.
public static class NineSlice
{
    public static IReadOnlyList<NinePatch> Patches(int srcW, int srcH, int[] slice, LayoutRect dst)
    {
        var l = slice.Length > 0 ? slice[0] : 0;
        var t = slice.Length > 1 ? slice[1] : 0;
        var r = slice.Length > 2 ? slice[2] : 0;
        var b = slice.Length > 3 ? slice[3] : 0;

        // Column/row cut lines in the SOURCE and the DESTINATION. The middle band stretches to fill
        // whatever is left after the fixed corner margins; clamped >= 0 so a too-small dst can't invert.
        var sx = new[] { 0, l, srcW - r, srcW };
        var sy = new[] { 0, t, srcH - b, srcH };
        var dx = new[] { dst.X, dst.X + l, dst.X + Math.Max(l, dst.W - r), dst.X + Math.Max(l + r, dst.W) };
        var dy = new[] { dst.Y, dst.Y + t, dst.Y + Math.Max(t, dst.H - b), dst.Y + Math.Max(t + b, dst.H) };

        var patches = new List<NinePatch>(9);
        for (var row = 0; row < 3; row++)
            for (var col = 0; col < 3; col++)
            {
                var src = new LayoutRect(sx[col], sy[row], sx[col + 1] - sx[col], sy[row + 1] - sy[row]);
                var dest = new LayoutRect(dx[col], dy[row], dx[col + 1] - dx[col], dy[row + 1] - dy[row]);
                if (src.W <= 0 || src.H <= 0 || dest.W <= 0 || dest.H <= 0) continue; // skip degenerate
                patches.Add(new NinePatch(src, dest));
            }
        return patches;
    }
}
