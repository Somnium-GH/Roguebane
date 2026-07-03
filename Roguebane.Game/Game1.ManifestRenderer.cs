using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Roguebane.Core;
using Roguebane.Core.Content;
using Roguebane.Core.Layout;

namespace Roguebane.Game;

// The manifest-driven screen RENDERER half of the shell (SRP split, 2026-07-02): everything that
// draws a screen straight from layout.json — the generic element renderer (§10 order), list/graph
// stamping, the live bind resolvers, and the fidelity primitives (fill/frame/shadow/wrap). Input and
// the game loop stay in Game1.cs; the legacy CityMap draw is its own split candidate.
public partial class Game1
{
    // ===== Generic manifest-driven screen renderer (RESCUE arc) =====
    // Draw a screen's elements straight from layout.json, in Z order. Static types (panel/text/icon/
    // button) render fully here; bound types (list/figure/graph/bar) draw only their chrome for now and
    // get live content in later slices. Each element: shadow -> frame OR fill -> border -> content (§10).
    // `skip` (smoke only): leave ONE element out, so the coverage validator can measure that
    // element's actual pixel contribution against the full render.
    private void DrawManifestScreen(string screenId, Element? skip = null)
    {
        var s = _ui.ScreenDef(screenId);
        if (s is null) return;
        // 07-03 drop: manifest z is ONE convention — a PAINT ORDINAL, back->front. Draw ascending;
        // the scene backdrop is found by its *.scene bind, never by z==0 (LAYOUT_CONTRACT §12).
        foreach (var e in s.Elements.OrderBy(x => x.Z))
        {
            if (ReferenceEquals(e, skip)) continue;
            _textOwner = e.Id; // collision detector context (recorded only while _collectText)
            _curScreen = s;    // panel headers confine to the band above their contained children
            DrawManifestElement(e, _ui.Rect(s, e));
        }
        _textOwner = null;
    }

    // The scene layer alone — the baseline the smoke paint-coverage diff measures against. A scene
    // element is identified by its *.scene bind (paint-ordinal z carries no layer semantics).
    private static bool IsSceneElement(Element e) => e.Binds is { } b && b.EndsWith(".scene");

    private void DrawManifestBackdrop(string screenId)
    {
        var s = _ui.ScreenDef(screenId);
        if (s is null) return;
        foreach (var e in s.Elements.Where(IsSceneElement))
            DrawManifestElement(e, _ui.Rect(s, e));
    }

    private Roguebane.Core.Layout.Screen? _curScreen; // per draw pass; panel headers see siblings

    private void DrawManifestElement(Element e, Rectangle r)
    {
        // data-bind-gate (LAYOUT_CONTRACT §12): content+binds coexisting means the content is the
        // literal and the bind GATES the whole element — a closed gate draws nothing, chrome included
        // (never an empty box). Buttons keep their own enabled/skin machinery below. Bound ICONS gate
        // the same way (foeReticle's authored image is a mock-position stand-in; live mounts draw it).
        if (e.Type == "text" && e.Content is { Length: > 0 } && e.Binds is { Length: > 0 }
            && ResolveScreenBind(e.Binds) is null)
            return;
        if (e.Type == "icon" && e.Binds is { Length: > 0 } && ResolveScreenBind(e.Binds) is null)
            return;
        // A TEXT element's shadow is a text shadow (offset glyph copy, drawn with the text below) —
        // rect-shadowing it would paint a solid box behind the words.
        if (e.Shadow is { } sh && e.Type != "text")
            DrawShadow(r.X, r.Y, r.Width, r.Height, sh.Dx, sh.Dy, sh.Blur, (float)sh.Opacity);
        // §10: fill is the element's BACKGROUND, the nine-slice frame is chrome layered on it — an
        // element may carry both (topBar), so draw fill first, then frame. EXCEPT the war-party
        // covered-ground fill: its width IS the datum (drawn in the text case below), so the full-rect
        // draw would always read "overrun".
        if (e.Fill is { } fill && e.Binds is not ("enemy.advancePct" or "runes.budgetPct"))
            DrawFill(r, fill);
        if (e.Frame is { } fr && fr.Slice.Length == 4 && _assets.Texture(fr.Asset) is { } ftex)
            DrawFrameTex(ftex, fr, r);
        if (e.Border is { } b)
        {
            // An element-level colorBind tints the border (the accent rule) when it resolves.
            var accent = ResolveColorToken(e.ColorBind, null);
            Border(r.X, r.Y, r.Width, r.Height, _ui.Color(accent ?? b.Color ?? "border", Border0),
                BorderPx(b.W), b.Sides);
        }

        switch (e.Type)
        {
            case "panel" when ResolveScreenBind(e.Binds) is { } ptxt:
            {
                // A bound panel whose bind resolves a display string is a titled gauge (SHIELD n/m,
                // SUPPLIES n/m...): the header draws inset over the chrome, CONFINED to the band
                // above any child elements the panel contains (the SHIELD label was drawing across
                // its own pips), height-fitting its font to that band. Container binds resolve null
                // and stay chrome-only.
                var bandBottom = r.Bottom - 5;
                if (_curScreen is { } scr)
                    foreach (var sib in scr.Elements)
                        if (!ReferenceEquals(sib, e) && _ui.Rect(scr, sib) is var sr
                            && sr != r && r.Contains(sr))
                            bandBottom = Math.Min(bandBottom, sr.Y);
                var inset = new Rectangle(r.X + 6, r.Y + 5, r.Width - 12,
                    Math.Max(5, bandBottom - (r.Y + 5)));
                var hFont = e.Font == "display" ? _assets.Display : _assets.Mono;
                var hPx = e.FontPx ?? 0;
                if (hPx > 0)
                {
                    var basePx = hFont == _assets.Display ? DisplayDesignPx : MonoDesignPx;
                    var lineH = MeasureText(hFont, "Ay").Y * (float)(hPx / basePx);
                    var lines = ptxt.Count(c => c == '\n') + 1;
                    if (lineH * lines > inset.Height) hPx *= inset.Height / (lineH * lines);
                }
                TextPxWrapped(hFont, ptxt, inset, _ui.Color(e.Color ?? "ink", Ink), hPx);
                break;
            }
            case "text":
                // A *.scene backdrop: the element's authored image IS the content (combat field,
                // merchant stall). §13 aspect-fill: it SCALE-TO-COVERS the whole (extended) design
                // space — source-cropped to the viewport aspect so nothing stretches, no bars.
                if (e.Binds is { } sceneBind && sceneBind.EndsWith(".scene") && e.Image is { Length: > 0 } bg)
                {
                    if (_assets.Texture(NormalizeContentPath(bg)) is { } bgTex)
                    {
                        var dw = _ui.DesignW > 0 ? _ui.DesignW : W;
                        var dh = _ui.DesignH > 0 ? _ui.DesignH : H;
                        var cover = Math.Min((float)bgTex.Width / dw, (float)bgTex.Height / dh);
                        var srcW = Math.Max(1, (int)(dw * cover));
                        var srcH = Math.Max(1, (int)(dh * cover));
                        _spriteBatch.Draw(bgTex, new Rectangle(0, 0, dw, dh),
                            new Rectangle((bgTex.Width - srcW) / 2, (bgTex.Height - srcH) / 2, srcW, srcH),
                            Color.White);
                    }
                    break;
                }
                // Rune-budget bar (design/02): the fill width is the SPENT fraction of the budget.
                if (e.Binds == "runes.budgetPct")
                {
                    if (e.Fill is { } bf && _build.Runes.Budget > 0)
                    {
                        var spent = _build.Runes.Budget - _build.Runes.Available;
                        var bw = (int)(r.Width * (float)spent / _build.Runes.Budget);
                        if (bw > 0) DrawFill(new Rectangle(r.X, r.Y, bw, r.Height), bf);
                    }
                    break;
                }
                // War-party covered ground (design/03 rev 2): the fill loads RIGHT->LEFT in tandem
                // with the host — its leading (left) edge tracks distance-to-camp. The bar IS the datum.
                if (e.Binds == "enemy.advancePct")
                {
                    if (InRun && e.Fill is { } df && Exp.Map.MarchLength > 0)
                    {
                        var frac = (float)Exp.Map.WarPartyDistance / Exp.Map.MarchLength;
                        var covered = (int)((1f - frac) * r.Width);
                        if (covered > 0)
                            DrawFill(new Rectangle(r.X + r.Width - covered, r.Y, covered, r.Height), df);
                    }
                    break;
                }
                // §6b regen readout: the element's fill/border drew the track above; the live progress
                // toward the NEXT pip fills it in the pips' live token. No text — the bar IS the datum.
                if (e.Binds == "ShieldPool.regen")
                {
                    if (InRun && Exp.Player.Body.ShieldRegenProgress is > 0f and var prog)
                        DrawFill(new Rectangle(r.X, r.Y, (int)(r.Width * prog), r.Height),
                            new Fill { Token = "mintActive" });
                    break;
                }
                var txt = e.Content ?? ResolveScreenBind(e.Binds);
                // A bindless text element carrying a static image (doomHost's enemy-host icon) IS
                // that image — blit it. State-skinned elements (buttons) keep their skin machinery.
                if (string.IsNullOrEmpty(txt) && e.Image is { Length: > 0 } img
                    && e.States.ValueKind is System.Text.Json.JsonValueKind.Undefined
                        or System.Text.Json.JsonValueKind.Null)
                {
                    Sprite(_assets.Texture(NormalizeContentPath(img)), r.X, r.Y, r.Width, r.Height, Color.White);
                    break;
                }
                DrawStateSkin(e, r, enabled: !string.IsNullOrEmpty(txt));
                if (!string.IsNullOrEmpty(txt))
                {
                    var font = e.Font == "display" ? _assets.Display : _assets.Mono;
                    var px = e.FontPx ?? 0;
                    if (e.Shadow is { } tsh)
                        TextPxWrapped(font, txt!, new Rectangle(r.X + tsh.Dx, r.Y + tsh.Dy, r.Width, r.Height),
                            _ui.Color(tsh.Color ?? "outline", Color.Black) * (float)tsh.Opacity, px);
                    TextPxWrapped(font, txt!, r, _ui.Color(e.Color ?? "ink", Ink), px);
                }
                break;
            case "icon" when !string.IsNullOrEmpty(e.Image):
                // Authored paths may carry the project prefix + extension ("Content/icons/x.png")
                // — normalize like every other image consumer, else the lookup misses and the
                // null-texture box draws (the merchant factorToken was this).
                Sprite(_assets.Texture(NormalizeContentPath(e.Image!)),
                    r.X, r.Y, r.Width, r.Height, Color.White);
                break;
            case "button":
                // A manifest-STYLED button (beginBtn: states.idle fill/color) draws its authored
                // chrome — fill + centered label at the authored fontPx (design/05's green CTA).
                // The legacy stone skin remains only for style-less buttons.
                if (e.States.ValueKind == System.Text.Json.JsonValueKind.Object
                    && e.States.TryGetProperty("idle", out var idle)
                    && idle.ValueKind == System.Text.Json.JsonValueKind.Object)
                {
                    var fillTok = idle.TryGetProperty("fill", out var bf2) ? bf2.GetString() : e.Fill?.Token;
                    var colTok = idle.TryGetProperty("color", out var bc2) ? bc2.GetString() : e.Color;
                    if (Hover(r) && e.States.TryGetProperty("hover", out var hov)
                        && hov.ValueKind == System.Text.Json.JsonValueKind.Object)
                    {
                        if (hov.TryGetProperty("fill", out var hf)) fillTok = hf.GetString();
                        if (hov.TryGetProperty("color", out var hc)) colTok = hc.GetString();
                    }
                    if (fillTok is { Length: > 0 }) DrawFill(r, new Fill { Token = fillTok });
                    if (e.Border is { } bb)
                        Border(r.X, r.Y, r.Width, r.Height, _ui.Color(bb.Color, Border0),
                            BorderPx(bb.W), bb.Sides);
                    var bfont = e.Font == "display" ? _assets.Display : _assets.Mono;
                    var blabel = e.Content ?? "";
                    var bpx = e.FontPx ?? 0;
                    var bsz = MeasureText(bfont, blabel)
                        * (bpx > 0 ? (float)(bpx / (e.Font == "display" ? DisplayDesignPx : MonoDesignPx)) : 1f);
                    TextPx(bfont, blabel, (int)(r.X + r.Width / 2 - bsz.X / 2),
                        (int)(r.Y + r.Height / 2 - bsz.Y / 2), _ui.Color(colTok ?? "ink", Ink), bpx);
                    break;
                }
                DrawButton(e.Content ?? "", r.X, r.Y, r.Width, r.Height, true, Keys.None);
                break;
            case "list" when e.Item is not null:
                DrawManifestList(e, r);
                break;
            case "figure" when e.Binds is "preview.fig" or "Body":
                // Composed figure: feet at the box bottom-centre, scaled to the box height. In a run
                // the LIVE body draws (part conditions, worn gear); pre-run the build preview does.
                if (InRun)
                    DrawHumanoid(Exp.Player.Body, Exp.FigureId, r.X + r.Width / 2, r.Y + r.Height, r.Height);
                else
                    DrawHumanoid(_build.Preview(), _build.CoreRune.FigureKey(_build.Race),
                        r.X + r.Width / 2, r.Y + r.Height, r.Height);
                break;
            case "figure" when e.Binds == "encounter.foe" && InRun && Exp.Enemy is { } ef:
                if (ef.Frame is { } frame)
                    DrawHumanoid(frame, ef.Figure, r.X + r.Width / 2, r.Y + r.Height, r.Height,
                        ef.Down ? new Color(70, 60, 55) : Color.White, allowBare: false);
                // Part-aim affordances (2026-07-02 directive: reticles sit ON the foe's body parts).
                // While a module is picking, the AIMING reticle centres on the hovered limb's ACTUAL
                // part rects (band strips remain the click hit-test); a locked part-aim shows the
                // FOCUS reticle on its part.
                // §8 targeting presentation (design/08, LOCKED): NO box affordances, ever — no hover
                // border, no band highlight, no whole-foe frame. The cursor IS the reticle.
                var picking = !ef.Down && _ctrl.IsTargeting(Exp);
                if (picking)
                {
                    // The red AIMING reticle rides the cursor, snapping/centring to the hovered limb
                    // band (band strips stay the click hit-test; they just aren't drawn).
                    var mount = ef.Frame is not null && FoePartAt(ef, _cursor) is { } hov
                        && FoePartScreenRect(ef, hov.Stat, r) is { } pr
                        ? new Point(pr.X + pr.Width / 2, pr.Y + pr.Height / 2)
                        : _cursor;
                    if (_assets.Reticle("aiming") is { } aim)
                        Sprite(aim, mount.X - 24, mount.Y - 24, 48, 48, Color.White);
                }
                // Locked part-aims read as reticle mounts (deduped by stat): the pulsing FOCUS frames
                // normally; the faint SECONDARY while ANOTHER module is actively picking (design/08).
                // Size = the part rect's larger side x1.5, clamped 64..136 SCREEN(scene) px.
                if (!ef.Down && ef.Frame is not null)
                {
                    var frames = _ui.ScreenDef("encounter")?.Elements
                        .FirstOrDefault(x => x.Binds == "targeting.focus")?.Frames;
                    var focus = frames is { Length: > 0 }
                        ? _assets.Texture(frames[_animTick / 10 % frames.Length])
                        : _assets.Reticle("focus");
                    var tex = picking ? _assets.Reticle("secondary") ?? focus : focus;
                    // Group aims per part: one reticle per part, its AIM TAG stack = the hotkey
                    // NUMBERS of every module kept on it (design/01: several actives stack).
                    var aims = new System.Collections.Generic.Dictionary<Stat, List<int>>();
                    for (var ti = 0; ti < Exp.Equipment.Count; ti++)
                        if (Exp.PartOf(Exp.Equipment[ti]) is { } aimed)
                            (aims.TryGetValue(aimed.Stat, out var l)
                                ? l : aims[aimed.Stat] = new List<int>()).Add(ti);
                    var tagT = _ui.Manifest?.Templates.GetValueOrDefault("aimTag");
                    foreach (var (stat, cardIxs) in aims)
                    {
                        if (FoePartScreenRect(ef, stat, r) is not { } fr2 || tex is null) continue;
                        var scenePx = Math.Clamp(Math.Max(fr2.Width, fr2.Height) * SS * 1.5f, 64f, 136f);
                        var d = (int)(scenePx / SS);
                        var top = fr2.Y + fr2.Height / 2 - d / 2;
                        Sprite(tex, fr2.X + fr2.Width / 2 - d / 2, top, d, d,
                            picking ? Color.White * 0.55f : Color.White);
                        if (tagT is null) continue;
                        // Tag row ABOVE the reticle, centred: templates.aimTag per kept module.
                        var (tw, thh, gap) = (tagT.Size[0], tagT.Size[1], 2);
                        var rowW = cardIxs.Count * tw + (cardIxs.Count - 1) * gap;
                        var tx = fr2.X + fr2.Width / 2 - rowW / 2;
                        var ty = top - thh - 2;
                        foreach (var ix in cardIxs)
                        {
                            DrawFill(new Rectangle(tx, ty, tw, thh), tagT.Fill ?? new Fill { Token = "hit" });
                            if (tagT.Border is { } tb)
                                Border(tx, ty, tw, thh, _ui.Color(tb.Color, Border0), BorderPx(tb.W), tb.Sides);
                            foreach (var tp in CardTemplate.Place(tagT, tx, ty))
                                TextPxWrapped(tp.Font == "display" ? _assets.Display : _assets.Mono,
                                    (ix + 1).ToString(), RectOf(tp.Rect),
                                    _ui.Color(tp.Color ?? "outline", Ink), tp.FontPx);
                            tx += tw + gap;
                        }
                    }
                }
                break;
            case "graph" when e.Binds == "map" && InRun && e.Item is not null:
                DrawManifestGraph(e, r);
                break;
            case "graph" when e.Binds == "campaign" && InRun && e.Item is not null:
                DrawCampaignGraph(e, r);
                break;
            case "figure" when e.Binds == "encounter.minions" && InRun:
                // The fielded retinue on the battlefield: each minion's sprite, feet on the box floor.
                for (var mi = 0; mi < Exp.Minions.Count; mi++)
                {
                    var tex = _assets.Minion(Exp.Minions[mi].Id);
                    var mw = r.Height * 2 / 3;
                    var mx = r.X + mi * (mw + 8);
                    if (tex is not null) Sprite(tex, mx, r.Y, mw, r.Height, Color.White);
                    else Rect(mx + 4, r.Y + 8, mw - 8, r.Height - 16, new Color(Amber, 70));
                }
                break;
        }
    }

    // The city chart (design/03): map nodes spread over the graph element's region via GraphLayout —
    // links first (dashed when uncharted), then a beacon per node (fog-aware icon), the current node
    // ringed and reachable deployments numbered. Same live rules as the legacy chart, manifest geometry.
    private void DrawManifestGraph(Element e, Rectangle region)
    {
        var map = Exp.Map;
        Template? tmplNode = null;
        _ui.Manifest?.Templates.TryGetValue(e.Item!.Template, out tmplNode);
        var nodes = map.Nodes;
        var cols = nodes.Max(x => x.Col) + 1;
        var rows = nodes.Max(x => x.Row) + 1;
        var cw = e.Item!.Size.Length == 2 ? e.Item.Size[0] : 28;
        var ch = e.Item.Size.Length == 2 ? e.Item.Size[1] : 28;
        Rectangle Cell(MapNode n) => RectOf(GraphLayout.Cell(
            new LayoutRect(region.X, region.Y, region.Width, region.Height), cols, rows, n.Col, n.Row, cw, ch));

        foreach (var node in nodes)
        {
            var from = Cell(node);
            foreach (var nid in node.Next)
            {
                var to = Cell(map.Node(nid));
                var charted = node.Visited; // a link out of a charted beacon is itself charted
                Line(from.X + cw / 2, from.Y + ch / 2, to.X + cw / 2, to.Y + ch / 2, 2,
                    charted ? new Color(150, 130, 95) : new Color(90, 78, 66), dashed: !charted);
            }
        }

        var options = map.Options;
        foreach (var node in nodes)
        {
            var r = Cell(node);
            var seen = map.Sees(node);
            var isCurrent = ReferenceEquals(node, map.Current);
            // The icon comes from the template's imageBind path ("icons/node/{node.type}"), resolved
            // with the FOG-AWARE type so an unrevealed beacon blits the unknown token.
            var tex = _assets.Node(seen);
            var iconBind = tmplNode?.Parts.FirstOrDefault(pt => pt.ImageBind is not null)?.ImageBind;
            if (iconBind is not null)
                tex = _assets.Texture(iconBind.Replace("{node.type}", AssetRegistry.NodeToken(seen))) ?? tex;
            Sprite(tex, r.X, r.Y, cw, ch, isCurrent ? Color.White : new Color(210, 200, 190));

            var oi = IndexOf(options, node);
            if (isCurrent)
            {
                // The ring alone marks the position — the youAreHere ELEMENT carries the label now
                // (the graph's own hand-drawn copy doubled it; deleted 2026-07-03).
                Border(r.X - 2, r.Y - 2, cw + 4, ch + 4, Amber);
            }
            else if (oi >= 0) // a reachable onward deployment
            {
                Border(r.X - 2, r.Y - 2, cw + 4, ch + 4, Hover(r) ? Ink : new Color(150, 130, 95));
                // Node labels at the design's caption size (dc.html ~11 CSS px = 5.5 design px) —
                // base-size Text() was the "label oversize" from the 07-03 walk.
                TextPx(_assets.Mono, $"[{oi + 1}] {seen.ToString().ToLower()}",
                    r.X - 6, r.Y + ch + 2, Ink, 5.5);
            }
        }
    }

    // The campaign chart (design/04): one city marker per campaign leg, spread across the graph
    // region — taken legs link solid (good), the current leg is framed, onward legs run dotted.
    // City NAMES are OPEN content (§12/§17: count + procgen-vs-authored undecided) so only the tier/
    // status label draws; the design's castle icons aren't in the manifest template (Needs-CD).
    private void DrawCampaignGraph(Element e, Rectangle region)
    {
        var cw = e.Item!.Size.Length == 2 ? e.Item.Size[0] : 8;
        var ch = e.Item.Size.Length == 2 ? e.Item.Size[1] : 8;
        var count = _campaign.LegCount;
        var m = _ui.Manifest;
        Template? tmpl = null;
        if (m is not null) m.Templates.TryGetValue(e.Item.Template, out tmpl);
        Rectangle Cell(int i) => RectOf(GraphLayout.Cell(
            new LayoutRect(region.X, region.Y, region.Width, region.Height), count, 1, i, 0, cw, ch));

        for (var i = 0; i < count - 1; i++)
        {
            var a = Cell(i); var b = Cell(i + 1);
            var taken = i < _campaign.LegIndex;
            Line(a.X + cw / 2, a.Y + ch / 2, b.X + cw / 2, b.Y + ch / 2, 2,
                taken ? _ui.Color("good", Amber) : i == _campaign.LegIndex
                    ? Amber : new Color(90, 78, 66), dashed: !taken && i != _campaign.LegIndex);
        }
        for (var i = 0; i < count; i++)
        {
            var c = Cell(i);
            var taken = i < _campaign.LegIndex;
            var current = i == _campaign.LegIndex;
            Rect(c.X, c.Y, c.Width, c.Height,
                taken ? _ui.Color("good", Amber) : current ? Amber : new Color(90, 78, 66));
            if (current) Border(c.X - 4, c.Y - 4, c.Width + 8, c.Height + 8, Amber);
            if (tmpl is null) continue;
            foreach (var pp in CardTemplate.Place(tmpl, c.X, c.Y))
            {
                // Only the tier/status line has data; city names are OPEN content — draw nothing there.
                if (pp.Binds != "city.tier") continue;
                var label = "Tier " + (i + 1) + (taken ? " - TAKEN" : current ? " - CURRENT" : "");
                TextPxWrapped(pp.Font == "display" ? _assets.Display : _assets.Mono,
                    label, RectOf(pp.Rect), _ui.Color(pp.Color ?? "ink", Ink), pp.FontPx);
            }
        }
    }

    // Screen-level (non-list) binds -> display text: the NewGame Loadout preview reads the BuildSession.
    private string? ResolveScreenBind(string? bind) => bind switch
    {
        "preview.name" => _build.Race.Name + " " + _build.CoreRune.Title,
        "preview.role" => _build.CoreRune.Archetype,
        "preview.hp" => _build.Race.Hp.ToString(),
        "preview.budget" => _build.CoreRune.RuneBudget.ToString(),
        "preview.techniques" => _build.CoreRune.Kit.Count.ToString(),
        "preview.bays" => _build.CoreRune.Bays.ToString(),
        "preview.coreEffectName" => _build.CoreRune.CoreEffectName,
        // *.coreEffect is the BLOCK container (border chrome; label/name/desc are their own
        // elements/parts) — resolving it to the desc painted the copy TWICE (the doubled-text P0).
        "preview.coreEffectDesc" => _build.CoreRune.CoreEffectDesc,
        // "core" = the equipment identity BLOCK (chrome only) — currentCoreName/Role carry the text;
        // resolving it printed the identity a SECOND time as a panel header (the doubled-name bug).
        // Equipment identity block (design/02): the core is fixed for the run, so the build's core
        // is the live source pre-run AND mid-run. core.coreEffect (the block) stays chrome-only.
        "core.name" => _build.Race.Name + " " + _build.CoreRune.Title,
        "core.role" => _build.CoreRune.Archetype,
        // Equipment strip labels (design/02). run.state reads the real expedition state in-run —
        // pre-run the march is armed, matching the design's READY TO MARCH copy.
        "run.state" => InRun ? Exp.State.ToString().ToUpperInvariant() : "READY TO MARCH",
        "loadout.slotLabel" => "TECHNIQUES - "
            + (InRun ? Exp.Equipment.Count : _build.Equipment.Count) + " / " + _build.CoreRune.Kit.Count + " slotted",
        "minions.slotLabel" => "MINIONS - "
            + (InRun ? Exp.Minions.Count : _build.CoreRune.MinionKit.Count) + " / " + _build.CoreRune.Bays + " slotted",
        "core.coreEffectName" => _build.CoreRune.CoreEffectName,
        "core.coreEffectDesc" => _build.CoreRune.CoreEffectDesc,
        "runes.budget" => _build.Runes.Available + " free / " + _build.Runes.Budget,
        "Body.hp" => InRun ? Exp.Player.Hp + " / " + Exp.Player.MaxHp : null,
        "encounter.foe.hp" => InRun && Exp.Enemy is { } foe ? foe.Hp + " / " + foe.MaxHp : null,
        // HP strip eyebrows (design/01: "GRUNT · HP" / "DIRE OGRE · HP 14 / 20"). ASCII dash — the
        // bundled font regions don't carry U+00B7 and GlyphSafe would degrade it to "?".
        "Body.hpLabel" => InRun
            ? (_build.Race.Name + " " + _build.CoreRune.Title).ToUpperInvariant() + " - HP" : null,
        "encounter.foe.hpLabel" => InRun && Exp.Enemy is { } f2
            ? f2.Id.Replace('_', ' ').ToUpperInvariant() + " - HP " + f2.Hp + " / " + f2.MaxHp : null,
        // Combat verbs (design/01 chips; labels were flattened by extraction -> authored here).
        "combat.autoAttack" => InRun ? "AUTO-ATTACK" : null, // the ON state reads from the amber skin
        "combat.retreat" => InRun ? "RETREAT" : null,
        // §6b shield bar header: standing points / total layers across the body's shield sources.
        "ShieldPool" => InRun && Exp.Player.Body.ShieldLayers > 0
            ? "SHIELD " + Exp.Player.Body.ShieldPoints + "/" + Exp.Player.Body.ShieldLayers : null,
        // Merchant screen (design/07): header/footer readouts. The pager label waits on the wares slice.
        "merchant.label" => "MERCHANT",
        "merchant.leave" => InRun && Exp.AtMerchant ? "LEAVE" : null,
        "run.gold" => InRun ? "PURSE " + Exp.Gold + "g" : null,
        "merchant.stock.pageLabel" => InRun && Exp.AtMerchant
            ? "PAGE " + (_merchantPage + 1) + " / " + MerchantPageCount() : null,
        "merchant.stock.pagePrev" => InRun && Exp.AtMerchant && _merchantPage > 0 ? "<" : null,
        "merchant.stock.pageNext" => InRun && Exp.AtMerchant
            && _merchantPage < MerchantPageCount() - 1 ? ">" : null,
        "combat.paused" => _paused ? "HELD" : null, // badge shows only while the fight is held
        // Navigation gates (07-03 drop): the bound datum is "this affordance applies here" — the
        // literal labels live in the manifest content (bind-gate semantics, LAYOUT_CONTRACT §12).
        "nav.close" => "CLOSE",           // Equipment's close is always available on that screen
        "nav.equipment" => "EQUIPMENT",   // the citymap Equipment button likewise
        "begin" => "BEGIN",               // NewGame's begin CTA
        // The chart's current-position label (design/03: "YOU ARE HERE" under the ringed node; the
        // dc.html glyph U+25BC isn't in the font regions — the ring itself marks the node).
        "map.current" => InRun ? "YOU ARE HERE" : null,
        // CityMap gauges (design/03): the panel binds carry the live counts + their flavor line (the
        // design's inner pip strips are a flattened-extraction gap, Needs-CD — the values render now).
        "supplies" => InRun
            ? $"SUPPLIES {Exp.Map.Supplies}/{Exp.Map.MaxSupplies}\n1 supply per deployment" : null,
        "support" => InRun
            ? $"MUSTERED SUPPORT {Exp.Map.SupportBank}/{Exp.Map.Nodes.Count(n => n.Type == Roguebane.Core.NodeType.ResourceHold)}"
              + "\nbanked from held beacons" : null,
        "enemy.advance" => InRun
            ? Exp.Map.WarPartyDistance + (Exp.Map.WarPartyDistance == 1 ? " WAYPOINT" : " WAYPOINTS")
              + " AWAY FROM CAMP" : null,
        // Scene descriptor: the node type is live data; locale place names ("the high pass") have no
        // model yet (design-open §17) — the type alone renders, nothing is invented.
        "encounter.label" => InRun ? NodeLabel(Exp.Map.Current.Type) : null,
        "campaign.taken" => InRun ? _campaign.LegIndex + " / " + _campaign.LegCount : null,
        _ => null,
    };

    // Run state exists only after a march; encounter binds fall back to samples until then.
    private bool InRun => _campaign is not null;

    private static string NodeLabel(Roguebane.Core.NodeType t) => t switch
    {
        Roguebane.Core.NodeType.ResourceHold => "RESOURCE HOLD",
        _ => t.ToString().ToUpperInvariant(),
    };

    // Smoke content-validation: does this element's bind resolve to LIVE data right now? Mirrors the
    // draw gating (scene images, the regen track, figures, lists, screen binds). Driven smokes
    // (RB_SCREEN=encounter RB_MF=all) shrink the unresolved list toward zero; the report makes a
    // bind that silently went dead visible the pass it happens.
    private bool BindResolves(Element e)
    {
        var b = e.Binds!;
        if (b.EndsWith(".scene")) return e.Image is { Length: > 0 } img && _assets.Texture(NormalizeContentPath(img)) is not null;
        if (b == "ShieldPool.regen") return InRun && Exp.Player.Body.ShieldRegenProgress > 0f;
        if (b == "enemy.advancePct") return InRun;
        if (b == "runes.budgetPct") return true; // build data always exists
        if (e.Type == "figure") return b is "preview.fig" or "Body" || (InRun && Exp.Enemy is not null);
        if (e.Type == "list") return ListData(b) is { Count: > 0 };
        if (e.Type == "graph") return InRun; // chart/cityGraph draw from live run/campaign state
        return ResolveScreenBind(b) is not null;
    }

    // A list container: stamp its item template into each cell (ListLayout), filling each part from the
    // i-th LIVE datum's `binds` (falling back to the manifest `sample` where a bind isn't mapped yet).
    private void DrawManifestList(Element e, Rectangle r)
    {
        var m = _ui.Manifest;
        if (m is null || e.Item is null || !m.Templates.TryGetValue(e.Item.Template, out var tmpl)) return;
        var data = ListData(e.Binds);
        var count = data?.Count ?? ListCountFor(e.Binds);
        var region = new LayoutRect(r.X, r.Y, r.Width, r.Height);
        var cells = ListLayout.Cells(region, e.Item, count, tmpl.Size);
        // Which datum is CHOSEN, for `.selection` parts (the ring/chip only the picked card wears).
        var selIx = e.Binds switch
        {
            "races" => _build.RaceIndex,
            "cores" => _build.CoreRuneIndex,
            _ => -1,
        };
        for (var i = 0; i < cells.Count; i++)
        {
            var datum = data is not null && i < data.Count ? data[i] : null;
            var cell = cells[i];
            if (tmpl.Parts.Length == 0) { DrawLeafTemplate(tmpl, cell, datum); continue; }
            // v4 card chrome: a parts-carrying template may style its own CELL (root fill/border,
            // restyled per state — spineCity's taken/current/future borders; pickerCard's
            // idle/selected follows the chosen index). Parts stamp on top.
            string? rootState = null;
            if (tmpl.States.ValueKind == System.Text.Json.JsonValueKind.Object
                && tmpl.States.TryGetProperty("family", out var famEl))
                rootState = famEl.GetString() switch
                {
                    "pickerCard" => i == selIx ? "selected" : "idle",
                    // actionCard (design/01): the card FRAME reads the FSM — targeting pulse,
                    // gold held, dim cooldown, dashed locked; idle/unpowered cards read locked.
                    "actionCard" when datum is Roguebane.Core.Technique => (
                        e.Binds == "loadout.techniques" && InRun && _ctrl.Targeting == i ? "targeting"
                        : ResolveStateBind(datum, "technique.state") switch
                        {
                            "HELD" => "held",
                            "READY" => "ready",
                            "COOLDOWN" => "cooldown",
                            "DRY" => "locked",
                            _ => InRun ? "locked" : "ready", // unpowered in-run reads locked
                        }),
                    _ => null,
                };
            DrawTemplateRootChrome(tmpl, cell, datum, rootState);
            // Positional binds repeat the SAME bind N times per card (attr tiles 4x, attr-bar pips 12x,
            // in template order); count each occurrence per card to pick the right datum slice.
            int valIx = 0, keyIx = 0, pipIx = 0;
            var occ = new System.Collections.Generic.Dictionary<string, int>(); // per-bind occurrence (rune rows)
            foreach (var pp in CardTemplate.Place(tmpl, cell.X, cell.Y))
            {
                if (pp.Binds is { } sel && sel.EndsWith(".selection") && i != selIx)
                    continue; // only the chosen card wears its selection chip
                // Merchant rows: an UNBOUND part is design-mock filler (the sample price digits) —
                // never draw it beside live data. A NESTED wares region stamps its own cards.
                if (datum is MerchantOffer or MerchantLot or ResourceReadout or MerchantSection
                    && pp.Binds is null) continue;
                // Technique/minion cards likewise: an UNBOUND sample part is design-mock filler
                // (the loose damage/cost digits) — never stamp it over live card copy (P0-C.9).
                if (datum is Roguebane.Core.Technique or Roguebane.Core.Minion
                    && pp.Binds is null && !string.IsNullOrEmpty(pp.Sample)) continue;
                // Nested rune list (07-03 drop): each group's region stamps runeCard rows — the
                // held rung (or the first) then the next, the pair the player acts on (RuneRow).
                if (pp.List is { } runeList && pp.Binds == "g.runes"
                    && datum is System.Collections.Generic.IReadOnlyList<Roguebane.Core.Mark> gLadder
                    && gLadder.Count > 0 && _ui.Manifest is { } mR
                    && mR.Templates.TryGetValue(runeList.Template, out var runeT))
                {
                    var rows = new List<Roguebane.Core.Mark>();
                    if (RuneRow(datum, 0) is { } r0) rows.Add(r0);
                    if (RuneRow(datum, 1) is { } r1 && rows.Count > 0 && !ReferenceEquals(r1, rows[0]))
                        rows.Add(r1);
                    var runeCells = ListLayout.Cells(pp.Rect, runeList, rows.Count, runeT.Size);
                    for (var ri = 0; ri < runeCells.Count; ri++)
                        foreach (var rp in CardTemplate.Place(runeT, runeCells[ri].X, runeCells[ri].Y))
                            DrawRunePart(rp, rows[ri]);
                    continue;
                }
                // Nested pip strip (§12): a part carrying its OWN list stamps a leaf template per
                // cell, the cells sliced from the ROW datum (pool rows / attr bars).
                if (pp.List is { } nested && pp.Binds is "pool.attr.cells" or "attrs.cells"
                    && datum is ValueTuple<string, string, int, int, string> rowD)
                {
                    if (_ui.Manifest is { } mN && mN.Templates.TryGetValue(nested.Template, out var pipT))
                    {
                        var cellsData = PoolCells(rowD);
                        var pipCells = ListLayout.Cells(pp.Rect, nested, cellsData.Count, pipT.Size);
                        for (var pi = 0; pi < pipCells.Count; pi++)
                            DrawLeafTemplate(pipT, pipCells[pi], cellsData[pi]);
                    }
                    continue;
                }
                if (pp.Binds == "section.wares" && datum is MerchantSection ws)
                {
                    if (_ui.Manifest is { } mm && mm.Templates.TryGetValue("wareCard", out var wc))
                        for (var wi = 0; wi < ws.Wares.Count; wi++)
                        {
                            var wx = pp.Rect.X + wi * (wc.Size[0] + WareGap);
                            if (wx + wc.Size[0] > pp.Rect.X + pp.Rect.W + 1) break;
                            foreach (var wp in CardTemplate.Place(wc, wx, pp.Rect.Y))
                                DrawWarePart(wp, ws.Wares[wi]);
                        }
                    continue;
                }
                // FSM state parts resolve from the LIVE run — an idle card shows no chip/label at all
                // (never the sample), so resolve BEFORE chrome and bail when there's nothing to say.
                string? stateText = null;
                var isStatePart = pp.Binds is "technique.state" or "bay.state" or "technique.cooldownLabel";
                if (isStatePart)
                {
                    // The card the targeting FSM is picking a foe for reads AIMING / "locking on".
                    var aiming = e.Binds == "loadout.techniques" && InRun && _ctrl.Targeting == i;
                    stateText = ResolveStateBind(datum, pp.Binds!, aiming);
                    if (string.IsNullOrEmpty(stateText)) continue;
                }
                // Rune-bag rows (g.runes.* repeats twice per group): a LIVE ladder shows its held
                // rung (or the first) then the next — resolved copy only, never the template samples.
                if (pp.Binds is { } gb && gb.StartsWith("g.runes") && datum is not null)
                {
                    var row = occ.GetValueOrDefault(gb);
                    occ[gb] = row + 1;
                    var rtxt = RuneRow(datum, row) is { } mk ? RuneBind(mk, gb) : null;
                    if (!string.IsNullOrEmpty(rtxt))
                        TextPxWrapped(pp.Font == "display" ? _assets.Display : _assets.Mono,
                            rtxt!, RectOf(pp.Rect), _ui.Color(pp.Color ?? "ink", Ink), pp.FontPx);
                    continue;
                }
                // Charge/cooldown progress: the fill bar's WIDTH is the resolved fraction.
                if (pp.Binds == "technique.chargePct")
                {
                    if (InRun && datum is Roguebane.Core.Technique ct && pp.Fill is { } cf)
                    {
                        var st = Exp.Status(ct);
                        var pct = st.Ready ? 1f
                            : st.Active && st.Cooldown > 0 ? 1f - (float)st.Countdown / st.Cooldown : 0f;
                        if (pct > 0f)
                            DrawFill(new Rectangle(pp.Rect.X, pp.Rect.Y,
                                (int)(pp.Rect.W * Math.Clamp(pct, 0f, 1f)), pp.Rect.H), cf);
                    }
                    continue;
                }
                // Remaining state-driven chrome (.rarity) still needs its model — keep gated.
                var stateBound = pp.Binds is { } sb && sb.EndsWith(".rarity");
                if (!stateBound)
                {
                    // attr.color binds the swatch's fill TOKEN to the datum (str/int/dex/con);
                    // attrs.pip picks per PIP INDEX: filled -> the attr's token, allocatable -> slot,
                    // beyond the cap -> nothing.
                    // colorBind (manifest-declared) wins; the older bind-specific tinting stays as the
                    // fallback for manifests that predate it.
                    string? fillTok = ResolveColorToken(pp.ColorBind, datum);
                    var skipFill = false;
                    if (fillTok is not null) { }
                    else if (pp.Binds == "attr.color" && datum is not null)
                        fillTok = ResolveBind(datum, pp.Binds);
                    else if (pp.Binds == "attrs.pip" && datum is ValueTuple<string, string, int, int, string> ab)
                    {
                        var p = pipIx++;
                        fillTok = p < ab.Item3 ? ab.Item5 : p < ab.Item4 ? "slot" : null;
                        skipFill = fillTok is null;
                    }
                    // Glyph tiles colour by the datum's stat (technique/minion cards).
                    else if (pp.Binds is "technique.icon" or "loadout.glyph" && datum is Roguebane.Core.Technique tq)
                        fillTok = tq.Stat.ToString().ToLowerInvariant();
                    else if (pp.Binds is "technique.icon" or "loadout.glyph" && datum is Roguebane.Core.Minion mnq)
                        fillTok = mnq.Stat.ToString().ToLowerInvariant();
                    if (fillTok is not null) DrawFill(RectOf(pp.Rect), new Fill { Token = fillTok });
                    else if (!skipFill && pp.Fill is { } pf) DrawFill(RectOf(pp.Rect), pf);
                    if (pp.Border is { } pb)
                        Border(pp.Rect.X, pp.Rect.Y, pp.Rect.W, pp.Rect.H, _ui.Color(pb.Color, Border0),
                            BorderPx(pb.W), pb.Sides);
                }
                // imageBind (CD #15): a Content path template whose {bind} placeholders resolve
                // from the bound item — the part blits that PNG instead of text/fill glyphs.
                var img = pp.Image;
                if (pp.Binds == "core.icon" && datum is Roguebane.Core.CoreRune ci)
                    img = "icons/rune/core_" + ci.Id; // identity token PNG, not the sample glyph
                if (pp.ImageBind is { } ib && datum is not null)
                    img = System.Text.RegularExpressions.Regex.Replace(ib, @"\{(.+?)\}",
                        mm => ResolveBind(datum, mm.Groups[1].Value) ?? "");
                if (!string.IsNullOrEmpty(img))
                {
                    // A missing icon PNG keeps the glyph-tile fill drawn above — never Sprite's
                    // null-texture border box (an un-shipped icon is a Needs-CD gap, not a frame).
                    if (_assets.Texture(img!) is { } itex)
                    {
                        Sprite(itex, pp.Rect.X, pp.Rect.Y, pp.Rect.W, pp.Rect.H, Color.White);
                        continue;
                    }
                    if (pp.ImageBind is not null) continue; // icon slot stays the tinted tile
                }
                var text = isStatePart ? stateText : pp.Binds switch
                {
                    "race.attrs.value" => AttrTile(datum, valIx++)?.value,
                    "race.attrs.key" => AttrTile(datum, keyIx++)?.key,
                    "attr.color" => null, // the swatch is pure fill, no text
                    // Hotkey chips (design/01, §8): the number IS the card's bar position — techniques
                    // first, then bays continue the sequence (same order the D1..D6 keys press).
                    "technique.hotkey" => (i + 1).ToString(),
                    "bay.hotkey" => (TechniqueCount() + i + 1).ToString(),
                    _ => datum is not null ? ResolveBind(datum, pp.Binds) : null,
                } ?? pp.Sample;
                if (!string.IsNullOrEmpty(text))
                    TextPxWrapped(pp.Font == "display" ? _assets.Display : _assets.Mono,
                        text!, RectOf(pp.Rect), _ui.Color(pp.Color ?? "ink", Ink), pp.FontPx);
            }
        }
    }

    // How many technique cards precede the bay lane on the action bar (hotkey numbering).
    private int TechniqueCount() => (InRun ? Exp.Equipment : _build.Equipment).Count;

    // The i-th attribute tile (STR/INT/DEX/CON order) of a Race datum, for the per-attr card tiles.
    private static (string key, string value)? AttrTile(object? datum, int i) => datum is Roguebane.Core.Race r
        ? i switch
        {
            0 => ("STR", r.Str.ToString()),
            1 => ("INT", r.Int.ToString()),
            2 => ("DEX", r.Dex.ToString()),
            3 => ("CON", r.Con.ToString()),
            _ => null,
        }
        : null;

    private static Rectangle RectOf(LayoutRect r) => new(r.X, r.Y, r.W, r.H);

    // The live data a bound list stamps one card per, or null for an unmapped bind (falls back to samples).
    private System.Collections.Generic.IReadOnlyList<object>? ListData(string? bind) => bind switch
    {
        "races" => Roguebane.Core.Content.Races.Roster.Cast<object>().ToList(),
        "cores" => Roguebane.Core.Content.CoreRunes.Roster.Cast<object>().ToList(),
        "preview.attrs" => PreviewAttrs(),
        "attrs" => AttrBars(),
        "loadout" => (InRun ? Exp.Equipment : _build.Equipment).Cast<object>().ToList(),
        "minions" => InRun ? Exp.Minions.Cast<object>().ToList()
                           : _build.CoreRune.MinionKit.Concat(_build.Runes.GrantedMinions).Cast<object>().ToList(),
        // Inventory follows the tab strip: GEAR = the run's wielded/worn/packed pieces (empty pre-run
        // — gear only exists once marching), TECHNIQUES = the palette, MINIONS = the retinue.
        // (CD's 07-02 drop renamed the bind invItems -> inventory.activeTab.items; chased clean.)
        "inventory.tabs" => new List<object> { "GEAR", "TECHNIQUES", "MINIONS" },
        "inventory.activeTab.items" => _invTab switch
        {
            0 => InRun
                ? Exp.Player.Body.Hands.Cast<object>()
                    .Concat(Exp.Stash.Weapons).Concat(Exp.Stash.Armor).ToList()
                : new List<object>(),
            // §12: bought techniques/minions join the pool the Equipment screen slots from.
            1 => _build.Palette.Cast<object>()
                .Concat(InRun ? Exp.Stash.Techniques : Enumerable.Empty<object>().Cast<Roguebane.Core.Technique>()).ToList(),
            2 => _build.CoreRune.MinionKit.Concat(_build.Runes.GrantedMinions).Cast<object>()
                .Concat(InRun ? Exp.Stash.Minions : Enumerable.Empty<Roguebane.Core.Minion>()).ToList(),
            _ => null,
        },
        // The Rune Bag (design/02): one group per PATH ladder — the MARKS/PATHS/KEYSTONES taxonomy
        // is OPEN (§17), so the model's actual grouping (ladders) is what renders.
        "runeGroups" => _build.Paths.Cast<object>().ToList(),
        // Equipment identity block (design/02): the core's headline numbers as label/value rows —
        // all live core/build data, no invented figures.
        "core.stats" => new List<object>
        {
            ("bays", _build.CoreRune.Bays.ToString()),
            ("actions", _build.CoreRune.Kit.Count.ToString()),
            ("budget", _build.CoreRune.RuneBudget.ToString()),
        },
        // CityMap chart legend (design/03): what the node icons mean — display metadata, same rows the
        // legacy legend drew; icon tokens through NodeToken so the key can't drift from the chart.
        "legend" => new List<object>
        {
            (AssetRegistry.NodeToken(Roguebane.Core.NodeType.Castle), "castle / exit"),
            (AssetRegistry.NodeToken(Roguebane.Core.NodeType.Merchant), "merchant"),
            (AssetRegistry.NodeToken(Roguebane.Core.NodeType.ResourceHold), "resource hold"),
            (AssetRegistry.NodeToken(Roguebane.Core.NodeType.Unknown), "unknown/fight"),
        },
        // Encounter (design/01): the combat pool + action bar read the RUN body once marching.
        "pool" => AttrBars(),
        "loadout.techniques" => InRun ? Exp.Equipment.Cast<object>().ToList()
                                      : _build.Equipment.Cast<object>().ToList(),
        "loadout.bays" => InRun ? Exp.Minions.Cast<object>().ToList()
                                : _build.CoreRune.MinionKit.Cast<object>().ToList(),
        // Shield bar (§6b): one bool per pip, filled first. No standing sources -> no pips at all.
        "ShieldPool.points" => InRun
            ? Enumerable.Range(0, Exp.Player.Body.ShieldLayers)
                .Select(i => (object)(i < Exp.Player.Body.ShieldPoints)).ToList()
            : new List<object>(),
        // Segmented HP strips (07-03 drop, design/01): one pip per max-HP point, live-first — the
        // same point.live leaf-template shape as shield pips.
        "Body.hp.points" => InRun
            ? Enumerable.Range(0, Exp.Player.MaxHp).Select(i => (object)(i < Exp.Player.Hp)).ToList()
            : new List<object>(),
        "encounter.foe.hp.points" => InRun && Exp.Enemy is { } hpFoe
            ? Enumerable.Range(0, hpFoe.MaxHp).Select(i => (object)(i < hpFoe.Hp)).ToList()
            : new List<object>(),
        // CityMap gauge pips (07-03 drop): textured pips via ui/pip/{point.asset} — full/empty pairs
        // per resource family, one pip per unit (design/03).
        "supplies.points" => InRun
            ? Enumerable.Range(0, Exp.Map.MaxSupplies).Select(i => (object)new PipPoint(
                i < Exp.Map.Supplies,
                i < Exp.Map.Supplies ? "pip_full_supplies" : "pip_empty_supplies")).ToList()
            : new List<object>(),
        "support.points" => InRun
            ? Enumerable.Range(0, Exp.Map.Nodes.Count(n => n.Type == Roguebane.Core.NodeType.ResourceHold))
                .Select(i => (object)new PipPoint(
                    i < Exp.Map.SupportBank,
                    i < Exp.Map.SupportBank ? "pip_full_support" : "pip_empty_support")).ToList()
            : new List<object>(),
        // Merchant (§12, design/07): the heal offers + the seeded provision lots, live-priced.
        "merchant.healing.offers" => InRun && Exp.AtMerchant
            ? new List<object>
            {
                new MerchantOffer("Mend 1 HP", "a careful binding", Exp.HealPricePerHp),
                new MerchantOffer("Full repair", "every wound, at a premium", Exp.FullHealPrice),
            }
            : new List<object>(),
        "merchant.stock.sections" => InRun && Exp.AtMerchant
            ? MerchantSections().Skip(_merchantPage * SectionsPerPage).Take(SectionsPerPage)
                .Cast<object>().ToList()
            : new List<object>(),
        "merchant.provisions.stock" => InRun && Exp.AtMerchant
            ? new List<object>
            {
                new MerchantLot("supplies", "Supplies", Exp.SuppliesStock, Exp.SuppliesPrice),
                new MerchantLot("charge", "Charge", Exp.ChargeStock, Exp.ChargePrice),
                new MerchantLot("summons", "Summons", Exp.SummonsStock, Exp.SummonsPrice),
            }
            : new List<object>(),
        // Campaign strip (design/03/04, ex-overlay): one spineCity chip per campaign leg —
        // taken (good border) / current (amber) / future (dim), straight from the live campaign.
        "campaign.cities" => InRun
            ? Enumerable.Range(0, _campaign.LegCount).Select(i => (object)new CityLeg(
                i < _campaign.LegIndex ? "taken" : i == _campaign.LegIndex ? "current" : "future")).ToList()
            : new List<object>(),
        // Castle fortifications (design/03): the structured boss's parts + their live condition.
        // The parts only EXIST while the castle encounter is live — the panel's rows are
        // state-gated until then (no persistent fort-damage model; §17 keeps that open).
        "city.castle.parts" => InRun && Exp.Enemy is { Id: "castle", Frame: { } cf }
            ? cf.Parts.Select(p => (object)new FortPart(
                DisplayName(p.Id),
                cf.Contribution(p) >= p.Capacity ? "INTACT"
                : cf.Contribution(p) == 0 ? "BROKEN" : "DAMAGED")).ToList()
            : new List<object>(),
        // PACK chips (design/03, ex-overlay homed by the 07-03 drop): the run's carried gear —
        // wielded pieces plus the packed stash. Chips read gear.name + a stat-tinted swatch.
        "Body.gear" => InRun
            ? Exp.Player.Body.Hands.Cast<object>()
                .Concat(Exp.Stash.Weapons).Concat(Exp.Stash.Armor).ToList()
            : new List<object>(),
        // The in-run resource strip as manifest data (id/value/label per chip).
        "run.resources" => InRun
            ? new List<object>
            {
                new ResourceReadout("supplies", Exp.Map.Supplies + "/" + Exp.Map.MaxSupplies, "SUPPLIES"),
                new ResourceReadout("spoils", Exp.Gold.ToString(), "GOLD"),
                new ResourceReadout("charge", Exp.Charge + "/" + Exp.MaxCharge, "CHARGE"),
                new ResourceReadout("summons", Exp.Summons + "/" + Exp.MaxSummons, "SUMMONS"),
            }
            : new List<object>(),
        _ => null,
    };

    // One pool/attr row's pip cells: capacity cells total, free-first — free points render the
    // stat-tinted full pip, reserved ones the reserved variant (design/01 pool rows, design/02 bars).
    private static List<object> PoolCells(ValueTuple<string, string, int, int, string> row)
        => Enumerable.Range(0, row.Item4).Select(i => (object)new PoolCell(
            i < row.Item3 ? "full" : "reserved",
            (i < row.Item3 ? "pip_full_" : "pip_reserved_") + row.Item5)).ToList();

    // A campaign leg on the spine strip: taken / current / future (spineCity state key).
    private sealed record CityLeg(string Status);
    // One castle fortification row: the structured boss's part + its live condition.
    private sealed record FortPart(string Name, string State);

    // A textured gauge/strip pip: live/spent + which ui/pip PNG renders it (imageBind).
    private sealed record PipPoint(bool Live, string Asset);
    // One cell of a pool/attr pip strip: full/reserved + its per-stat ui/pip PNG.
    private sealed record PoolCell(string State, string Asset);

    // Merchant list data (shell-side view records — the Core sells, the shell narrates).
    private sealed record MerchantOffer(string Name, string Note, int Price);
    private sealed record MerchantLot(string Id, string Name, int Qty, int Price);
    private sealed record ResourceReadout(string Id, string Value, string Label);
    private sealed record MerchantSection(string Label, IReadOnlyList<Ware> Wares);
    private sealed record Ware(string Category, string Name, string Note, string Desc,
        string PriceText, string BuyState, object Item);

    // Wares-shelf geometry shared by render + click hit-test: cards flow horizontally inside the
    // section's wares region; 3 sections fill a page of the shelf area.
    private const int WareGap = 11, SectionsPerPage = 3;
    private int _merchantPage;

    // §12 (receiving LOCKED 2026-07-03): EVERY ware category is a click-to-buy tile. A purchase
    // lands in the run inventory — technique -> palette pool, minion -> minion inventory,
    // rune -> rune bag — and slotting stays the Equipment screen's job.
    private List<MerchantSection> MerchantSections()
    {
        var s = new List<MerchantSection>();
        void Add(string label, IEnumerable<Ware> wares)
        {
            var list = wares.ToList();
            if (list.Count > 0) s.Add(new MerchantSection(label, list));
        }
        Add("WEAPONS", Exp.OfferedWeapons.Select(w => new Ware("WPN", DisplayName(w.Id),
            w.Stat.ToString().ToUpperInvariant() + " " + w.Reserve, "",
            Roguebane.Core.Expedition.Price(w) + "g", "BUY", w)));
        Add("ARMOR", Exp.OfferedArmor.Select(a => new Ware("ARM", DisplayName(a.Id),
            a.Kind.ToString().ToUpperInvariant() + " " + a.Group.ToString().ToUpperInvariant(), "",
            Roguebane.Core.Expedition.Price(a) + "g", "BUY", a)));
        Add("TECHNIQUES", Exp.OfferedTechniques.Select(t => new Ware("TEC", DisplayName(t.Id),
            t.Stat.ToString().ToUpperInvariant() + " " + t.Reserve, t.DescText,
            Roguebane.Core.Expedition.Price(t) + "g", "BUY", t)));
        Add("MINIONS", Exp.OfferedMinions.Select(m => new Ware("MIN", DisplayName(m.Id),
            m.Stat.ToString().ToUpperInvariant() + " " + m.Reserve, m.DescText,
            Roguebane.Core.Expedition.Price(m) + "g", "BUY", m)));
        Add("RUNES", Exp.OfferedMarks.Select(k => new Ware("RUNE", k.DisplayName,
            "RANK " + k.Rank, "", Roguebane.Core.Expedition.Price(k) + "g", "BUY", k)));
        return s;
    }

    private int MerchantPageCount()
        => Math.Max(1, (MerchantSections().Count + SectionsPerPage - 1) / SectionsPerPage);

    // Ware-card hit-test sharing the nested-stamping geometry: page sections in their manifest list
    // cells, cards flowing horizontally through each section's wares region. Template ids are needed
    // for GEOMETRY here (guarded — a CD rename degrades to no shelves, never a crash).
    private IEnumerable<(object Item, Rectangle Rect)> WareRects()
    {
        if (_ui.Manifest is not { } m || !m.Templates.TryGetValue("wareCard", out var wc)
            || !m.Templates.TryGetValue("shopSection", out var sect)) yield break;
        var waresPart = sect.Parts.FirstOrDefault(p => p.Binds == "section.wares");
        if (waresPart is null) yield break;
        var sections = MerchantSections().Skip(_merchantPage * SectionsPerPage)
            .Take(SectionsPerPage).ToList();
        var cells = ManifestListCells("merchant", "merchant.stock.sections", sections.Count);
        for (var si = 0; si < sections.Count && si < cells.Count; si++)
        {
            var rx = cells[si].X + waresPart.Rect[0];
            var ry = cells[si].Y + waresPart.Rect[1];
            var wares = sections[si].Wares;
            for (var wi = 0; wi < wares.Count; wi++)
            {
                var wx = rx + wi * (wc.Size[0] + WareGap);
                if (wx + wc.Size[0] > rx + waresPart.Rect[2] + 1) break;
                yield return (wares[wi].Item, new Rectangle(wx, ry, wc.Size[0], wc.Size[1]));
            }
        }
    }

    // One stamped ware-card part: chrome + bound text. A bound part resolving to "" is a SUPPRESSED
    // slot (rarity tags / buy chips whose model isn't built) — no chrome, no sample. Unbound parts are
    // design-mock filler and never draw against live data.
    private void DrawWarePart(PlacedPart wp, Ware w)
    {
        if (wp.Binds is null) return;
        var bound = ResolveBind(w, wp.Binds);
        if (bound == "") return;
        if (ResolveColorToken(wp.ColorBind, w) is { } tok) DrawFill(RectOf(wp.Rect), new Fill { Token = tok });
        else if (wp.Fill is { } f) DrawFill(RectOf(wp.Rect), f);
        if (wp.Border is { } b)
            Border(wp.Rect.X, wp.Rect.Y, wp.Rect.W, wp.Rect.H, _ui.Color(b.Color, Border0),
                BorderPx(b.W), b.Sides);
        var text = bound ?? wp.Sample;
        if (string.IsNullOrEmpty(text)) return;
        var font = wp.Font == "display" ? _assets.Display : _assets.Mono;
        TextPxWrapped(font, text, new Rectangle(wp.Rect.X, wp.Rect.Y, wp.Rect.W, wp.Rect.H),
            _ui.Color(wp.Color, Ink), wp.FontPx);
    }

    // The attribute bars/pool rows: one datum per stat — (key, part label §6, free pool, capacity,
    // pip colour token). In a run the LIVE body supplies them (actives reserve, damage shrinks caps);
    // pre-run it's the build preview, where nothing is reserved so free == capacity.
    private System.Collections.Generic.IReadOnlyList<object> AttrBars()
    {
        var b = InRun ? Exp.Player.Body : _build.Preview();
        return new object[]
        {
            ("STR", "Arms", b.Available(Stat.Str), b.Capacity(Stat.Str), "str"),
            ("INT", "Head", b.Available(Stat.Int), b.Capacity(Stat.Int), "int"),
            ("DEX", "Legs", b.Available(Stat.Dex), b.Capacity(Stat.Dex), "dex"),
            ("CON", "Chest", b.Available(Stat.Con), b.Capacity(Stat.Con), "con"),
        };
    }

    // The Loadout preview's 4 attribute tiles: key/value/swatch-token per stat, from the LIVE build.
    private System.Collections.Generic.IReadOnlyList<object> PreviewAttrs()
    {
        var b = _build.Preview();
        return new object[]
        {
            ("STR", b.Capacity(Stat.Str).ToString(), "str"),
            ("INT", b.Capacity(Stat.Int).ToString(), "int"),
            ("DEX", b.Capacity(Stat.Dex).ToString(), "dex"),
            ("CON", b.Capacity(Stat.Con).ToString(), "con"),
        };
    }

    // Manifest image paths may be authored project-relative ("Content/bg/x.png"); the pipeline wants
    // the content NAME (no root, no extension).
    private static string NormalizeContentPath(string p)
    {
        if (p.StartsWith("Content/")) p = p["Content/".Length..];
        return p.EndsWith(".png") ? p[..^4] : p;
    }

    // Root fill/border of a parts-carrying template, restyled by its `states` block keyed from the
    // datum's resolved root bind (spineCity: city.status -> taken/current/future borders).
    private void DrawTemplateRootChrome(Template t, LayoutRect cell, object? datum, string? stateKey = null)
    {
        if (t.Fill is null && t.Border is null) return;
        var fillTok = t.Fill?.Token;
        var borderTok = t.Border?.Color;
        var key = stateKey ?? (datum is not null && t.Binds is { } b ? ResolveBind(datum, b) : null);
        if (key is not null && t.States.ValueKind == System.Text.Json.JsonValueKind.Object
            && t.States.TryGetProperty(key, out var st)
            && st.ValueKind == System.Text.Json.JsonValueKind.Object)
        {
            if (st.TryGetProperty("fill", out var f)) fillTok = f.GetString();
            if (st.TryGetProperty("border", out var bo)) borderTok = bo.GetString();
        }
        if (fillTok is { Length: > 0 })
            DrawFill(new Rectangle(cell.X, cell.Y, cell.W, cell.H), new Fill { Token = fillTok });
        if (borderTok is { Length: > 0 })
            Border(cell.X, cell.Y, cell.W, cell.H, _ui.Color(borderTok, Border0),
                BorderPx(t.Border?.W ?? 1), t.Border?.Sides);
    }

    // A self-styled LEAF template (no parts): the cell itself is the visual — its fill/border restyled
    // by the template's `states` block keyed from the bound datum (shield pips: live/spent). Dashed
    // border styles draw solid for now (a 8x5 design-px pip; dash polish rides the pixel-compare pass).
    private void DrawLeafTemplate(Template t, LayoutRect cell, object? datum)
    {
        var fillTok = t.Fill?.Token;
        var borderTok = t.Border?.Color;
        var key = t.Binds == "point.live" && (datum is bool || datum is PipPoint)
            ? (datum is true or PipPoint { Live: true } ? "live" : "spent") : null;
        if (key is not null && t.States.ValueKind == System.Text.Json.JsonValueKind.Object
            && t.States.TryGetProperty(key, out var st)
            && st.ValueKind == System.Text.Json.JsonValueKind.Object)
        {
            if (st.TryGetProperty("fill", out var f)) fillTok = f.GetString();
            if (st.TryGetProperty("border", out var b)) borderTok = b.GetString();
        }
        var r = new Rectangle(cell.X, cell.Y, cell.W, cell.H);
        // A textured pip (imageBind, §12) IS the visual — the PNG replaces fill+border chrome.
        if (t.ImageBind is { } ib && datum is not null
            && System.Text.RegularExpressions.Regex.Replace(ib, @"\{(.+?)\}",
                mm => ResolveBind(datum, mm.Groups[1].Value) ?? "") is { Length: > 0 } path
            && !path.EndsWith("/") && _assets.Texture(NormalizeContentPath(path)) is { } pip)
        {
            Sprite(pip, r.X, r.Y, r.Width, r.Height, Color.White);
            return;
        }
        if (fillTok is { Length: > 0 }) DrawFill(r, new Fill { Token = fillTok });
        if (borderTok is { Length: > 0 })
            Border(r.X, r.Y, r.Width, r.Height, _ui.Color(borderTok, Border0),
                BorderPx(t.Border?.W ?? 1), t.Border?.Sides);
    }

    // states (CD drop 2026-07-02): a button-family element draws its interaction skin under its label.
    // The shell picks the state — disabled (its bind resolved null), on (a lit toggle), down/hover from
    // the pointer, else normal — and nine-slices the named texture like DrawButton (CD #11 corners).
    private bool DrawStateSkin(Element e, Rectangle r, bool enabled)
    {
        if (e.States.ValueKind != System.Text.Json.JsonValueKind.Object
            || !e.States.TryGetProperty("family", out var fam) || fam.GetString() != "button")
            return false;
        var key = !enabled ? "disabled"
            : e.Binds == "combat.autoAttack" && InRun && Exp.IsAuto() ? "on"
            : Hover(r) ? (Mouse.GetState().LeftButton == ButtonState.Pressed ? "down" : "hover")
            : "normal";
        if (!e.States.TryGetProperty(key, out var pathEl) || pathEl.GetString() is not { Length: > 0 } path
            || _assets.Texture(path) is not { } skin)
            return false;
        foreach (var p in NineSlice.Patches(skin.Width, skin.Height, ButtonSlice,
                     new LayoutRect(r.X, r.Y, r.Width, r.Height), tile: false, centerFill: true,
                     dstCornerScale: 1.0 / ChromeBake))
            _spriteBatch.Draw(skin, new Rectangle(p.Dst.X, p.Dst.Y, p.Dst.W, p.Dst.H),
                new Rectangle(p.Src.X, p.Src.Y, p.Src.W, p.Src.H), Color.White);
        return true;
    }

    // colorBind (CD drop 2026-07-02, APPROVED): resolve a bound COLOUR — a palette token derived from
    // the datum (attr colours from a stat, a core's accent) — or null to keep the part's static chrome.
    // ware.* waits on the merchant consumer; core accent VALUES are unset content (resolve when authored).
    private string? ResolveColorToken(string? colorBind, object? datum) => colorBind switch
    {
        null or "" => null,
        "preview.accent" => _build.CoreRune.Accent is { Length: > 0 } a ? a : null,
        "core.accent" when datum is Roguebane.Core.CoreRune c =>
            c.Accent is { Length: > 0 } ca ? ca : null,
        "attr.color" when datum is not null => ResolveBind(datum, "attr.color"),
        "technique.attrColor" or "loadout.attrColor" or "invItems.attrColor" or "bay.gateColor" =>
            datum switch
            {
                Roguebane.Core.Technique t => t.Stat.ToString().ToLowerInvariant(),
                Roguebane.Core.Minion m => m.Stat.ToString().ToLowerInvariant(),
                Roguebane.Core.Weapon w => w.Stat.ToString().ToLowerInvariant(),
                Roguebane.Core.Armor ar => ar.Group.ToString().ToLowerInvariant(),
                _ => null,
            },
        _ => null,
    };

    // Resolve a template part's `binds` against a live datum -> display text, or null to use the sample.
    // Missing-data binds (race tag/blurb, per-attr tiles, Core Effect text) return null pending their data.
    private static string? ResolveBind(object datum, string? bind) => datum switch
    {
        System.Collections.Generic.IReadOnlyList<Roguebane.Core.Mark> lad => bind switch
        {
            // §17 keeps the MARKS/PATHS/KEYSTONES taxonomy OPEN — the group header reads the
            // ladder's live PATH name rather than inventing a category.
            "runeGroups.type" => lad.Count > 0 ? lad[0].Path.ToUpperInvariant() : null,
            _ => null,
        },
        PipPoint pt => bind switch
        {
            "point.asset" => pt.Asset,
            "point.live" => pt.Live ? "live" : "spent",
            _ => null,
        },
        CityLeg leg => bind switch
        {
            "city.status" => leg.Status,
            _ => null, // city.castle = the icon slot; its static imageBind carries the visual
        },
        FortPart fp => bind switch
        {
            "fort.name" => fp.Name,
            "fort.state" => fp.State,
            _ => null,
        },
        PoolCell pc => bind switch
        {
            "cell.asset" => pc.Asset,
            "cell.state" => pc.State,
            _ => null,
        },
        Roguebane.Core.Race r => bind switch
        {
            "race.name" => r.Name,
            "race.hp" => r.Hp.ToString(),
            "race.tag" => r.Tag,
            "race.blurb" => r.Blurb,
            _ => null,
        },
        Roguebane.Core.CoreRune c => bind switch
        {
            "core.name" => c.Title,
            "core.role" => c.Archetype,
            "core.budget" => c.RuneBudget.ToString(),
            "core.bays" => c.Bays.ToString(),
            "core.actionSlots" => c.Kit.Count.ToString(),
            "core.coreEffectName" => c.CoreEffectName,
            "core.coreEffectDesc" => c.CoreEffectDesc, // core.coreEffect = block chrome, resolves to nothing
            _ => null,
        },
        ValueTuple<string, string> kv => bind switch // (key, value): legend rows, core-stat rows
        {
            "legend.type" or "stat.label" => kv.Item1,
            "legend.label" or "stat.value" => kv.Item2,
            _ => null,
        },
        ValueTuple<string, string, string> a => bind switch // (key, value, swatch-token) attr tile
        {
            "attr.key" => a.Item1,
            "attr.value" => a.Item2,
            "attr.color" => a.Item3,
            _ => null,
        },
        ValueTuple<string, string, int, int, string> ab => bind switch // (key, part, free, cap, token) attr bar
        {
            "attrs.key" or "pool.attr.key" => ab.Item1,
            "attrs.part" or "pool.attr.part" => ab.Item2,
            "attrs.alloc" or "pool.attr.alloc" => ab.Item3.ToString(),
            "attrs.available" or "pool.attr.available" => ab.Item4.ToString(),
            _ => null,
        },
        MerchantSection sec => bind switch // §12 wares shelves (design/07)
        {
            "section.label" => sec.Label,
            "section.count" => sec.Wares.Count.ToString(),
            _ => null,
        },
        Ware w2 => bind switch // ware cards; "" SUPPRESSES a slot whose model isn't built (tag/buy)
        {
            "ware.category" => w2.Category,
            "ware.tag" => "",
            "ware.name" => w2.Name,
            "ware.note" => w2.Note,
            "ware.desc" => w2.Desc,
            "ware.price" => w2.PriceText,
            "ware.buyState" => w2.BuyState,
            _ => null,
        },
        MerchantOffer off => bind switch // §12 heal rows (design/07)
        {
            "offer.name" => off.Name,
            "offer.note" => off.Note,
            "offer.price" => off.Price + "g",
            _ => null,
        },
        MerchantLot lot => bind switch // §12 provision rows: id feeds the imageBind icon path
        {
            "lot.id" => lot.Id,
            "lot.name" => lot.Name,
            "lot.qty" => "x" + lot.Qty,
            "lot.price" => lot.Price + "g",
            _ => null,
        },
        ResourceReadout res => bind switch // the in-run resource strip chips
        {
            "resource.id" => res.Id,
            "resource.value" => res.Value,
            "resource.label" => res.Label,
            _ => null,
        },
        Roguebane.Core.Technique t => bind switch
        {
            "loadout.name" or "invItems.name" or "technique.name" => DisplayName(t.Id),
            "loadout.attr" => t.Stat.ToString().ToUpperInvariant() + " " + t.Reserve,
            "invItems.badgeLabel" or "technique.cost" => t.Stat.ToString().ToUpperInvariant(),
            "invItems.badgeNum" => t.Reserve.ToString(),
            "technique.description" => t.DescText,
            // The mock's separately-positioned damage highlight can't land on live wrap — the
            // description already carries the number ({power}); resolve EMPTY so the sample never stamps.
            "technique.amount" => "",
            "technique.attr" => t.Stat.ToString().ToUpperInvariant(),
            "technique.id" or "loadout.id" => t.Id, // icons/technique/{id} imageBinds
            _ => null,
        },
        Roguebane.Core.Weapon w => bind switch
        {
            "invItems.name" or "gear.name" => DisplayName(w.Id),
            "invItems.badgeLabel" => w.Stat.ToString().ToUpperInvariant(),
            "invItems.badgeNum" => w.Reserve.ToString(),
            _ => null,
        },
        Roguebane.Core.Armor ar => bind switch
        {
            "invItems.name" or "gear.name" => DisplayName(ar.Id),
            "invItems.badgeLabel" => ar.Group.ToString().ToUpperInvariant(),
            "invItems.badgeNum" => ar.Value.ToString(),
            _ => null,
        },
        Roguebane.Core.Minion mn => bind switch
        {
            "loadout.name" or "invItems.name" or "bay.name" => DisplayName(mn.Id),
            "loadout.id" => mn.Id,
            "loadout.attr" => mn.Stat.ToString().ToUpperInvariant() + " " + mn.Reserve,
            "invItems.badgeLabel" or "bay.cost" => mn.Stat.ToString().ToUpperInvariant(),
            "invItems.badgeNum" => mn.Reserve.ToString(),
            "bay.description" => mn.DescText,
            _ => null,
        },
        _ => null,
    };

    // Content ids are lower-case ("swing", "skeleton"); cards show them capitalised, per design/02.
    private static string DisplayName(string id) =>
        id.Length == 0 ? id : char.ToUpperInvariant(id[0]) + id[1..];

    // A card's live FSM read (design/01 chips + cooldown label): null = idle, show nothing.
    // Countdown is in fixed ticks (10/s) -> seconds for the label.
    private string? ResolveStateBind(object? datum, string bind, bool aiming = false)
    {
        if (!InRun) return null;
        if (datum is Roguebane.Core.Technique t)
        {
            var st = Exp.Status(t);
            return bind switch
            {
                "technique.state" => aiming ? "AIMING"
                    : st.ChargeDry ? "DRY"
                    : st.Sustained && st.Active ? "HELD"
                    : st.Ready ? "READY"
                    : st.Active && st.Countdown > 0 ? "COOLDOWN" : null,
                "technique.cooldownLabel" => aiming ? "locking on"
                    : st.Ready ? "ready"
                    : st.Sustained && st.Active ? "held"
                    : st.Active && st.Countdown > 0 ? (st.Countdown / 10f).ToString("0.0") + "s" : null,
                _ => null,
            };
        }
        // A fielded minion is ACTIVE by definition (the bays list holds the live retinue in a run).
        return datum is Roguebane.Core.Minion && bind == "bay.state" ? "ACTIVE" : null;
    }

    // Which rung a rune-group row shows: the held rung (or the first) then the next — the pair the
    // player acts on. Rows past the ladder clamp to the keystone.
    private Roguebane.Core.Mark? RuneRow(object? datum, int row)
    {
        if (datum is not System.Collections.Generic.IReadOnlyList<Roguebane.Core.Mark> ladder
            || ladder.Count == 0) return null;
        var held = _build.Runes.CurrentRank(ladder[0].Path);
        var first = held > 0 ? Math.Min(held - 1, ladder.Count - 1) : 0;
        return ladder[Math.Min(first + row, ladder.Count - 1)];
    }

    // One stamped runeCard part: bound parts resolve live copy through RuneBind (the state chip
    // takes its states-block color); unbound parts are design-mock filler and never draw.
    private void DrawRunePart(PlacedPart rp, Roguebane.Core.Mark m)
    {
        if (rp.Binds is not { } rb) return;
        var text = RuneBind(m, rb);
        if (string.IsNullOrEmpty(text)) return;
        var colTok = rp.Color;
        if (rb == "g.runes.state")
            colTok = text switch
            {
                "EQUIPPED" => "good", "EQUIPPABLE" => "amber", _ => "lockText",
            };
        if (rp.Fill is { } f) DrawFill(RectOf(rp.Rect), f);
        if (rp.Border is { } b)
            Border(rp.Rect.X, rp.Rect.Y, rp.Rect.W, rp.Rect.H, _ui.Color(b.Color, Border0),
                BorderPx(b.W), b.Sides);
        TextPxWrapped(rp.Font == "display" ? _assets.Display : _assets.Mono, text!,
            RectOf(rp.Rect), _ui.Color(colTok ?? "ink", Ink), rp.FontPx);
    }

    private string? RuneBind(Roguebane.Core.Mark m, string bind) => bind switch
    {
        "g.runes.name" => m.DisplayName,
        "g.runes.effect" => RuneEffect(m),
        "g.runes.state" => _build.Runes.Has(m) ? "EQUIPPED"
            : _build.Runes.CurrentRank(m.Path) == m.Rank - 1
                && _build.Runes.Available >= _build.Runes.EffectiveCost(m) ? "EQUIPPABLE" : "LOCKED",
        "g.runes.cost" => _build.Runes.EffectiveCost(m) + "p",
        _ => null, // icon glyph + stack countLabel have no model — draw nothing, never the sample
    };

    // A rune's effect line, derived from what the rung actually grants so copy can't drift from data.
    private static string RuneEffect(Roguebane.Core.Mark m)
    {
        var bits = new System.Collections.Generic.List<string>();
        foreach (var p in m.Granted) bits.Add("Sockets +" + p.Capacity + " " + p.Stat.ToString().ToUpperInvariant());
        foreach (var t in m.GrantedTechniques) bits.Add("Grants the " + DisplayName(t.Id) + " technique");
        foreach (var mn in m.GrantedMinions) bits.Add("Grants the " + DisplayName(mn.Id) + " minion");
        if (bits.Count == 0) bits.Add("Rung " + m.Rank + " of the " + m.Path + " ladder");
        return string.Join("; ", bits) + ".";
    }

    private static int ListCountFor(string? bind) => 3; // sample-count fallback for unmapped binds

    private void DrawFrameTex(Texture2D tex, Frame fr, Rectangle r)
    {
        var dst = new LayoutRect(r.X, r.Y, r.Width, r.Height);
        // v4 frames (07-03 drop) are authored at 1:1 DRAW size: border-image-width == slice in
        // SCENE px, independent of scene scale. dst here is design px, so scale by 1/SS — at SS=2
        // that's the old ChromeBake look; at higher scenes the chrome stays native instead of soft.
        foreach (var p in NineSlice.Patches(tex.Width, tex.Height, fr.Slice, dst,
                     tile: fr.Repeat == "tile", centerFill: fr.CenterFill, dstCornerScale: 1.0 / SS))
            _spriteBatch.Draw(tex, new Rectangle(p.Dst.X, p.Dst.Y, p.Dst.W, p.Dst.H),
                new Rectangle(p.Src.X, p.Src.Y, p.Src.W, p.Src.H), Color.White);
    }

    private void DrawFill(Rectangle r, Fill fill)
    {
        if (fill.IsGradient)
            DrawGradient(r.X, r.Y, r.Width, r.Height,
                _ui.Color(fill.From ?? "panel", PanelTop), _ui.Color(fill.To ?? "border", PanelBot),
                fill.Dir == "horizontal" ? GradientDir.Horizontal : GradientDir.Vertical);
        else if (!string.IsNullOrEmpty(fill.Token))
            Rect(r.X, r.Y, r.Width, r.Height, _ui.Color(fill.Token!, Panel0));
    }

    private enum GradientDir { Vertical, Horizontal }

    // §10 gradient fill, ENGINE-drawn: interpolate `from`->`to` across the rect in 1px strips (the
    // PointClamp sampler rules out a stretched-texture lerp). Diagonal isn't needed yet -> vertical.
    private void DrawGradient(int x, int y, int w, int h, Color from, Color to, GradientDir dir)
    {
        if (w <= 0 || h <= 0) return;
        if (dir == GradientDir.Horizontal)
            for (var i = 0; i < w; i++)
                Rect(x + i, y, 1, h, Color.Lerp(from, to, w <= 1 ? 0f : (float)i / (w - 1)));
        else
            for (var i = 0; i < h; i++)
                Rect(x, y + i, w, 1, Color.Lerp(from, to, h <= 1 ? 0f : (float)i / (h - 1)));
    }

    // §10 drop shadow, ENGINE-drawn (never baked into art -> resolution-independent): the element
    // silhouette offset by (dx,dy), softened by `blur` concentric rings of decaying alpha, UNDER the
    // element. Drawn outer-faint -> inner-dark so the core reads solid and the edge fades.
    private void DrawShadow(int x, int y, int w, int h, int dx, int dy, int blur, float opacity)
    {
        if (w <= 0 || h <= 0) return;
        var peak = (int)(Math.Clamp(opacity, 0f, 1f) * 255);
        for (var i = blur; i >= 0; i--)
        {
            var a = peak * (blur - i + 1) / (blur + 1); // outer rings fainter
            Rect(x + dx - i, y + dy - i, w + 2 * i, h + 2 * i, new Color(0, 0, 0, a));
        }
    }
}
