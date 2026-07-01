using Roguebane.Core.Layout;

namespace Roguebane.Core.Tests;

// Nine-slice frame geometry (§10): a painted frame's fixed corners + stretched edges/centre wrap any
// destination rect. Pure geometry, so it stays headless; the Game shell blits each Src->Dst.
public class NineSliceTests
{
    [Fact]
    public void WrapsADestinationWithNinePatches()
    {
        // 64x64 frame, 16px margins, into a 200x120 box at (100,50).
        var p = NineSlice.Patches(64, 64, new[] { 16, 16, 16, 16 }, new LayoutRect(100, 50, 200, 120));

        Assert.Equal(9, p.Count);
        // Top-left corner: fixed source + fixed dest size.
        Assert.Equal(new LayoutRect(0, 0, 16, 16), p[0].Src);
        Assert.Equal(new LayoutRect(100, 50, 16, 16), p[0].Dst);
        // Centre: the middle source band stretched to fill the interior.
        Assert.Equal(new LayoutRect(16, 16, 32, 32), p[4].Src);
        Assert.Equal(new LayoutRect(116, 66, 168, 88), p[4].Dst);
        // Bottom-right corner: fixed, pinned to the far edge.
        Assert.Equal(new LayoutRect(48, 48, 16, 16), p[8].Src);
        Assert.Equal(new LayoutRect(284, 154, 16, 16), p[8].Dst);
    }

    [Fact]
    public void PatchesTileTheInteriorEdgeToEdge()
    {
        var p = NineSlice.Patches(64, 64, new[] { 16, 16, 16, 16 }, new LayoutRect(0, 0, 200, 120));
        // The top edge (patch 1) sits between the two top corners with no gap or overlap.
        Assert.Equal(16, p[0].Dst.X + p[0].Dst.W);   // TL right edge
        Assert.Equal(16, p[1].Dst.X);                // top-edge left starts there
        Assert.Equal(200 - 16, p[1].Dst.X + p[1].Dst.W); // and ends at the TR corner
    }

    [Fact]
    public void DegenerateSmallDestNeverProducesNegativeRects()
    {
        // A box smaller than the combined corners: middle bands collapse (skipped), no negative dims.
        var p = NineSlice.Patches(64, 64, new[] { 16, 16, 16, 16 }, new LayoutRect(0, 0, 20, 20));
        Assert.NotEmpty(p);
        Assert.All(p, patch =>
        {
            Assert.True(patch.Src.W > 0 && patch.Src.H > 0);
            Assert.True(patch.Dst.W > 0 && patch.Dst.H > 0);
        });
    }
}
