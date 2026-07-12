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
        var (hiddenCounts, reflowed) = CountWidthLayout(s); // B27: minion columns reflow / collapse
        foreach (var e in s.Elements.OrderBy(x => x.Z))
        {
            if (ReferenceEquals(e, skip)) continue;
            // A collapsed countWidth container and every element parented to it draw nothing (the flat
            // loop visits children independently, so gating the container alone wouldn't hide them).
            if (hiddenCounts.Count > 0 && (hiddenCounts.Contains(e.Id)
                || (e.Parent is { } hp && hiddenCounts.Contains(hp))))
                continue;
            _textOwner = e.Id; // collision detector context (recorded only while _collectText)
            _curScreen = s;    // panel headers confine to the band above their contained children
            // Scene/backdrop art stretches to the FULL design canvas — never its authored anchor/size
            // — so it can't diverge from edge-anchored chrome when the canvas extends past 16:9 (§13).
            // A visible countWidth container (and its children) reflow to the data-driven width (B27).
            Rectangle rect =
                IsSceneElement(e) ? _ui.FullCanvasRect(s)
                : IsFullBarElement(e) ? _ui.FullWidthRect(s, e)
                : reflowed.TryGetValue(e.Id, out var own) ? own
                : e.Parent is { } rp && reflowed.TryGetValue(rp, out var par) ? ReflowChild(par, e)
                : _ui.Rect(s, e);
            DrawManifestElement(e, rect);
        }
        _textOwner = null;
    }

    // The scene layer alone — the baseline the smoke paint-coverage diff measures against. A scene
    // element is identified by its *.scene bind (paint-ordinal z carries no layer semantics).
    private static bool IsSceneElement(Element e) => e.Binds is { } b && b.EndsWith(".scene");

    // Full-bleed chrome bars: authored at the base 960 design width, but meant to span the real
    // viewport edge-to-edge past 16:9 (§13) — same bug class as the scene backdrop, different
    // element set. Scoped by id, not "every Top/Bottom element", since some are deliberately narrower.
    private static readonly HashSet<string> FullBarIds = ["statusStrip", "footer"];
    private static bool IsFullBarElement(Element e) => FullBarIds.Contains(e.Id);

    // CD #30's fixed-tick pulse/glow clock: cosmetic-only wall time, sampled once per Draw(GameTime)
    // in Game1.cs. Never read by Update or Core — purely how far along the current breathe cycle is.
    private double _pulseMs;

    private void DrawManifestBackdrop(string screenId)
    {
        var s = _ui.ScreenDef(screenId);
        if (s is null) return;
        foreach (var e in s.Elements.Where(IsSceneElement))
            DrawManifestElement(e, _ui.FullCanvasRect(s));
    }

    private Roguebane.Core.Layout.Screen? _curScreen; // per draw pass; panel headers see siblings

    private void DrawManifestElement(Element e, Rectangle r)
    {
        // Node-conditional popup (quest): a FLAT cluster of siblings (panel chrome + its
        // kicker/label/note/actions), NOT a parent->child tree, and most members carry no bind of
        // their own — so nothing hid them at a fight node (Doug 2026-07-12: an empty "QUEST" box floated
        // over a live Skirmish, foe at 19/28). It belongs on screen ONLY at its own node type. Gate
        // the whole cluster by id-prefix off the panel's bind; a closed gate draws nothing, chrome
        // included. This is NOT the general panel case: attrPool/actionBar/inventory/supplies/... bind
        // structural markers that resolve null yet must ALWAYS draw, so the gate stays scoped to this
        // cluster by name, never "any panel whose bind resolves null". (Camp used to be the second
        // gated cluster; RETIRED per CD_STATUS #39 — see NodeGateBindFor below.)
        if (NodeGateBindFor(e.Id) is { } gate && ResolveScreenBind(gate) is null)
            return;
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
        // Pattern elements draw their own fill inside the pattern block below, clipped with it.
        if (e.Fill is { } fill
            && e.Binds is not ("enemy.advancePct" or "runes.budgetPct" or "ShieldPool.regenPct")
            && !(e.ImageBind is { Length: > 0 } && !e.ImageBind.Contains('{')))
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

        // §12 pattern imageBind: a STATIC path (no {bind} holes) TILES its PNG across the element
        // rect, over the fill — the doom gauge's hazard stripes riding its blood track. Pattern art
        // ships at the dc.html 2x paint density (same as ChromeBake), so a tile's design footprint
        // is texture-size / ChromeBake.
        if (e.ImageBind is { Length: > 0 } pat && !pat.Contains('{')
            && _assets.Texture(NormalizeContentPath(pat)) is { } ptex)
        {
            var region = r;
            // FLAGGED STOPGAP (Needs-CD): doomFillStripes is the covered-ground PATTERN of the
            // doom gauge, but the extraction dropped the advance bind its sibling fill carries —
            // clip to the same right-to-left covered width (the §war-party tandem rule) so the
            // track doesn't always read fully overrun. Dies when CD binds the stripes element.
            if (InRun && Exp.Map.MarchLength > 0 && _curScreen is { } patScr
                && patScr.Elements.Any(s => s.Binds == "enemy.advancePct" && _ui.Rect(patScr, s).Intersects(r)))
            {
                var frac = (float)Exp.Map.WarPartyDistance / Exp.Map.MarchLength;
                var covered = (int)((1f - frac) * r.Width);
                region = new Rectangle(r.X + r.Width - covered, r.Y, covered, r.Height);
            }
            if (e.Fill is { } pfill) DrawFill(region, pfill); // the fill rides the same clip
            int tw = Math.Max(1, (int)(ptex.Width / ChromeBake)), th = Math.Max(1, (int)(ptex.Height / ChromeBake));
            for (var ty = region.Y; ty < region.Bottom; ty += th)
                for (var tx = region.X; tx < region.Right; tx += tw)
                {
                    int dw = Math.Min(tw, region.Right - tx), dh = Math.Min(th, region.Bottom - ty);
                    _spriteBatch.Draw(ptex, new Rectangle(tx, ty, dw, dh),
                        new Rectangle(0, 0, (int)(dw * ChromeBake), (int)(dh * ChromeBake)), Color.White);
                }
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
                    // Prefer the LIVE per-node scene: resolve the imageBind template (bg/{encounter.scene})
                    // through the screen binds. Fall back to the authored mock image when the bind can't
                    // resolve to a shipped texture (out of run, or an unmapped scene id).
                    if (e.ImageBind is { } sib && sib.Contains('{'))
                    {
                        var live = System.Text.RegularExpressions.Regex.Replace(sib, @"\{(.+?)\}",
                            mm => ResolveScreenBind(mm.Groups[1].Value) ?? "");
                        if (!live.Contains('{') && _assets.Texture(NormalizeContentPath(live)) is not null)
                            bg = live;
                    }
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
                if (e.Binds == "ShieldPool.regenPct")
                {
                    // 07-03 drop: the manifest authors the regen FILL as its own element (the
                    // track's inline draw is retired) — width = live progress across the
                    // containing track's inner width; the authored width is the design sample.
                    if (InRun && Exp.Player.Body.ShieldRegenProgress is > 0f and var prog
                        && e.Fill is { } rf && _curScreen is { } regScr)
                    {
                        var track = regScr.Elements.FirstOrDefault(s => s.Binds == "ShieldPool.regen");
                        var full = track is not null ? _ui.Rect(regScr, track).Width - 2 : r.Width;
                        var fw = (int)(full * prog);
                        if (fw > 0) DrawFill(new Rectangle(r.X, r.Y, fw, r.Height), rf);
                    }
                    break;
                }
                // §12 element parts: the dc.html source keeps value/label spans as separate runs
                // with element-local rects — draw each part, NEVER the element's flattened sample
                // (retires the M1 preview-tile stopgap; the A3 re-extraction authors these).
                if (e.Parts.Length > 0)
                {
                    DrawElementParts(e, r);
                    break;
                }
                var txt = e.Content ?? ResolveScreenBind(e.Binds);
                // Node cleared: the header RETREAT button BECOMES the REDEPLOY button (relabel here +
                // gold skin in DrawStateSkin), replacing the removed standalone "NODE CLEARED" overlay
                // (Doug 2026-07-09). The click is rewired to Redeploy in UpdateRun's Cleared branch.
                if (e.Binds == "combat.retreat" && InRun && Exp.State == ExpeditionState.Cleared)
                    txt = (e.Content ?? txt)?.Replace("RETREAT", "REDEPLOY"); // keep the authored glyph/prefix
                // A bindless text element carrying a static image (doomHost's enemy-host icon) IS
                // that image — blit it. State-skinned elements (buttons) keep their skin machinery.
                if (string.IsNullOrEmpty(txt) && e.Image is { Length: > 0 } img
                    && e.States.ValueKind is System.Text.Json.JsonValueKind.Undefined
                        or System.Text.Json.JsonValueKind.Null)
                {
                    var dest = r;
                    // FLAGGED STOPGAP (Needs-CD): this bindless icon (doomHost) carries no advance
                    // bind of its own — ride the same war-party tandem rule as doomFillStripes,
                    // riding the covered/uncovered boundary of the sibling track that shares the
                    // advancePct fill's right edge. Dies when CD binds the icon's own position.
                    if (InRun && Exp.Map.MarchLength > 0 && _curScreen is { } hostScr
                        && hostScr.Elements.FirstOrDefault(s => s.Binds == "enemy.advancePct") is { } fillSib)
                    {
                        var fillR = _ui.Rect(hostScr, fillSib);
                        var track = hostScr.Elements.FirstOrDefault(s => s.Type == "panel" && s.Binds is null
                            && Math.Abs(_ui.Rect(hostScr, s).Right - fillR.Right) <= 2
                            && _ui.Rect(hostScr, s).Bottom >= r.Y && _ui.Rect(hostScr, s).Y <= r.Bottom);
                        if (track is not null)
                        {
                            var trackR = _ui.Rect(hostScr, track);
                            var frac = (float)Exp.Map.WarPartyDistance / Exp.Map.MarchLength;
                            var cx = trackR.X + (int)(frac * trackR.Width);
                            dest = new Rectangle(cx - r.Width / 2, r.Y, r.Width, r.Height);
                        }
                    }
                    Sprite(_assets.Texture(NormalizeContentPath(img)), dest.X, dest.Y, dest.Width, dest.Height, Color.White);
                    break;
                }
                var skinned = DrawStateSkin(e, r, enabled: !string.IsNullOrEmpty(txt));
                if (skinned && !string.IsNullOrEmpty(txt))
                {
                    // Skinned-button labels are authored centered, mono-bold, ground-dark in the
                    // dc.html source (the extraction flattens the inner spans and mis-attributes
                    // display/ink — logged Needs-CD). Draw per the source, but at THIS button's own
                    // authored fontPx (was hardcoded to 7.0 — autoAttackBtn's value only; every other
                    // skinned button's real fontPx differs (retreatBtn/closeBtn/leaveBtn 6, equipmentBtn
                    // 6.5, beginBtn 7.5), which measurably overflowed the label past the button rect).
                    var btnPx = e.FontPx ?? 7.0;
                    var bsz = MeasureText(_assets.Mono, txt!) * (float)(btnPx / MonoDesignPx);
                    var slx = (int)(r.X + r.Width / 2 - bsz.X / 2);
                    var sly = (int)(r.Y + r.Height / 2 - bsz.Y / 2);
                    RecordTextBox(InkBox(_assets.Mono, txt!, slx, sly, btnPx), r, txt!, _assets.Mono);
                    TextPx(_assets.Mono, txt!, slx, sly, _ui.Color("ground", Color.Black), btnPx);
                    break;
                }
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
                    var blx = (int)(r.X + r.Width / 2 - bsz.X / 2);
                    var bly = (int)(r.Y + r.Height / 2 - bsz.Y / 2);
                    RecordTextBox(InkBox(bfont, blabel, blx, bly, bpx), r, blabel, bfont);
                    TextPx(bfont, blabel, blx, bly, _ui.Color(colTok ?? "ink", Ink), bpx);
                    break;
                }
                DrawButton(e.Content ?? "", r.X, r.Y, r.Width, r.Height, true, Keys.None);
                break;
            case "list" when e.Item is not null:
                DrawManifestList(e, r);
                break;
            case "figure" when e.Binds is "preview.fig" or "Body":
                // Composed figure: feet at the box bottom-centre, scaled to the box height.
                // preview.fig ALWAYS previews the BUILD (newgame's stage must match its own card
                // copy — an in-run smoke was drawing the run's Summoner under Grunt text);
                // Body draws the LIVE run body (part conditions, worn gear) once marching.
                if (e.Binds == "Body" && InRun)
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
                // §8 targeting presentation (design/08, LOCKED; CORRECTED 2026-07-04, Doug): NO box
                // affordances, ever — no hover border, no band highlight, no whole-foe frame. The
                // cursor IS the reticle: it draws AT THE RAW CURSOR and never snaps/warps onto a part.
                // Hovering still runs the part hit-test every frame (FoePartAt below) — that's what
                // click-to-aim locks onto — it just no longer repositions the drawn sprite.
                var picking = !ef.Down && _ctrl.IsTargeting(Exp);
                if (picking && _assets.Reticle("aiming") is { } aim)
                    Sprite(aim, _cursor.X - 24, _cursor.Y - 24, 48, 48, Color.White);
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
                    // Group aims per PART: one reticle per aimed part, its AIM TAG stack = the hotkey
                    // NUMBERS of every module kept on it (design/01: several actives stack). Keyed by the
                    // specific BodyPart (not its Stat) so a paired limb aims land on THAT arm/leg, not the
                    // union-centre of both — which sat on the torso (Doug item 3).
                    var aims = new System.Collections.Generic.Dictionary<Roguebane.Core.BodyPart, List<int>>();
                    for (var ti = 0; ti < Exp.Equipment.Count; ti++)
                        if (Exp.PartOf(Exp.Equipment[ti]) is { } aimed)
                            (aims.TryGetValue(aimed, out var l)
                                ? l : aims[aimed] = new List<int>()).Add(ti);
                    var tagT = _ui.Manifest?.Templates.GetValueOrDefault("aimTag");
                    foreach (var (aimedPart, cardIxs) in aims)
                    {
                        if (FoeAimedPartScreenRect(ef, aimedPart, r) is not { } fr2 || tex is null) continue;
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
    // §12: each element part is one text run at an element-local rect — a literal (content) or a
    // live bind falling back to its sample. Alignment is horizontal within the part band; text
    // centres vertically in it (the authored band IS the line box).
    private void DrawElementParts(Element e, Rectangle r)
    {
        foreach (var p in e.Parts)
        {
            if (p.Rect.Length != 4) continue;
            var pr = new Rectangle(r.X + p.Rect[0], r.Y + p.Rect[1], p.Rect[2], p.Rect[3]);
            var txt = p.Content
                ?? (p.Binds is { Length: > 0 } ? ResolveScreenBind(p.Binds) ?? p.Sample : p.Sample);
            if (string.IsNullOrEmpty(txt)) continue;
            var font = p.Font == "display" ? _assets.Display : _assets.Mono;
            var basePx = font == _assets.Display ? DisplayDesignPx : MonoDesignPx;
            var sz = MeasureText(font, txt!) * (float)(p.FontPx / basePx);
            var x = p.Align switch
            {
                "center" => (int)(pr.X + pr.Width / 2f - sz.X / 2f),
                "right" => (int)(pr.Right - sz.X),
                _ => pr.X,
            };
            var y = (int)(pr.Y + pr.Height / 2f - sz.Y / 2f);
            RecordTextBox(InkBox(font, txt!, x, y, p.FontPx), r, txt!, font);
            TextPx(font, txt!, x, y, _ui.Color(p.Color ?? e.Color ?? "ink", Ink), p.FontPx);
        }
    }

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

    // B27: resolve a countWidth's COUNT bind to a live integer. Both the encounter minionGroup
    // (`loadout.minionCap`) and the equipment minionColumn (`minions.cap`) read the current core's minion
    // capacity — pre-run from the build, in-run from the expedition. Unknown binds resolve 0 (collapses).
    private int ResolveCountBind(string? bind) => bind switch
    {
        "loadout.minionCap" or "minions.cap" => InRun ? Exp.MinionCap : _build.CoreRune.MinionCap,
        _ => 0,
    };

    // B27 per-draw resolution of every countWidth container on the screen. `hidden` = collapsed at
    // count 0 (the container AND its parented children draw nothing). `reflowed` = each VISIBLE
    // container's data-driven rect; children parented to it reflow against THAT rect (the flat draw
    // loop resolves parents from authored size, so a child would otherwise sit at the authored 2-slot
    // width — its content spilling past the reflowed border at any other count).
    private (HashSet<string> hidden, Dictionary<string, Rectangle> reflowed) CountWidthLayout(
        Roguebane.Core.Layout.Screen s)
    {
        HashSet<string>? hidden = null;
        Dictionary<string, Rectangle>? reflowed = null;
        foreach (var e in s.Elements)
            if (e.CountWidth is { } cw)
            {
                var count = ResolveCountBind(cw.Bind);
                if (!cw.VisibleAt(count)) (hidden ??= new()).Add(e.Id);
                else (reflowed ??= new())[e.Id] = _ui.RectWithWidth(s, e, cw.WidthFor(count));
            }
        return (hidden ?? EmptyIds, reflowed ?? EmptyRects);
    }
    private static readonly HashSet<string> EmptyIds = new();
    private static readonly Dictionary<string, Rectangle> EmptyRects = new();

    // A child of a reflowed countWidth container, placed against the container's LIVE rect. The minion-
    // column children are TopLeft-anchored with an [offsetX,offsetY] into the parent and a zero right-
    // inset (offsetX + childW == authored parentW), so the child spans [offsetX .. parentW]; the reflow
    // keeps that span, shrinking/growing width with the box while holding the left offset and height.
    private static Rectangle ReflowChild(Rectangle parent, Element child)
    {
        int ox = child.Offset.Length > 0 ? child.Offset[0] : 0;
        int oy = child.Offset.Length > 1 ? child.Offset[1] : 0;
        int h = child.Size.Length > 1 ? child.Size[1] : 0;
        return new Rectangle(parent.X + ox, parent.Y + oy, Math.Max(0, parent.Width - ox), h);
    }

    // Maps a quest cluster member (by id) to the screen-bind that gates its whole cluster, or null
    // when the element is not part of a node-conditional popup. "quest" is unique to the encounter
    // cluster. (Camp was RETIRED from the manifest, CD_STATUS #39: it no longer floats an element —
    // it resolves purely via encounter.scene=enc_camp backdrop + a foeless action bar, so the old
    // "campMarker" gate branch is gone.)
    private static string? NodeGateBindFor(string id) =>
        id.StartsWith("quest", StringComparison.Ordinal) ? "encounter.quest"
        : null;

    // Screen-level (non-list) binds -> display text: the NewGame Loadout preview reads the BuildSession.
    private string? ResolveScreenBind(string? bind) => bind switch
    {
        "preview.name" => _build.Race.Name + " " + _build.CoreRune.Title,
        "preview.role" => _build.CoreRune.Archetype,
        "preview.hp" => _build.Race.Hp.ToString(),
        "preview.budget" => _build.CoreRune.RuneBudget.ToString(),
        // Labeled "ACTIONS" in the manifest (design/NewGame.dc.html previewActionsTile) — the action
        // bar's real capacity, not the starting kit size (2026-07-06 loop: was Kit.Count, undersized
        // on the 5/7 cores where a rune-granted technique has room the kit alone doesn't show).
        "preview.techniques" => _build.CoreRune.ActionSlots.ToString(),
        "preview.minionCap" => _build.CoreRune.MinionCap.ToString(),
        "preview.coreEffectName" => _build.CoreRune.CoreEffectName,
        // *.coreEffect is the BLOCK container (border chrome; label/name/desc are their own
        // elements/parts) — resolving it to the desc painted the copy TWICE (the doubled-text P0).
        "preview.coreEffectDesc" => _build.CoreRune.CoreEffectDesc,
        // NewGame core-roster pager (design/NewGame, 7 cores/3 per page -- CD's own NewGame.dc.html
        // reference JS uses the same PER=3, matching GridCapacity's read of the coreCards geometry).
        "cores.pageLabel" => "PAGE " + (CorePager.Index(_build.Roster.Count) + 1)
            + " / " + CorePager.PageCount(_build.Roster.Count),
        "cores.pagePrev" => CorePager.HasPrev(_build.Roster.Count) ? "<" : null,
        "cores.pageNext" => CorePager.HasNext(_build.Roster.Count) ? ">" : null,
        // "core" = the equipment identity BLOCK (chrome only) — currentCoreName/Role carry the text;
        // resolving it printed the identity a SECOND time as a panel header (the doubled-name bug).
        // Equipment identity block (design/02): the core is fixed for the run, so the build's core
        // is the live source pre-run AND mid-run. core.coreEffect (the block) stays chrome-only.
        "core.name" => _build.Race.Name + " " + _build.CoreRune.Title,
        "core.role" => _build.CoreRune.Archetype,
        // The topbar chip (B0b landed: CD split it off core.name) — "CORE GRUNT" per the source.
        "core.label" => "CORE " + _build.CoreRune.Title.ToUpperInvariant(),
        // Equipment strip labels (design/02). run.state reads the real expedition state in-run —
        // pre-run AND at the chart (Choosing) the march is armed, matching the design's
        // READY TO MARCH copy; a live fight reads FIGHTING.
        "run.state" => !InRun || Exp.State == Roguebane.Core.ExpeditionState.Choosing
            ? "READY TO MARCH" : Exp.State.ToString().ToUpperInvariant(),
        // Denominator is the action bar's real capacity (ActionSlots), not the starting kit size —
        // same undersizing bug as preview.techniques above (2026-07-06 loop).
        "loadout.slotLabel" => "TECHNIQUES - "
            + (InRun ? Exp.Equipment.Count : _build.Equipment.Count) + " / " + _build.CoreRune.ActionSlots + " slotted",
        "minions.slotLabel" => "MINIONS - "
            + (InRun ? Exp.Minions.Count : _build.CoreRune.MinionKit.Count) + " / " + _build.CoreRune.MinionCap + " slotted",
        // Equipment inventory pager (design/02): pages whichever tab is active, sized live off the
        // invItems grid geometry (GridCapacity -- its authored "cols":2 hint is a 1px-short fit today).
        "inventory.activeTab.pageLabel" => InventoryTabItems() is { } ipItems
            ? "PAGE " + (InvPager.Index(ipItems.Count) + 1) + " / " + InvPager.PageCount(ipItems.Count)
            : null,
        "inventory.activeTab.pagePrev" => InventoryTabItems() is { } ipPrev && InvPager.HasPrev(ipPrev.Count) ? "<" : null,
        "inventory.activeTab.pageNext" => InventoryTabItems() is { } ipNext && InvPager.HasNext(ipNext.Count) ? ">" : null,
        "core.coreEffectName" => _build.CoreRune.CoreEffectName,
        "core.coreEffectDesc" => _build.CoreRune.CoreEffectDesc,
        // The authored copy is "BUDGET n free / m" (design/02's rune bag readout).
        "runes.budget" => "BUDGET " + _build.Runes.Available + " free / " + _build.Runes.Budget,
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
        // §6b shield bar: the 07-03 drop authors the count as its OWN element — the header keeps
        // only the label, the count element carries the live n / m.
        "ShieldPool" => InRun && Exp.Player.Body.ShieldLayers > 0 ? "SHIELD" : null,
        "ShieldPool.count" => InRun && Exp.Player.Body.ShieldLayers > 0
            ? Exp.Player.Body.ShieldPoints + " / " + Exp.Player.Body.ShieldLayers : null,
        // Merchant screen (design/07): header/footer readouts. The pager label waits on the wares slice.
        "merchant.label" => "MERCHANT",
        "merchant.leave" => InRun && Exp.AtMerchant ? "LEAVE" : null,
        "run.gold" => InRun ? "PURSE " + Exp.Gold + "g" : null,
        "merchant.stock.pageLabel" => InRun && Exp.AtMerchant
            ? "PAGE " + (_merchantPager.Index(MerchantSections().Count) + 1) + " / "
                + _merchantPager.PageCount(MerchantSections().Count) : null,
        "merchant.stock.pagePrev" => InRun && Exp.AtMerchant
            && _merchantPager.HasPrev(MerchantSections().Count) ? "<" : null,
        "merchant.stock.pageNext" => InRun && Exp.AtMerchant
            && _merchantPager.HasNext(MerchantSections().Count) ? ">" : null,
        "combat.paused" => _paused ? "HELD" : null, // badge shows only while the fight is held
        // Navigation gates (07-03 drop): the bound datum is "this affordance applies here" — the
        // literal labels live in the manifest content (bind-gate semantics, LAYOUT_CONTRACT §12).
        "nav.close" => "CLOSE",           // Equipment's close is always available on that screen
        "nav.equipment" => "EQUIPMENT",   // the citymap Equipment button likewise
        "begin" => "BEGIN",               // NewGame's begin CTA
        // The chart's current-position label (design/03: "YOU ARE HERE" under the ringed node; the
        // dc.html glyph U+25BC isn't in the font regions — the ring itself marks the node).
        "map.current" => InRun ? "YOU ARE HERE" : null,
        // CityMap gauges (07-03 drop): titles/counts/notes are REAL elements now — the panel binds
        // ("supplies"/"support") are container-only and resolve nothing (the one-text-run stopgap
        // header is retired); the count elements carry the live n / m.
        "supplies.count" => InRun ? $"{Exp.Map.Supplies} / {Exp.Map.MaxSupplies}" : null,
        "support.count" => InRun
            ? $"{Exp.Map.SupportBank} / {Exp.Map.Nodes.Count(n => n.Type == Roguebane.Core.NodeType.ResourceHold)}" : null,
        "enemy.advance" => InRun
            ? Exp.Map.WarPartyDistance + (Exp.Map.WarPartyDistance == 1 ? " WAYPOINT" : " WAYPOINTS")
              + " AWAY FROM CAMP" : null,
        // Scene descriptor: the node type is live data; locale place names ("the high pass") have no
        // model yet (design-open §17) — the type alone renders, nothing is invented.
        "encounter.label" => InRun ? NodeLabel(Exp.Map.Current.Type) : null,
        // Node-conditional popup gate (Doug 2026-07-12): the quest cluster draws ONLY at a Quest node.
        // Resolve non-null (truthy) there, null everywhere else — at a fight node the gate closes and
        // NodeGateBindFor hides the whole cluster, killing the empty-box overprint. (Camp had a twin
        // encounter.camp gate; RETIRED per CD_STATUS #39 — camp now renders via encounter.scene backdrop.)
        "encounter.quest" => InRun && Exp.Map.Current.Type == Roguebane.Core.NodeType.Quest ? "quest" : null,
        // Per-node backdrop scene (CD_STATUS #41): Core owns the scene id off the live node; the
        // backdrop element's imageBind bg/{encounter.scene} resolves through here. Null out of run so
        // the element falls back to its authored mock image.
        "encounter.scene" => InRun ? Exp.Map.Current.Scene : null,
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
        if (b is "ShieldPool.regen" or "ShieldPool.regenPct")
            return InRun && Exp.Player.Body.ShieldRegenProgress > 0f;
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
            "cores" => LocalCoreSelIx(), // page-local -- "cores" data is sliced to the current page
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
            if (tmpl.States.ValueKind == System.Text.Json.JsonValueKind.Object)
            {
                if (tmpl.States.TryGetProperty("family", out var famEl))
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
                        // invCard/loadoutCard (§6e, B5/B6 landed): family is now a literal template
                        // name, not a shared shape like pickerCard/actionCard — route both to the
                        // same equipped/disabled/equippable/locked resolver.
                        "invCard" or "loadoutCard" => InvCardState(datum),
                        _ => null,
                    };
                // §6e interim (FLAGGED, dies when B5 family keys land): states WITHOUT a family
                // key resolve GENERICALLY from what they author — never per-template one-offs.
                // "slotted"/"empty" reads datum presence (the loadout bar); the four-state
                // inventory vocabulary reads the gear/technique/minion state.
                else if (tmpl.States.TryGetProperty("slotted", out _))
                    rootState = datum is not null ? "slotted" : "empty";
                else if (tmpl.States.TryGetProperty("equipped", out _))
                    rootState = InvCardState(datum);
            }
            DrawTemplateRootChrome(tmpl, cell, datum, rootState);
            // FLAGGED STOPGAP (§6e: hover treatment is CD-authored; equipment authors none today —
            // B6 asks for it): a generic brighten ring on the hovered inventory card meanwhile.
            if (e.Binds == "inventory.activeTab.items"
                && Hover(new Rectangle(cell.X, cell.Y, cell.W, cell.H)))
                Border(cell.X, cell.Y, cell.W, cell.H, _ui.Color("ink", Ink) * 0.35f, BorderPx(1), null);
            // §6e drag feedback (PLACEHOLDER visual, Needs Claude Design for real chrome): the card
            // being dragged reads as a dimmed "ghost" left behind in its vacated slot; the slot nearest
            // the cursor draws an insertion ring — the two cues a mid-drag bar needs to read at all.
            if (e.Binds == "loadout" && _dragging && datum is not null)
            {
                if (Equals(datum, _dragTech))
                    Rect(cell.X, cell.Y, cell.W, cell.H, Color.Black * 0.5f);
                else if (WithinBar(cells) && i == DragInsertionIndex(cells))
                    Border(cell.X, cell.Y, cell.W, cell.H, _ui.Color("accent", Color.Gold), BorderPx(2), null);
            }
            // Positional binds repeat the SAME bind N times per card (attr tiles 4x, attr-bar pips 12x,
            // in template order); count each occurrence per card to pick the right datum slice.
            int valIx = 0, keyIx = 0, pipIx = 0;
            var occ = new System.Collections.Generic.Dictionary<string, int>(); // per-bind occurrence (rune rows)
            foreach (var pp in CardTemplate.Place(tmpl, cell.X, cell.Y))
            {
                if (pp.Binds is { } sel && sel.EndsWith(".selection")
                    && pp.States.ValueKind == System.Text.Json.JsonValueKind.Object)
                {
                    // Per-state chip labels (07-03 drop, A2 landed — the M1 stopgap is retired):
                    // the selection part restyles itself AND carries its own label per state; the
                    // chosen/selected key follows the picked index. (The data also authors a
                    // LOCKED state; no lock model exists yet, so it never resolves — §17-adjacent.)
                    var key = i == selIx
                        ? (pp.States.TryGetProperty("chosen", out _) ? "chosen" : "selected")
                        : "idle";
                    if (pp.States.TryGetProperty(key, out var chip)
                        && chip.ValueKind == System.Text.Json.JsonValueKind.Object)
                    {
                        string? St(string k) => chip.TryGetProperty(k, out var v)
                            && v.ValueKind == System.Text.Json.JsonValueKind.String ? v.GetString() : null;
                        // The state style REPLACES the part style wholesale — the part's own
                        // fill/border captured the CHOSEN sample; an idle chip that "inherits"
                        // them paints the chosen look on every card.
                        if (St("fill") is { } sf) DrawFill(RectOf(pp.Rect), new Fill { Token = sf });
                        var bcol = St("border");
                        if (bcol is { Length: > 0 })
                            Border(pp.Rect.X, pp.Rect.Y, pp.Rect.W, pp.Rect.H,
                                _ui.Color(bcol, Border0), BorderPx(pp.Border?.W ?? 1), pp.Border?.Sides);
                        var label = St("label") ?? pp.Sample;
                        if (!string.IsNullOrEmpty(label))
                        {
                            var op = chip.TryGetProperty("opacity", out var ov)
                                && ov.ValueKind == System.Text.Json.JsonValueKind.Number
                                ? (float)ov.GetDouble() : 1f;
                            var cfont = pp.Font == "display" ? _assets.Display : _assets.Mono;
                            var cbase = cfont == _assets.Display ? DisplayDesignPx : MonoDesignPx;
                            var lsz = MeasureText(cfont, label!) * (float)(pp.FontPx / cbase);
                            var lx = (int)(pp.Rect.X + pp.Rect.W / 2 - lsz.X / 2);
                            var ly = (int)(pp.Rect.Y + pp.Rect.H / 2 - lsz.Y / 2);
                            RecordTextBox(InkBox(cfont, label!, lx, ly, pp.FontPx),
                                RectOf(pp.Rect), label!, cfont);
                            TextPx(cfont, label!, lx, ly,
                                _ui.Color(St("color") ?? pp.Color ?? "ink", Ink) * op, pp.FontPx);
                        }
                        continue;
                    }
                }
                // Merchant rows: an UNBOUND part is design-mock filler (the sample price digits) —
                // never draw it beside live data. A NESTED wares region stamps its own cards.
                if (datum is MerchantOffer or MerchantLot or ResourceReadout or MerchantSection
                    && pp.Binds is null) continue;
                // Technique/minion cards likewise: an UNBOUND sample part is design-mock filler
                // (the loose damage/cost digits) — never stamp it over live card copy (P0-C.9).
                if (datum is Roguebane.Core.Technique or Roguebane.Core.Minion
                    && pp.Binds is null && !string.IsNullOrEmpty(pp.Sample)) continue;
                // Race cards: the same P0-C.9 rule for an unbound STATIC-IMAGE part — raceCard
                // ships a leftover human_grunt head mock under the live race.headImage imageBind,
                // and both drew (Doug's ghosted double-head). Mock filler never draws on live data.
                if (datum is Roguebane.Core.Race && pp.Binds is null && pp.ImageBind is null
                    && !string.IsNullOrEmpty(pp.Image)) continue;
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
                    && datum is AttrRow rowD)
                {
                    if (_ui.Manifest is { } mN && mN.Templates.TryGetValue(nested.Template, out var pipT))
                    {
                        var cellsData = PoolCells(rowD);
                        // Stretch the pips to fill this bar's width (Doug #9): each stat's bar maxes its
                        // own region instead of all bars sharing a fixed pip size, so a 5-pip bar reads
                        // as long as a 7-pip one. For a 6-pip bar this computes ~the authored pip width.
                        var pipH = pipT.Size.Length > 1 ? pipT.Size[1] : pp.Rect.H;
                        var pipCells = ListLayout.StretchCells(pp.Rect, cellsData.Count, nested.Gap, pipH);
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
                            DrawTemplateRootChrome(wc, new LayoutRect(wx, pp.Rect.Y, wc.Size[0], wc.Size[1]), ws.Wares[wi]);
                            foreach (var wp in CardTemplate.Place(wc, wx, pp.Rect.Y))
                                DrawWarePart(wp, ws.Wares[wi]);
                        }
                    continue;
                }
                // FSM state parts resolve from the LIVE run — an idle card shows no chip/label at all
                // (never the sample), so resolve BEFORE chrome and bail when there's nothing to say.
                string? stateText = null;
                var isStatePart = pp.Binds is "technique.state" or "minion.state" or "technique.cooldownLabel";
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
                    // attrs.pip picks per PIP INDEX, 4-way (§7 4-ZONE ENCODING): gear-reserved and
                    // technique-reserved -> slot (this legacy color-fill path predates the textured
                    // pip art and can't tell hash textures apart), free -> the attr's token, damaged
                    // (lost, healable) -> "damage", beyond cap+damaged -> nothing. Dead in current
                    // layout.json (attrs.cells/PoolCells is the live sprite path) but kept in shape.
                    // colorBind (manifest-declared) wins; the older bind-specific tinting stays as the
                    // fallback for manifests that predate it.
                    string? fillTok = ResolveColorToken(pp.ColorBind, datum);
                    var skipFill = false;
                    if (fillTok is not null) { }
                    else if (pp.Binds == "attr.color" && datum is not null)
                        fillTok = ResolveBind(datum, pp.Binds);
                    else if (pp.Binds == "attrs.pip" && datum is AttrRow ab)
                    {
                        var p = pipIx++;
                        var reservedEnd = ab.GearReserved + ab.TechReserved;
                        fillTok = p < reservedEnd ? "slot"
                            : p < ab.Capacity ? ab.Token
                            : p < ab.Capacity + ab.Damaged ? "damage"
                            : null;
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
                    if (_assets.Texture(NormalizeContentPath(img!)) is { } itex)
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
                    "minion.hotkey" => (TechniqueCount() + i + 1).ToString(),
                    _ => datum is not null ? ResolveBind(datum, pp.Binds) : null,
                } ?? pp.Sample;
                if (string.IsNullOrEmpty(text)) continue;
                // The core.coreEffect BLOCK's flattened sample IS the source's eyebrow ("CORE
                // EFFECT": mono 9px source = 4.5 design px, mutedDim) — extraction attributed the
                // block's display/8px style to it, overlapping the name line 8px below (Doug's
                // eyebrow x title collision). Draw per the dc.html source; mis-attribution Needs-CD.
                if (pp.Binds == "core.coreEffect")
                {
                    var esz = MeasureText(_assets.Mono, text!) * (float)(4.5 / MonoDesignPx);
                    RecordTextBox(InkBox(_assets.Mono, text!, pp.Rect.X + 5, pp.Rect.Y + 1, 4.5),
                        RectOf(pp.Rect), text!, _assets.Mono);
                    TextPx(_assets.Mono, text!, pp.Rect.X + 5, pp.Rect.Y + 1,
                        _ui.Color("mutedDim", Muted), 4.5);
                    continue;
                }
                var tfont = pp.Font == "display" ? _assets.Display : _assets.Mono;
                // Honor authored align (center/right) for a single-line label that fits its band —
                // matches DrawElementParts' screen-side treatment (NewGame race attr tiles, Doug #4).
                // A run too wide to fit falls through to the wrapped left draw so nothing clips.
                if (pp.Align is "center" or "right")
                {
                    var abase = tfont == _assets.Display ? DisplayDesignPx : MonoDesignPx;
                    var asz = MeasureText(tfont, text!) * (float)(pp.FontPx / abase);
                    if (asz.X <= pp.Rect.W)
                    {
                        var ax = pp.Align == "center"
                            ? (int)(pp.Rect.X + pp.Rect.W / 2f - asz.X / 2f)
                            : (int)(pp.Rect.X + pp.Rect.W - asz.X);
                        var ay = (int)(pp.Rect.Y + pp.Rect.H / 2f - asz.Y / 2f);
                        RecordTextBox(InkBox(tfont, text!, ax, ay, pp.FontPx), RectOf(pp.Rect), text!, tfont);
                        TextPx(tfont, text!, ax, ay, _ui.Color(pp.Color ?? "ink", Ink), pp.FontPx);
                        continue;
                    }
                }
                TextPxWrapped(tfont, text!, RectOf(pp.Rect), _ui.Color(pp.Color ?? "ink", Ink), pp.FontPx);
            }
        }
    }

    // The invCard/loadoutCard root-chrome state per §6e's LOCKED vocabulary (B5/B6 landed —
    // invCard/loadoutCard's manifest states carry these four keys directly, no rename left to chase):
    //   equipped = EQUIPPED (green, active) · disabled = DISABLED (red: assigned but unsustainable)
    //   equippable = EQUIPPABLE (plain, requirements met) · locked = LOCKED (dim: reqs unmet, or the
    //   bar/bays are FULL — capacity reads as locked, never a displacement conflict).
    // Only gates that EXIST in Core apply: weapon = free hand + stat capacity >= Reserve; worn
    // armor disables when its part-group breaks; §6c armor attr-requirements and §6d arm-broken
    // weapon lockout join when those models build (queued — not invented here).
    private string? InvCardState(object? datum) => datum switch
    {
        Roguebane.Core.Weapon w when InRun && Exp.Player.Body.Ranged == w =>
            Exp.Player.Body.RangedGearOnlyUsable ? "equipped" : "disabled",
        Roguebane.Core.Weapon w when InRun && Exp.Player.Body.Hands.Contains(w) =>
            Exp.Player.Body.HandItemGearOnlyUsable(
                Exp.Player.Body.Hands.ToList().IndexOf(w)) ? "equipped" : "disabled",
        Roguebane.Core.Weapon w when InRun =>
            Exp.Player.Body.Hands.Count < 2 && Exp.Player.Body.Capacity(w.Stat) >= w.Reserve
                ? "equippable" : "locked",
        Roguebane.Core.Weapon => "locked", // pre-run: no body to lift it yet
        Roguebane.Core.Armor a when InRun && Exp.Player.Body.ArmorOn(a.Slot) == a =>
            Exp.Player.Body.ArmorGearOnlySustained(a) ? "equipped" : "disabled",
        Roguebane.Core.Armor ar2 => InRun
            && Exp.Player.Body.Capacity(ar2.Governing) >= ar2.Requirement ? "equippable" : "locked",
        Roguebane.Core.Technique t when (InRun ? Exp.Equipment : _build.Equipment).Contains(t)
            => "equipped",
        // Threshold is ActionSlots (real action-bar capacity), not Kit.Count (starting kit size) — a
        // rune-granted 4th technique has room on the 5/7 cores where ActionSlots > Kit.Count
        // (2026-07-06 loop: card previously read "locked" one slot early).
        Roguebane.Core.Technique => (InRun ? Exp.Equipment.Count : _build.Equipment.Count)
            >= _build.CoreRune.ActionSlots ? "locked" : "equippable",
        Roguebane.Core.Minion m when (InRun
            ? Exp.Minions.Contains(m) : _build.CoreRune.MinionKit.Contains(m)) => "equipped",
        Roguebane.Core.Minion => (InRun ? Exp.Minions.Count : _build.CoreRune.MinionKit.Count)
            >= _build.CoreRune.MinionCap ? "locked" : "equippable",
        _ => null,
    };

    // The GEAR tab's rows, ONE composition shared by the renderer's list bind and the click
    // hit-test (a divergence would mis-route clicks). Base order is the fixed identity/acquisition
    // order from Stash's roster (Stash.cs) — NOT built by concatenating wherever each piece currently
    // lives (Body vs pack), because equipping/unequipping MOVES a piece between those, which
    // reshuffled every other piece's screen-slot index too and mis-routed clicks (HIGH PRIORITY
    // bug #1). On top of that stable base, equipped items sort to the front (Doug's ask) via a
    // STABLE OrderByDescending on the same live EQUIPPED read `InvCardState` already uses for the
    // card badge — so equip/unequip still visibly moves an item to/from the top cluster (the point
    // of the ask), but within the equipped and unequipped halves the roster's acquisition order is
    // preserved, keeping click routing exactly as stable as bug #1's fix made it.
    private List<object> GearTabItems() => InRun
        ? Exp.Stash.WeaponRoster.Cast<object>().Concat(Exp.Stash.ArmorRoster)
            .OrderByDescending(item => InvCardState(item) == "equipped")
            .ToList()
        : new List<object>();

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
        "cores" => _build.Roster.Skip(CorePager.Skip(_build.Roster.Count)).Take(CorePageSize)
            .Cast<object>().ToList(),
        "preview.attrs" => PreviewAttrs(),
        "attrs" => AttrBars(),
        "loadout" => (InRun ? Exp.Equipment : _build.Equipment).Cast<object>().ToList(),
        "minions" => InRun ? Exp.Minions.Cast<object>().ToList()
                           : _build.CoreRune.MinionKit.Concat(_build.Runes.GrantedMinions).Cast<object>().ToList(),
        // Inventory follows the tab strip: GEAR = the run's wielded/worn/packed pieces (empty pre-run
        // — gear only exists once marching), TECHNIQUES = the palette, MINIONS = the retinue.
        // (CD's 07-02 drop renamed the bind invItems -> inventory.activeTab.items; chased clean.)
        "inventory.tabs" => new List<object> { "GEAR", "TECHNIQUES", "MINIONS" },
        // §12: bought techniques/minions join the pool the Equipment screen slots from.
        "inventory.activeTab.items" => InventoryTabItems() is { } tabItems
            ? tabItems.Skip(InvPager.Skip(tabItems.Count)).Take(InvPageSize).ToList()
            : null,
        // The Rune Bag (design/02): one group per PATH ladder — the MARKS/PATHS/KEYSTONES taxonomy
        // is OPEN (§17), so the model's actual grouping (ladders) is what renders.
        "runeGroups" => _build.Paths.Cast<object>().ToList(),
        // Equipment identity block (design/02): the core's headline numbers as label/value rows —
        // all live core/build data, no invented figures. Order matches the 2x2 grid in the 02 refs
        // (budget/actions top row, bays/base hp bottom row); "base hp" was missing entirely until
        // CHUNK C item 3 (2026-07-06, loop) — it's the RACE's flat Hp (pre-CON-scaling), the same
        // field Fighter.Scaled's _base reads, never the live/CON-scaled MaxHp.
        "core.stats" => new List<object>
        {
            ("budget", _build.CoreRune.RuneBudget.ToString()),
            ("actions", _build.CoreRune.ActionSlots.ToString()), // real capacity, not Kit.Count (2026-07-06 loop)
            ("bays", _build.CoreRune.MinionCap.ToString()),
            ("base hp", _build.Race.Hp.ToString()),
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
        "loadout.minions" => InRun ? Exp.Minions.Cast<object>().ToList()
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
            ? MerchantSections().Skip(_merchantPager.Skip(MerchantSections().Count)).Take(SectionsPerPage)
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

    // One attr row's pip cells (§7 "ATTRIBUTE PIP BAR — 4-ZONE ENCODING", LOCKED 2026-07-05): bar is
    // authored to MAX (undamaged) capacity, so Capacity+Damaged cells total, left to right:
    // 1. gear-reserved (armor/weapons, permanent while equipped) -> hashed pip_reserved
    // 2. technique-reserved (active/charging, frees on deactivate) -> solid non-hashed pip_empty
    // 3. free (whatever's left, unreserved) -> the stat-tinted solid pip_full
    // 4. damaged (capacity lost to injury, gone until healed) -> hashed pip_damage, different tone
    // All 4 looks already exist on disk (confirmed by direct view) -- pure wiring, no new art.
    private static List<object> PoolCells(AttrRow row)
    {
        var gearEnd = row.GearReserved;
        var techEnd = gearEnd + row.TechReserved;
        var freeEnd = row.Capacity;
        // §7 4-ZONE ENCODING [LOCKED 2026-07-05]: zone 1 gear/weapon = HASHED (pip_reserved), zone 2
        // active-technique reservation = REGULAR SOLID (pip_full), zone 3 free = EMPTY, zone 4 damage
        // = HASHED. Filled reads as "reserved/in use," empty as "free" (Doug bug #12); zones 2 and 3
        // were previously swapped, so activating a technique EMPTIED a pip instead of filling one.
        return Enumerable.Range(0, row.Capacity + row.Damaged).Select(i => (object)new PoolCell(
            i < gearEnd ? "gear" : i < techEnd ? "tech" : i < freeEnd ? "free" : "damaged",
            i < gearEnd ? "pip_reserved_" + row.Token
                : i < techEnd ? "pip_full_" + row.Token
                : i < freeEnd ? "pip_empty"
                : "pip_damage")).ToList();
    }

    // A campaign leg on the spine strip: taken / current / future (spineCity state key).
    private sealed record CityLeg(string Status);
    // One castle fortification row: the structured boss's part + its live condition.
    private sealed record FortPart(string Name, string State);

    // A textured gauge/strip pip: live/spent + which ui/pip PNG renders it (imageBind).
    private sealed record PipPoint(bool Live, string Asset);
    // One cell of a pool/attr pip strip: gear/tech/free/damaged + its ui/pip PNG.
    private sealed record PoolCell(string State, string Asset);

    // One attribute pip-bar row (§7 4-ZONE ENCODING): key, part label, the 2 reservation zone sizes,
    // current (post-damage) capacity, capacity lost to damage, and the pip colour token.
    private sealed record AttrRow(string Key, string Part, int GearReserved, int TechReserved, int Capacity, int Damaged, string Token);

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
    private readonly Pager _merchantPager = new(SectionsPerPage);

    // NewGame's coreCards / Equipment's invItems are grid lists whose page size is real geometry
    // (476x404 seats 3 coreCards; invItems' authored "cols":2 doesn't quite fit its 403px region --
    // see ListLayoutTests.GridCapacityHonestlyReportsAOnePixelColumnShortfall), so their page size is
    // derived live via GridCapacity rather than a hand-picked constant. Lazy: the manifest isn't
    // loaded yet when these fields would otherwise initialize (LoadContent runs after field init).
    private int? _corePageSize;
    private int CorePageSize => _corePageSize ??= Math.Max(1, ManifestGridCapacity("newgame", "cores"));
    private Pager? _corePagerBacking;
    private Pager CorePager => _corePagerBacking ??= new Pager(CorePageSize);

    private int? _invPageSize;
    private int InvPageSize => _invPageSize ??= Math.Max(1, ManifestGridCapacity("equipment", "inventory.activeTab.items"));
    private Pager? _invPagerBacking;
    private Pager InvPager => _invPagerBacking ??= new Pager(InvPageSize);

    // The active inventory tab's full (unpaginated) item list -- shared by ListData's slicing, the
    // page-label text binds, and Game1.cs's three tab-branch click/drag handlers so all three agree
    // on exactly what "item N of the current tab" means.
    private List<object>? InventoryTabItems() => _invTab switch
    {
        0 => GearTabItems(),
        1 => _build.Palette.Cast<object>()
            .Concat(InRun ? Exp.Stash.Techniques : Enumerable.Empty<object>().Cast<Roguebane.Core.Technique>()).ToList(),
        2 => _build.CoreRune.MinionKit.Concat(_build.Runes.GrantedMinions).Cast<object>()
            .Concat(InRun ? Exp.Stash.Minions : Enumerable.Empty<Roguebane.Core.Minion>()).ToList(),
        _ => null,
    };

    // Page-relative index of the currently-selected core, for the "pickerCard" selection ring --
    // ListData("cores") is sliced to the current page, so the ring must compare against a page-local
    // index, not the roster-global CoreRuneIndex (-1 hides the ring when the selection is off-page).
    private int LocalCoreSelIx()
    {
        var local = _build.CoreRuneIndex - CorePager.Skip(_build.Roster.Count);
        return local >= 0 && local < CorePageSize ? local : -1;
    }

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
        Add("WEAPONS", Exp.OfferedWeapons.Select(w => new Ware("WPN", w.Name is { Length: > 0 } ? w.Name : DisplayName(w.Id),
            w.Stat.ToString().ToUpperInvariant() + " " + w.Reserve, "",
            Roguebane.Core.Expedition.Price(w) + "g", "BUY", w)));
        Add("ARMOR", Exp.OfferedArmor.Select(a => new Ware("ARM", a.Name,
            a.Line.ToString().ToUpperInvariant() + " T" + a.Tier, "",
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

    // Ware-card hit-test sharing the nested-stamping geometry: page sections in their manifest list
    // cells, cards flowing horizontally through each section's wares region. Template ids are needed
    // for GEOMETRY here (guarded — a CD rename degrades to no shelves, never a crash).
    private IEnumerable<(object Item, Rectangle Rect)> WareRects()
    {
        if (_ui.Manifest is not { } m || !m.Templates.TryGetValue("wareCard", out var wc)
            || !m.Templates.TryGetValue("shopSection", out var sect)) yield break;
        var waresPart = sect.Parts.FirstOrDefault(p => p.Binds == "section.wares");
        if (waresPart is null) yield break;
        var allSections = MerchantSections();
        var sections = allSections.Skip(_merchantPager.Skip(allSections.Count))
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

    // The attribute bars/pool rows: one AttrRow per stat (§7 4-ZONE ENCODING). In a run the LIVE body
    // supplies them (actives reserve, gear reserves, damage shrinks caps); pre-run it's the build
    // preview, where nothing is reserved or damaged.
    private System.Collections.Generic.IReadOnlyList<object> AttrBars()
    {
        var b = InRun ? Exp.Player.Body : _build.Preview();
        return new object[]
        {
            new AttrRow("STR", "Arms", b.GearReserved(Stat.Str), b.TechReserved(Stat.Str), b.Capacity(Stat.Str), b.Damaged(Stat.Str), "str"),
            new AttrRow("INT", "Head", b.GearReserved(Stat.Int), b.TechReserved(Stat.Int), b.Capacity(Stat.Int), b.Damaged(Stat.Int), "int"),
            new AttrRow("DEX", "Legs", b.GearReserved(Stat.Dex), b.TechReserved(Stat.Dex), b.Capacity(Stat.Dex), b.Damaged(Stat.Dex), "dex"),
            new AttrRow("CON", "Chest", b.GearReserved(Stat.Con), b.TechReserved(Stat.Con), b.Capacity(Stat.Con), b.Damaged(Stat.Con), "con"),
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
        // No early bail on "root has neither fill nor border" — coreCard (and any other family-driven
        // template) carries its chrome ONLY inside states.idle/selected/etc, nothing at the template
        // root. Bailing here skipped the states lookup below entirely, so pickerCard's amber selected
        // ring never drew for ANY state, not just selected (Doug's HiFi report, NewGame core-picker).
        var fillTok = t.Fill?.Token;
        var borderTok = t.Border?.Color;
        var key = stateKey ?? (datum is not null && t.Binds is { } b ? ResolveBind(datum, b) : null);
        // CD #30 (LOCKED 2026-07-04): a state may additionally carry `pulse`/`glow` — the ONE
        // fixed-tick breathe primitive, three ways (border alpha / outer ring+halo / whole-element).
        var pulseBorder = false;
        var pulseSelf = false;
        var glow = false;
        if (key is not null && t.States.ValueKind == System.Text.Json.JsonValueKind.Object
            && t.States.TryGetProperty(key, out var st)
            && st.ValueKind == System.Text.Json.JsonValueKind.Object)
        {
            if (st.TryGetProperty("fill", out var f)) fillTok = f.GetString();
            if (st.TryGetProperty("border", out var bo)) borderTok = bo.GetString();
            if (st.TryGetProperty("pulse", out var pu))
            {
                pulseBorder = pu.ValueKind == System.Text.Json.JsonValueKind.True;
                pulseSelf = pu.ValueKind == System.Text.Json.JsonValueKind.String && pu.GetString() == "self";
            }
            if (st.TryGetProperty("glow", out var gl)) glow = gl.ValueKind == System.Text.Json.JsonValueKind.True;
        }
        var pulse = _ui.Manifest?.Style.Pulse;
        var t01 = pulse is not null && (pulseBorder || pulseSelf || glow) ? PulseT(pulse.PeriodMs) : 0f;
        var selfAlpha = pulseSelf && pulse is not null ? Lerp(pulse.Self.AlphaLo, pulse.Self.AlphaHi, t01) : 1f;
        if (fillTok is { Length: > 0 })
            DrawFill(new Rectangle(cell.X, cell.Y, cell.W, cell.H), new Fill { Token = fillTok }, selfAlpha);
        if (borderTok is { Length: > 0 })
        {
            var borderColor = _ui.Color(borderTok, Border0) * selfAlpha;
            if (pulseBorder && pulse is not null)
                borderColor = _ui.Color(borderTok, Border0) * Lerp(pulse.Border.AlphaLo, pulse.Border.AlphaHi, t01);
            Border(cell.X, cell.Y, cell.W, cell.H, borderColor, BorderPx(t.Border?.W ?? 1), t.Border?.Sides);
            if (glow && pulse is not null) DrawGlow(cell, borderColor, pulse.Glow, t01);
        }
    }

    // CD #30: the shared fixed-tick breathe phase, 0..1, easeInOut via a sine (fastest at the
    // midpoint, resting at the extremes) — one clock so every pulsing element stays in lockstep.
    private float PulseT(int periodMs)
    {
        var period = Math.Max(1, periodMs);
        var phase = _pulseMs % period / period;
        return (float)((Math.Sin(phase * Math.PI * 2 - Math.PI / 2) + 1) / 2);
    }

    private static float Lerp(double lo, double hi, float t) => (float)(lo + (hi - lo) * t);

    // CD #30 glow: a thin outer ring (breathing alpha) plus a soft halo OUTSIDE the border. MonoGame's
    // SpriteBatch has no blur primitive, so the halo reuses DrawShadow's own concentric-decaying-
    // ring approximation (already the established stand-in for blur in this renderer) rather than a
    // solid fill, so the interior border/fill chrome stays undimmed underneath it.
    private void DrawGlow(LayoutRect cell, Color tint, GlowPulse glow, float t01)
    {
        var haloEnvelope = Lerp(glow.Halo.AlphaLo, glow.Halo.AlphaHi, t01);
        if (haloEnvelope > 0f)
            for (var i = 1; i <= glow.Halo.Blur; i++)
            {
                var a = haloEnvelope * (glow.Halo.Blur - i + 1) / (glow.Halo.Blur + 1);
                Border(cell.X - i, cell.Y - i, cell.W + 2 * i, cell.H + 2 * i, tint * a, 1, null);
            }
        var ringAlpha = Lerp(glow.Ring.AlphaLo, glow.Ring.AlphaHi, t01);
        var ringW = Math.Max(1, (int)Math.Round(glow.Ring.W));
        Border(cell.X - ringW, cell.Y - ringW, cell.W + 2 * ringW, cell.H + 2 * ringW,
            tint * ringAlpha, ringW, null);
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

        // HIGH PRIORITY pager bug: the pager buttons author their own compact `button_pager.png` in
        // `e.Image` specifically BECAUSE the family's normal/hover/down/disabled/on textures are wide
        // ~3:1 bars sized for the ButtonSlice 9-slice — squeezing that geometry into a ~20x15px pager
        // button skewed its bevel/rivets into a mismatched aspect ratio ("rotated and overscaled").
        // When `e.Image` names a texture that ISN'T one of this family's own state textures, it's a
        // deliberate per-element override (not just CD's usual same-family preview default, which DOES
        // match one of the state entries and must keep using the normal state-driven skin/9-slice
        // below) — draw it untinted for every interaction state instead of 9-slicing the wrong frame.
        if (e.Image is { Length: > 0 } img && !FamilyOwnsImage(e.States, img)
            && _assets.Texture(NormalizeContentPath(img)) is { } ownSkin)
        {
            _spriteBatch.Draw(ownSkin, r, Color.White);
            return true;
        }

        var key = !enabled ? "disabled"
            : e.Binds == "combat.autoAttack" && InRun && Exp.IsAuto() ? "on"
            // Node cleared: RETREAT->REDEPLOY reuses the gold "on" skin (Doug wants it gold; the "on"
            // texture already ships on every button-family element — no new CD asset needed).
            : e.Binds == "combat.retreat" && InRun && Exp.State == ExpeditionState.Cleared ? "on"
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

    // Whether `image` (a manifest-authored path) matches one of this family's own state textures —
    // i.e. it's just CD's same-family preview default, not a genuinely distinct per-element skin.
    private static bool FamilyOwnsImage(System.Text.Json.JsonElement states, string image)
    {
        var stem = NormalizeContentPath(image);
        foreach (var prop in states.EnumerateObject())
            if (prop.Value.ValueKind == System.Text.Json.JsonValueKind.String && prop.Value.GetString() == stem)
                return true;
        return false;
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
        "technique.attrColor" or "loadout.attrColor" or "invItems.attrColor" or "minion.gateColor" =>
            datum switch
            {
                Roguebane.Core.Technique t => t.Stat.ToString().ToLowerInvariant(),
                Roguebane.Core.Minion m => m.Stat.ToString().ToLowerInvariant(),
                Roguebane.Core.Weapon w => w.Stat.ToString().ToLowerInvariant(),
                Roguebane.Core.Armor ar => ar.Governing.ToString().ToLowerInvariant(),
                _ => null,
            },
        _ => null,
    };

    // Resolve a template part's `binds` against a live datum -> display text, or null to use the sample.
    // Missing-data binds (race tag/blurb, per-attr tiles, Core Effect text) return null pending their data.
    // Instance (not static): the Weapon case needs InRun/Exp to read the equipped Body's discounted
    // reserve instead of the item's raw authored Reserve (2026-07-06 loop bug).
    private string? ResolveBind(object datum, string? bind) => datum switch
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
            "race.id" => r.Id, // head-sprite imageBind (sprites/body/{race.id}_grunt/head_healthy)
            "race.hp" => r.Hp.ToString(),
            "race.tag" => r.Tag,
            "race.blurb" => r.Blurb,
            _ => null,
        },
        Roguebane.Core.CoreRune c => bind switch
        {
            "core.name" => c.Title,
            "core.role" => c.Archetype,
            "core.badge" => c.Badge, // the role chip datum (07-03 drop A1)
            "core.budget" => c.RuneBudget.ToString(),
            "core.minionCap" => c.MinionCap.ToString(),
            "core.actionSlots" => c.ActionSlots.ToString(), // the bar's real capacity (RULES_SNAPSHOT
                                                             // "Actions") -- was Kit.Count, undersized
                                                             // 5 of 7 cores (fixed 2026-07-06)
            "core.coreEffectName" => c.CoreEffectName,
            "core.coreEffectDesc" => c.CoreEffectDesc, // core.coreEffect = block chrome, resolves to nothing
            // 2026-07-06: these four were unhandled -> every card fell back to the manifest's static
            // SAMPLE kit text (every card showing the same "Iron Longsword + Wooden Shield" etc.),
            // surfaced verifying Task #2's paging. Format from the roster's own live data instead.
            "core.kitWeapon" => FormatKitWeapons(c.WeaponKit),
            "core.kitArmor" => FormatKitArmor(c.ArmorKit),
            "core.kitTech" => string.Join(", ", c.Kit.Select(tk => DisplayName(tk.Id))),
            "core.kitMinion" => c.MinionKit.Count > 0
                ? string.Join(", ", c.MinionKit.Select(mk => DisplayName(mk.Id)))
                : "—",
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
        // AttrRow (§7 4-ZONE ENCODING). Bind names read by MEANING, not by tuple position (that swap
        // was bug #4's smaller half): "available" = free-right-now, "alloc" = the total pool
        // allocated to this stat (== current Capacity).
        AttrRow ab => bind switch
        {
            "attrs.key" or "pool.attr.key" => ab.Key,
            "attrs.part" or "pool.attr.part" => ab.Part,
            "attrs.available" or "pool.attr.available" => (ab.Capacity - ab.GearReserved - ab.TechReserved).ToString(),
            "attrs.alloc" or "pool.attr.alloc" => ab.Capacity.ToString(),
            "attrs.damaged" or "pool.attr.damaged" => ab.Damaged.ToString(),
            "attrs.gearReserved" or "pool.attr.gearReserved" => ab.GearReserved.ToString(),
            "attrs.techReserved" or "pool.attr.techReserved" => ab.TechReserved.ToString(),
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
            // Discounted, not raw — same shape as Weapon's invItems.badgeNum fix (2026-07-06): a
            // technique card must show what Activate()'s Consults/Finesse/JackOfAllTrades gate
            // actually charges, not Technique.Reserve.
            "invItems.badgeNum" => (InRun ? Exp.Player.Body.EffectiveTechniqueReserve(t) : t.Reserve).ToString(),
            // same gap as Weapon/Armor's invItems.effect (2026-07-06): every technique card in the
            // Inventory list fell back to the static gear SAMPLE ("...DPS.") instead of its own copy.
            "technique.description" or "invItems.effect" => t.DescText,
            // The mock's separately-positioned damage highlight can't land on live wrap — the
            // description already carries the number ({power}); resolve EMPTY so the sample never stamps.
            "technique.amount" => "",
            "technique.attr" => t.Stat.ToString().ToUpperInvariant(),
            "technique.id" or "loadout.id" => t.Id, // icons/technique/{id} imageBinds
            _ => null,
        },
        Roguebane.Core.Weapon w => bind switch
        {
            "invItems.name" or "gear.name" => w.Name is { Length: > 0 } ? w.Name : DisplayName(w.Id),
            "invItems.badgeLabel" => w.Stat.ToString().ToUpperInvariant(),
            // Discounted, not raw: a card must show what the equip gate actually charges (WarlordMight/
            // FletcherLuck/JackOfAllTrades), or the badge lies about the cost (2026-07-06 loop bug).
            "invItems.badgeNum" => (InRun ? Exp.Player.Body.EffectiveWeaponReserve(w) : w.Reserve).ToString(),
            // 2026-07-06: unhandled -> every gear card (weapon AND armor alike) fell back to the
            // manifest's static SAMPLE "4 dmg . 1.0x timer . 0.50 DPS.", surfaced alongside the
            // coreCard kit-bind gap. Power/Timer are the weapon's real §6d fields.
            "invItems.effect" => $"{w.Power} power * {w.Timer:0.0}x timer",
            _ => null,
        },
        Roguebane.Core.Armor ar => bind switch
        {
            "invItems.name" or "gear.name" => ar.Name,
            "invItems.badgeLabel" => ar.Governing.ToString().ToUpperInvariant(),
            "invItems.badgeNum" => ar.Tier.ToString(),
            // Armor has no dmg/timer (that's weapon cadence) -- show the piece's real §6c line effect.
            "invItems.effect" => ar.Line switch
            {
                ArmorLine.Plate => $"-{ar.PartMitigation} part dmg",
                ArmorLine.Leather => $"+{ar.EvadePct}% evade",
                _ => $"+{ar.SpellDamage} spell dmg",
            },
            _ => null,
        },
        Roguebane.Core.Minion mn => bind switch
        {
            "loadout.name" or "invItems.name" or "minion.name" => DisplayName(mn.Id),
            "loadout.id" => mn.Id,
            "loadout.attr" => mn.Stat.ToString().ToUpperInvariant() + " " + mn.Reserve,
            "invItems.badgeLabel" or "minion.cost" => mn.Stat.ToString().ToUpperInvariant(),
            "invItems.badgeNum" => mn.Reserve.ToString(),
            "minion.description" => mn.DescText,
            _ => null,
        },
        _ => null,
    };

    // Content ids are lower-case, underscore-separated for multi-word ones ("swing", "iron_golem");
    // cards show them capitalised with underscores as spaces, per design/02 (bug: "iron_golem" was
    // rendering as literal "Iron_golem" on the merchant wares card — only the id's first char was
    // ever capitalised, the underscore never touched).
    private static string DisplayName(string id) => string.Join(" ", id.Split('_')
        .Select(w => w.Length == 0 ? w : char.ToUpperInvariant(w[0]) + w[1..]));

    // A core's starting weapons, joined "+" (matches the coreCard's one-line WEAPON row); a repeated
    // weapon (Reaver's twin daggers) collapses to "2x Name" instead of listing it twice.
    private static string FormatKitWeapons(IReadOnlyList<Weapon> weapons) => weapons.Count == 0 ? "—"
        : string.Join(" + ", weapons
            .Select(wk => wk.Name is { Length: > 0 } ? wk.Name : DisplayName(wk.Id))
            .GroupBy(n => n)
            .Select(g => g.Count() > 1 ? $"{g.Count()}× {g.Key}" : g.Key));

    // A core's starting armor as family + count ("4x Plate"): every kit today is one uniform ladder/
    // tier (§7a), and the coreCard's ARMOR row is sized for a short line, not 4 distinct piece names.
    private static string FormatKitArmor(IReadOnlyList<Armor> armor) =>
        armor.Count == 0 ? "—" : $"{armor.Count}× {armor[0].Line}";

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
        return datum is Roguebane.Core.Minion && bind == "minion.state" ? "ACTIVE" : null;
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

    private void DrawFill(Rectangle r, Fill fill, float alpha = 1f)
    {
        if (fill.IsGradient)
            DrawGradient(r.X, r.Y, r.Width, r.Height,
                _ui.Color(fill.From ?? "panel", PanelTop) * alpha, _ui.Color(fill.To ?? "border", PanelBot) * alpha,
                fill.Dir == "horizontal" ? GradientDir.Horizontal : GradientDir.Vertical);
        else if (!string.IsNullOrEmpty(fill.Token))
            Rect(r.X, r.Y, r.Width, r.Height, _ui.Color(fill.Token!, Panel0) * alpha);
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
