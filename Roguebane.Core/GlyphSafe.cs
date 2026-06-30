using System.Text;

namespace Roguebane.Core;

// The render shell's SpriteFonts are ASCII-only and THROW on a glyph they lack — one stray typographic
// dash once crashed run-start. This is the pure, engine-agnostic guard: fold common typographic chars
// down to ASCII and replace anything still missing, so no string the shell draws can crash it. The
// shell supplies the font's available glyph set; the policy + algorithm live here, headless-testable.
public static class GlyphSafe
{
    // Common typographic chars (escaped so this file stays ASCII) -> their plain-ASCII stand-ins.
    private static readonly Dictionary<char, char> Fix = new()
    {
        ['—'] = '-', ['–'] = '-', ['·'] = '*', ['×'] = 'x',
        ['→'] = '>', ['←'] = '<', ['•'] = '*', ['…'] = '.',
        ['’'] = '\'', ['‘'] = '\'', ['“'] = '"', ['”'] = '"',
        ['»'] = '>', ['«'] = '<',
    };

    // Return a string every char of which the font can draw: pass-through what it has, fold a known
    // typographic char to its ASCII twin when the font has that, else '?' (or space if even '?' is
    // missing). Allocates only when a substitution is actually needed.
    public static string Sanitize(string? s, ISet<char> available)
    {
        if (string.IsNullOrEmpty(s)) return s ?? "";
        StringBuilder? sb = null;
        for (var i = 0; i < s.Length; i++)
        {
            var c = s[i];
            if (available.Contains(c)) { sb?.Append(c); continue; }
            var r = Fix.TryGetValue(c, out var f) && available.Contains(f) ? f
                : available.Contains('?') ? '?' : ' ';
            (sb ??= new StringBuilder(s.Substring(0, i), s.Length + 8)).Append(r);
        }
        return sb?.ToString() ?? s;
    }
}
