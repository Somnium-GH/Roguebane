namespace Roguebane.Core.Tests;

public class PagerTests
{
    [Fact]
    public void PageCountRoundsUpAndFloorsAtOne()
    {
        var pager = new Pager(3);
        Assert.Equal(1, pager.PageCount(0));
        Assert.Equal(1, pager.PageCount(3));
        Assert.Equal(2, pager.PageCount(4));
        Assert.Equal(3, pager.PageCount(7));
    }

    [Fact]
    public void NextAndPrevClampAtTheEnds()
    {
        var pager = new Pager(3);
        Assert.False(pager.HasPrev(7));
        Assert.True(pager.HasNext(7));

        pager.Prev(7); // already at 0 -> no-op
        Assert.Equal(0, pager.Index(7));

        pager.Next(7);
        pager.Next(7);
        Assert.Equal(2, pager.Index(7)); // last page
        Assert.False(pager.HasNext(7));

        pager.Next(7); // already at last -> no-op
        Assert.Equal(2, pager.Index(7));
    }

    [Fact]
    public void SkipIsIndexTimesPageSize()
    {
        var pager = new Pager(3);
        pager.Next(7);
        Assert.Equal(3, pager.Skip(7));
    }

    [Fact]
    public void IndexSnapsBackWhenTheItemCountShrinks()
    {
        var pager = new Pager(3);
        pager.Next(7);
        pager.Next(7); // page 2 (last of 3 pages over 7 items)
        Assert.Equal(2, pager.Index(7));

        Assert.Equal(0, pager.Index(2)); // only 1 page over 2 items now -> snaps to 0
    }
}
