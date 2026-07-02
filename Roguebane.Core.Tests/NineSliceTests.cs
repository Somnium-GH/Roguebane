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

    [Fact]
    public void TiledEdgesRepeatAtNativeSizeAndCropTheTail()
    {
        // Frame v3 `repeat:"tile"`: a 90x90 frame with 30px margins over a 300-wide dest tiles the top
        // edge in native 30px chunks; the last chunk crops BOTH src and dest to the remainder (no squash).
        var patches = NineSlice.Patches(90, 90, new[] { 30, 30, 30, 30 },
            new LayoutRect(0, 0, 300, 90), tile: true, centerFill: true);
        var top = patches.Where(p => p.Src.Y == 0 && p.Src.X == 30 && p.Dst.Y == 0).ToList();
        Assert.Equal(8, top.Count);                       // 240px band / 30px tiles
        Assert.All(top, p => Assert.Equal(p.Src.W, p.Dst.W)); // every chunk blits 1:1 wide
        Assert.Equal(240, top.Sum(p => p.Dst.W));         // full coverage, no overlap
    }

    [Fact]
    public void CenterFillFalseLeavesTheMiddleOpen()
    {
        var patches = NineSlice.Patches(90, 90, new[] { 30, 30, 30, 30 },
            new LayoutRect(0, 0, 300, 300), tile: false, centerFill: false);
        Assert.Equal(8, patches.Count); // 9 minus the omitted centre
        Assert.DoesNotContain(patches, p => p.Src.X == 30 && p.Src.Y == 30);
    }
}
