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

    // Fonts are built at SSx; under the scene's SS scale matrix we draw at 1/SS so the ON-SCREEN size
    // matches the design space while the glyph rasterizes at full 1080-class density.
    private void Text(SpriteFont font, string s, int x, int y, Color color) =>
        _spriteBatch.DrawString(font, Safe(font, s), new Vector2(x, y), color, 0f, Vector2.Zero,
            1f / SS, SpriteEffects.None, 0f);

    // The DESIGN-space px each built font draws at through Text() (its SSx size / SS): mono 28/2=14,
    // display 40/2=20. A manifest `fontPx` is honoured by scaling relative to this base.
    private const float MonoDesignPx = 14f, DisplayDesignPx = 20f;

    // Draw text at a manifest-specified `fontPx` (design px): scale the built glyph so its on-screen size
    // is fontPx, keeping the 1/SS supersample factor. Falls back to the plain size when fontPx <= 0.
    private void TextPx(SpriteFont font, string s, int x, int y, Color color, double fontPx)
    {
        if (fontPx <= 0) { Text(font, s, x, y, color); return; }
        var basePx = font == _assets.Display ? DisplayDesignPx : MonoDesignPx;
        var scale = (1f / SS) * (float)(fontPx / basePx);
        _spriteBatch.DrawString(font, Safe(font, s), new Vector2(x, y), color, 0f, Vector2.Zero,
            scale, SpriteEffects.None, 0f);
    }

    // Design-space text size: MeasureString is at the SSx raster, so scale back by 1/SS to match Text().
    private Vector2 MeasureText(SpriteFont font, string s) => font.MeasureString(Safe(font, s)) / SS;

    // Manifest text inside a rect: greedy word-wrap to the rect width, capped at the lines the rect
    // HEIGHT can hold (a one-line-high rect never wraps, so names/values stay single-line).
    private void TextPxWrapped(SpriteFont font, string s, Rectangle r, Color color, double fontPx)
    {
        var basePx = font == _assets.Display ? DisplayDesignPx : MonoDesignPx;
        var sc = fontPx <= 0 ? 1f : (float)(fontPx / basePx);
        var lineH = MeasureText(font, "Ay").Y * sc;
        var maxLines = Math.Max(1, (int)Math.Round(r.Height / lineH)); // a near-2-line box (0.9x) still wraps
        if (maxLines == 1 || MeasureText(font, s).X * sc <= r.Width)
        {
            TextPx(font, s, r.X, r.Y, color, fontPx);
            return;
        }
        var line = "";
        var ly = (float)r.Y;
        var lines = 0;
        foreach (var word in s.Split(' '))
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
        if (line.Length > 0) TextPx(font, line, r.X, (int)ly, color, fontPx);
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

    private static readonly int[] PanelSlice = { 60, 60, 60, 60 }; // style.frames.panel (240px asset)
    private static readonly int[] CardSlice = { 36, 36, 36, 36 };  // style.frames.card (144px asset)

    private void Panel(int x, int y, int w, int h)
    {
        DrawShadow(x, y, w, h, dx: 2, dy: 3, blur: 3, opacity: 0.40f); // §10 depth under the chrome
        // Carved nine-slice frames by size: the PANEL frame (60px corners) for large panels, the lighter
        // CARD frame (36px corners) for card-sized elements, and the clean gradient for small cards/bars
        // (whose corner ornament would be crushed by either frame).
        if (w >= 220 && h >= 170 && DrawFrame(x, y, w, h, "panel", PanelSlice)) return;
        if (w >= 100 && h >= 80 && DrawFrame(x, y, w, h, "card", CardSlice)) return;
        DrawGradient(x, y, w, h, PanelTop, PanelBot, GradientDir.Vertical);
        Border(x, y, w, h, Border0);
    }

    // §10 nine-slice frame: blit a painted frame asset around the rect -- fixed corners, stretched edges
    // + centre (geometry from Core.NineSlice). False if the asset isn't loaded (caller keeps its fallback).
    private bool DrawFrame(int x, int y, int w, int h, string name, int[] slice)
    {
        var tex = _assets.Frame(name);
        if (tex is null) return false;
        var dst = new Roguebane.Core.Layout.LayoutRect(x, y, w, h);
        foreach (var p in Roguebane.Core.Layout.NineSlice.Patches(tex.Width, tex.Height, slice, dst))
            _spriteBatch.Draw(tex, new Rectangle(p.Dst.X, p.Dst.Y, p.Dst.W, p.Dst.H),
                new Rectangle(p.Src.X, p.Src.Y, p.Src.W, p.Src.H), Color.White);
        return true;
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
                         new LayoutRect(x, y, w, h), tile: false, centerFill: true, dstCornerScale: 1.0 / SS))
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
    {
        Rect(x, y, w, 2, color);
        Rect(x, y + h - 2, w, 2, color);
        Rect(x, y, 2, h, color);
        Rect(x + w - 2, y, 2, h, color);
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
