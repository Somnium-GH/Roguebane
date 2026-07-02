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
        => Patches(srcW, srcH, slice, dst, tile: false, centerFill: true);

    // Frame v3 (CD #16): `tile` repeats edge/centre patches at NATIVE size along their stretch axes
    // (the trailing chunk crops 1:1 instead of squashing); `centerFill:false` leaves the middle open.
    // Corners always stay fixed. The classic overload above keeps the stretch-everything behaviour.
    public static IReadOnlyList<NinePatch> Patches(int srcW, int srcH, int[] slice, LayoutRect dst,
        bool tile, bool centerFill, double dstCornerScale = 1)
    {
        var l = slice.Length > 0 ? slice[0] : 0;
        var t = slice.Length > 1 ? slice[1] : 0;
        var r = slice.Length > 2 ? slice[2] : 0;
        var b = slice.Length > 3 ? slice[3] : 0;
        // Art authored above design scale (1080-class skins) keeps its SOURCE margins but lands
        // smaller DESTINATION corners: dst corner = slice * dstCornerScale (e.g. 0.5 for 2x art).
        var ld = (int)Math.Round(l * dstCornerScale);
        var td = (int)Math.Round(t * dstCornerScale);
        var rd = (int)Math.Round(r * dstCornerScale);
        var bd = (int)Math.Round(b * dstCornerScale);

        // Column/row cut lines in the SOURCE and the DESTINATION. The middle band stretches to fill
        // whatever is left after the fixed corner margins; clamped >= 0 so a too-small dst can't invert.
        var sx = new[] { 0, l, srcW - r, srcW };
        var sy = new[] { 0, t, srcH - b, srcH };
        var dx = new[] { dst.X, dst.X + ld, dst.X + Math.Max(ld, dst.W - rd), dst.X + Math.Max(ld + rd, dst.W) };
        var dy = new[] { dst.Y, dst.Y + td, dst.Y + Math.Max(td, dst.H - bd), dst.Y + Math.Max(td + bd, dst.H) };

        var patches = new List<NinePatch>(9);
        for (var row = 0; row < 3; row++)
            for (var col = 0; col < 3; col++)
            {
                var src = new LayoutRect(sx[col], sy[row], sx[col + 1] - sx[col], sy[row + 1] - sy[row]);
                var dest = new LayoutRect(dx[col], dy[row], dx[col + 1] - dx[col], dy[row + 1] - dy[row]);
                if (src.W <= 0 || src.H <= 0 || dest.W <= 0 || dest.H <= 0) continue; // skip degenerate
                var centre = row == 1 && col == 1;
                if (centre && !centerFill) continue; // open middle: the frame is chrome only
                if (!tile || (row != 1 && col != 1)) { patches.Add(new NinePatch(src, dest)); continue; }
                // Tile along the axes this band stretches on (x for top/bottom + centre, y for the
                // side edges + centre); the last chunk crops the SOURCE 1:1 so nothing squashes.
                var stepX = col == 1 ? src.W : dest.W;
                var stepY = row == 1 ? src.H : dest.H;
                for (var oy = 0; oy < dest.H; oy += stepY)
                    for (var ox = 0; ox < dest.W; ox += stepX)
                    {
                        var w = Math.Min(stepX, dest.W - ox);
                        var h = Math.Min(stepY, dest.H - oy);
                        var sw = col == 1 ? Math.Min(src.W, w) : src.W;
                        var sh = row == 1 ? Math.Min(src.H, h) : src.H;
                        patches.Add(new NinePatch(
                            new LayoutRect(src.X, src.Y, sw, sh),
                            new LayoutRect(dest.X + ox, dest.Y + oy, w, h)));
                    }
            }
        return patches;
    }
}
