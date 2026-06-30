namespace Roguebane.Core.Layout;

// A parsed RGB(A) colour from the manifest palette (hex strings like "#ece0cb"). Engine-agnostic
// so it stays testable; the shell maps Rgba -> its own colour type. Named lookups resolve against
// style.palette and fall back to a supplied default rather than throwing on an unknown key.
public readonly record struct Rgba(byte R, byte G, byte B, byte A = 255);

public static class PaletteColor
{
    public static bool TryParse(string? hex, out Rgba color)
    {
        color = default;
        if (string.IsNullOrWhiteSpace(hex)) return false;
        var s = hex.Trim();
        if (s.StartsWith('#')) s = s[1..];

        // #rgb, #rrggbb, #rrggbbaa
        if (s.Length == 3)
            s = $"{s[0]}{s[0]}{s[1]}{s[1]}{s[2]}{s[2]}";
        if (s.Length != 6 && s.Length != 8) return false;

        if (!byte.TryParse(s.AsSpan(0, 2), System.Globalization.NumberStyles.HexNumber, null, out var r)) return false;
        if (!byte.TryParse(s.AsSpan(2, 2), System.Globalization.NumberStyles.HexNumber, null, out var g)) return false;
        if (!byte.TryParse(s.AsSpan(4, 2), System.Globalization.NumberStyles.HexNumber, null, out var b)) return false;
        byte a = 255;
        if (s.Length == 8 && !byte.TryParse(s.AsSpan(6, 2), System.Globalization.NumberStyles.HexNumber, null, out a))
            return false;

        color = new Rgba(r, g, b, a);
        return true;
    }

    public static Rgba Parse(string hex)
        => TryParse(hex, out var c) ? c : throw new FormatException($"not a hex colour: {hex}");

    // Resolve a named palette entry (style.palette); unknown name or bad value -> fallback.
    public static Rgba Named(Style style, string name, Rgba fallback)
        => style.Palette.TryGetValue(name, out var hex) && TryParse(hex, out var c) ? c : fallback;
}
