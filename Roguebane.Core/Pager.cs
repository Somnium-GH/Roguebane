namespace Roguebane.Core;

// Shared paging math for any manifest list too long for one screen (NewGame's 7 cores, Inventory's
// tabs, the merchant's ware shelves) — one clamped page-index model instead of each screen hand-
// rolling its own Math.Max/Math.Min. Page count derives from live item count each call, so a screen
// that reflows its list (buy/sell, tab switch) never needs to reset the pager itself.
public sealed class Pager
{
    private readonly int _pageSize;
    private int _index;

    public Pager(int pageSize) => _pageSize = Math.Max(1, pageSize);

    public int PageCount(int itemCount) => Math.Max(1, (itemCount + _pageSize - 1) / _pageSize);

    // Clamped to the live item count -- a page that no longer exists (items shrank) snaps back.
    public int Index(int itemCount) => _index = Math.Clamp(_index, 0, PageCount(itemCount) - 1);

    public int Skip(int itemCount) => Index(itemCount) * _pageSize;

    public bool HasPrev(int itemCount) => Index(itemCount) > 0;
    public bool HasNext(int itemCount) => Index(itemCount) < PageCount(itemCount) - 1;

    public void Prev(int itemCount) => _index = Math.Max(0, Index(itemCount) - 1);
    public void Next(int itemCount) => _index = Math.Min(PageCount(itemCount) - 1, Index(itemCount) + 1);
}
