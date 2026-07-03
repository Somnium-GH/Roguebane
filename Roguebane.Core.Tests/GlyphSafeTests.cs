namespace Roguebane.Core.Tests;

// The crash-guard for the ASCII-only render font: no string the shell draws may contain a glyph the
// font lacks (one stray em-dash once crashed run-start). The fold-to-ASCII policy is tested here.
public class GlyphSafeTests
{
    // A stand-in "font" that draws printable ASCII (incl. '?', '-', '*', '<', '>', '\'', '"').
    private static readonly HashSet<char> Ascii =
        new(Enumerable.Range(32, 95).Select(i => (char)i));

    [Fact]
    public void FoldsTypographicDashToAscii()
    {
        Assert.Equal("weak 4-6s", GlyphSafe.Sanitize("weak 4–6s", Ascii)); // en-dash -> '-'
        Assert.Equal("a-b", GlyphSafe.Sanitize("a—b", Ascii));             // em-dash -> '-'
    }

    [Fact]
    public void FoldsArrowsBulletsAndQuotes()
    {
        Assert.Equal("a>b", GlyphSafe.Sanitize("a→b", Ascii)); // ->
        Assert.Equal("* x", GlyphSafe.Sanitize("• ×", Ascii)); // bullet, multiply
        Assert.Equal("'q'", GlyphSafe.Sanitize("‘q’", Ascii)); // curly quotes
    }

    [Fact]
    public void UnknownGlyphFallsBackToQuestionMark()
    {
        Assert.Equal("a?b", GlyphSafe.Sanitize("a☃b", Ascii)); // snowman, no mapping -> '?'
    }

    [Fact]
    public void FallsBackToSpaceWhenEvenQuestionMarkIsMissing()
    {
        var bare = new HashSet<char> { 'a', 'b' }; // no '?'
        Assert.Equal("a b", GlyphSafe.Sanitize("a☃b", bare));
    }

    [Fact]
    public void FoldTargetMissingFromFontStillFallsBack()
    {
        var noDash = new HashSet<char> { 'a', 'b', '?' }; // em-dash maps to '-', but '-' absent
        Assert.Equal("a?b", GlyphSafe.Sanitize("a—b", noDash));
    }

    [Fact]
    public void DropChipGlyphsFoldToAsciiTwins()
    {
        // The 07-03 manifest literals ("BEGIN THE RUN ▶", "✕ CLOSE", "◀"/"▶"
        // pager arrows) fold to readable ASCII instead of the '?' fallback.
        Assert.Equal("BEGIN THE RUN >", GlyphSafe.Sanitize("BEGIN THE RUN ▶", Ascii));
        Assert.Equal("x CLOSE", GlyphSafe.Sanitize("✕ CLOSE", Ascii));
        Assert.Equal("<", GlyphSafe.Sanitize("◀", Ascii));
        Assert.Equal("< LEAVE", GlyphSafe.Sanitize("⮐ LEAVE", Ascii));
    }

    [Fact]
    public void PlainAsciiIsReturnedUnchanged()
    {
        const string s = "SIEGE 12/16";
        Assert.Same(s, GlyphSafe.Sanitize(s, Ascii)); // no allocation when nothing to fix
    }

    [Fact]
    public void EmptyAndNullAreSafe()
    {
        Assert.Equal("", GlyphSafe.Sanitize("", Ascii));
        Assert.Equal("", GlyphSafe.Sanitize(null, Ascii));
    }
}
