using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Roguebane.Core;
using Roguebane.Core.Layout;

namespace Roguebane.Game;

// The CANVAS half of the shell (SRP split, 2026-07-02): the locked palette and every raw draw
// primitive — rects/borders/lines, sprites, glyph-safe text at the supersampled raster, panels,
// bars, buttons. Pure paint helpers with no game or screen knowledge.
public partial class Game1
{
    // Locked palette (ASSET_MANIFEST): ink/muted text, amber highlight, ember/blood, panel + borders.
    private static readonly Color Ink = new(0xec, 0xe0, 0xcb);
    private static readonly Color Muted = new(0x9a, 0x84, 0x68);
    private static readonly Color Amber = new(0xd9, 0xa4, 0x41);
    private static readonly Color Blood = new(0xb2, 0x3b, 0x32);
    private static readonly Color Panel0 = new(0x1d, 0x15, 0x0e);
    private static readonly Color Border0 = new(0x5a, 0x46, 0x36);
    // §10 gradient chrome: panels fade a touch lighter at the top -> Panel0 at the base (soft lit depth).
    private static readonly Color PanelTop = new(0x2b, 0x21, 0x17, 220);
    private static readonly Color PanelBot = new(0x1d, 0x15, 0x0e, 220);

    private static Color StatColor(Stat s)
    {
        foreach (var (stat, color) in StatColors) if (stat == s) return color;
        return Ink;
    }

    private void Sprite(Texture2D? tex, int x, int y, int w, int h, Color tint)
    {
        if (tex is null) { Border(x, y, w, h, Border0); return; } // visible gap, not a crash
        _spriteBatch.Draw(tex, new Rectangle(x, y, w, h), tint);
    }

    private void Stretch(Texture2D? tex, int x, int y, int w, int h) => Sprite(tex, x, y, w, h, Color.White);

    // Fonts are built at FontBake x design px; drawing at 1/FontBake inside the scene's SS matrix keeps
    // the DESIGN size constant while the glyph rasterizes at up to FontBake x density (native-res P0).
    private void Text(SpriteFont font, string s, int x, int y, Color color) =>
        _spriteBatch.DrawString(font, Safe(font, s), new Vector2(x, y), color, 0f, Vector2.Zero,
            1f / FontBake, SpriteEffects.None, 0f);

    // The DESIGN-space px each built font draws at through Text() (its raster size / FontBake):
    // mono 42/3=14, display 60/3=20. A manifest `fontPx` is honoured by scaling relative to this base.
    private const float MonoDesignPx = 14f, DisplayDesignPx = 20f;

    // Manifest border weight in DESIGN px, as authored (w=1 -> a 1px hairline; the scene scale keeps
    // it crisp). The old x2 pin came from the fixed-SS=2 conflation and read ~2x OVERSIZE vs design
    // (Doug's 2026-07-02 pm note) — borders now draw at their authored weight.
    private static int BorderPx(int w) => Math.Max(1, w);

    // Draw text at a manifest-specified `fontPx` (design px): scale the built glyph so its on-screen size
    // is fontPx, keeping the 1/FontBake density factor. Falls back to the plain size when fontPx <= 0.
    private void TextPx(SpriteFont font, string s, int x, int y, Color color, double fontPx)
    {
        if (fontPx <= 0) { Text(font, s, x, y, color); return; }
        var basePx = font == _assets.Display ? DisplayDesignPx : MonoDesignPx;
        var scale = (1f / FontBake) * (float)(fontPx / basePx);
        _spriteBatch.DrawString(font, Safe(font, s), new Vector2(x, y), color, 0f, Vector2.Zero,
            scale, SpriteEffects.None, 0f);
    }

    // Design-space text size: MeasureString is at the FontBake raster, so scale back to design space.
    private Vector2 MeasureText(SpriteFont font, string s) => font.MeasureString(Safe(font, s)) / FontBake;

    // Manifest text inside a rect: greedy word-wrap to the rect width, capped at the lines the rect
    // HEIGHT can hold (a one-line-high rect never wraps, so names/values stay single-line).
    // '\n' forces a break — a resolver can stack a title line over its caption (the gauge panels).
    private void TextPxWrapped(SpriteFont font, string s, Rectangle r, Color color, double fontPx)
    {
        var basePx = font == _assets.Display ? DisplayDesignPx : MonoDesignPx;
        var sc = fontPx <= 0 ? 1f : (float)(fontPx / basePx);
        var lineH = MeasureText(font, "Ay").Y * sc;
        var maxLines = Math.Max(1, (int)Math.Round(r.Height / lineH)); // a near-2-line box (0.9x) still wraps
        if (!s.Contains('\n') && (maxLines == 1 || MeasureText(font, s).X * sc <= r.Width))
        {
            // A single-line box that can't wrap SHRINKS its label to fit instead of spilling over the
            // neighbouring chrome (IM Fell runs wider than the extraction font — "AUTO-ATTACK" vs its
            // 98px chip). The authored box stays the truth; only the glyphs give.
            var w = MeasureText(font, s).X * sc;
            var fit = maxLines == 1 && w > r.Width && fontPx > 0 ? fontPx * r.Width / w : fontPx;
            RecordTextBox(new Rectangle(r.X, r.Y,
                (int)(fit > 0 && fontPx > 0 ? w * (fit / fontPx) : w), (int)lineH), r);
            TextPx(font, s, r.X, r.Y, color, fit);
            return;
        }
        // Wrapped text is line-clamped to the box, so its drawn footprint IS (at most) the bound.
        RecordTextBox(r, r);
        var ly = (float)r.Y;
        var lines = 0;
        foreach (var para in s.Split('\n'))
        {
            var line = "";
            foreach (var word in para.Split(' '))
            {
                var trial = line.Length == 0 ? word : line + " " + word;
                if (line.Length > 0 && MeasureText(font, trial).X * sc > r.Width)
                {
                    TextPx(font, line, r.X, (int)ly, color, fontPx);
                    ly += lineH;
                    if (++lines >= maxLines) return;
                    line = word;
                }
                else line = trial;
            }
            if (line.Length > 0)
            {
                TextPx(font, line, r.X, (int)ly, color, fontPx);
                ly += lineH;
                if (++lines >= maxLines) return;
            }
        }
    }

    // P0-A.5 collision detector: while the smoke's full render runs, every drawn text footprint is
    // recorded with its element context — SmokeAllScreensAndExit diffs footprints against element
    // bounds (overflow) and against sibling footprints (collision).
    private bool _collectText;
    private string? _textOwner; // the element id whose content is being drawn
    private readonly System.Collections.Generic.List<(string El, Rectangle Box, Rectangle Bound)> _textBoxes = new();

    private void RecordTextBox(Rectangle drawn, Rectangle bound)
    {
        if (_collectText && _textOwner is { } el)
            _textBoxes.Add((el, drawn, bound));
    }

    // SpriteFonts are ASCII-only and THROW on an unknown glyph. The fold-to-ASCII policy + algorithm
    // live in Core.GlyphSafe (headless-tested); here we just cache each font's glyph set and call it.
    private readonly System.Collections.Generic.Dictionary<SpriteFont, System.Collections.Generic.HashSet<char>> _fontGlyphs = new();
    private string Safe(SpriteFont font, string s)
    {
        if (!_fontGlyphs.TryGetValue(font, out var set))
            _fontGlyphs[font] = set = new System.Collections.Generic.HashSet<char>(font.Characters);
        return Roguebane.Core.GlyphSafe.Sanitize(s, set);
    }

    private void Panel(int x, int y, int w, int h)
    {
        DrawShadow(x, y, w, h, dx: 2, dy: 3, blur: 3, opacity: 0.40f); // §10 depth under the chrome
        // v3 chrome (P0 render-accuracy floor): legacy panels draw through the SAME manifest
        // style.frames the element renderer uses — tiled edges + painted centres. The old local
        // stretch-blit smeared the tiled art. Size ladder unchanged: the PANEL frame for large
        // panels, the lighter CARD frame for card-sized ones, the clean gradient for small boxes
        // (whose corner ornament would be crushed by either frame).
        if (_ui.Manifest?.Style.Frames is { } frames)
        {
            if (w >= 220 && h >= 170 && frames.TryGetValue("panel", out var pf)
                && _assets.Texture(pf.Asset) is { } pt)
            { DrawFrameTex(pt, pf, new Rectangle(x, y, w, h)); return; }
            if (w >= 100 && h >= 80 && frames.TryGetValue("card", out var cf)
                && _assets.Texture(cf.Asset) is { } ct)
            { DrawFrameTex(ct, cf, new Rectangle(x, y, w, h)); return; }
        }
        DrawGradient(x, y, w, h, PanelTop, PanelBot, GradientDir.Vertical);
        Border(x, y, w, h, Border0);
    }

    // A skinned button whose state is driven by input (manifest: drives-from input/interaction).
    // Stretch-scaled for now; true 9-slice is polish. Returns nothing — input lives in Update.
    // CD #11: the 320x88 button skins are 1080-class (2x design), sliced at 12 SOURCE px — corners
    // land at 12 TARGET px (= 6 design px, dstCornerScale 1/SS) so rivets stay native, never chunky.
    private static readonly int[] ButtonSlice = { 12, 12, 12, 12 };

    private void DrawButton(string label, int x, int y, int w, int h, bool enabled, Keys key)
    {
        var hovered = enabled && Hover(new Rectangle(x, y, w, h));
        var state = !enabled ? "disabled" : _keys.IsKeyDown(key) || hovered ? "down" : "normal";
        var skin = _assets.Button(state);
        if (skin is not null)
            foreach (var p in NineSlice.Patches(skin.Width, skin.Height, ButtonSlice,
                         new LayoutRect(x, y, w, h), tile: false, centerFill: true, dstCornerScale: 1.0 / ChromeBake))
                _spriteBatch.Draw(skin, new Rectangle(p.Dst.X, p.Dst.Y, p.Dst.W, p.Dst.H),
                    new Rectangle(p.Src.X, p.Src.Y, p.Src.W, p.Src.H), Color.White);
        else Panel(x, y, w, h);
        var size = MeasureText(_assets.Mono, label);
        Text(_assets.Mono, label, (int)(x + w / 2 - size.X / 2), (int)(y + h / 2 - size.Y / 2),
            enabled ? Ink : Muted);
        if (hovered) Border(x, y, w, h, Amber);
    }

    // A labelled value bar (HP, etc.): icon, filled track, and the mono "cur/max" readout.
    private void DrawBar(int x, int y, int w, Texture2D? icon, int cur, int max, Color fill)
    {
        Sprite(icon, x, y, 20, 20, Color.White);
        var bx = x + 24;
        var bw = w - 24;
        Rect(bx, y + 2, bw, 16, new Color(40, 30, 24));
        if (max > 0) Rect(bx, y + 2, bw * Math.Max(0, cur) / max, 16, fill);
        Text(_assets.Mono, $"{cur}/{max}", bx + 6, y + 2, Ink);
    }

    private void Rect(int x, int y, int w, int h, Color color)
    {
        if (w <= 0 || h <= 0) return;
        _spriteBatch.Draw(_pixel, new Rectangle(x, y, w, h), color);
    }

    private void Border(int x, int y, int w, int h, Color color)
        => Border(x, y, w, h, color, 2, null);

    // §10 border.sides: a manifest border may name a subset of edges (e.g. ["top"] for an accent rule)
    // — null/empty draws all four. Thickness is in design px (see BorderPx).
    private void Border(int x, int y, int w, int h, Color color, int t, string[]? sides)
    {
        bool On(string s) => sides is null || sides.Length == 0 || Array.IndexOf(sides, s) >= 0;
        if (On("top")) Rect(x, y, w, t, color);
        if (On("bottom")) Rect(x, y + h - t, w, t, color);
        if (On("left")) Rect(x, y, t, h, color);
        if (On("right")) Rect(x + w - t, y, t, h, color);
    }

    // A straight line between two points (a stretched, rotated 1px texture) for the chart's links.
    // dashed => draw only alternating segments, so an uncharted jump reads dotted.
    private void Line(int x1, int y1, int x2, int y2, int thickness, Color color, bool dashed = false)
    {
        var a = new Vector2(x1, y1);
        var b = new Vector2(x2, y2);
        var delta = b - a;
        var len = delta.Length();
        if (len < 1f) return;
        var angle = (float)Math.Atan2(delta.Y, delta.X);
        if (!dashed)
        {
            _spriteBatch.Draw(_pixel, a, null, color, angle,
                new Vector2(0, 0.5f), new Vector2(len, thickness), SpriteEffects.None, 0f);
            return;
        }
        var dir = delta / len;
        const float seg = 6f;
        for (var d = 0f; d < len; d += seg * 2)
        {
            var p = a + dir * d;
            var segLen = Math.Min(seg, len - d);
            _spriteBatch.Draw(_pixel, p, null, color, angle,
                new Vector2(0, 0.5f), new Vector2(segLen, thickness), SpriteEffects.None, 0f);
        }
    }
}
