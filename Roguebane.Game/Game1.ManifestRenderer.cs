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
    private void DrawManifestScreen(string screenId)
    {
        var s = _ui.ScreenDef(screenId);
        if (s is null) return;
        // Manifest z is DEPTH (extracted container panels carry high z, leaf content z=1) — draw
        // back-to-front, so a filled panel never paints over the content it contains.
        foreach (var e in s.Elements.OrderByDescending(x => x.Z))
            DrawManifestElement(e, ManifestUi.Rect(s, e));
    }

    private void DrawManifestElement(Element e, Rectangle r)
    {
        // A TEXT element's shadow is a text shadow (offset glyph copy, drawn with the text below) —
        // rect-shadowing it would paint a solid box behind the words.
        if (e.Shadow is { } sh && e.Type != "text")
            DrawShadow(r.X, r.Y, r.Width, r.Height, sh.Dx, sh.Dy, sh.Blur, (float)sh.Opacity);
        // §10: fill is the element's BACKGROUND, the nine-slice frame is chrome layered on it — an
        // element may carry both (topBar), so draw fill first, then frame.
        if (e.Fill is { } fill)
            DrawFill(r, fill);
        if (e.Frame is { } fr && fr.Slice.Length == 4 && _assets.Texture(fr.Asset) is { } ftex)
            DrawFrameTex(ftex, fr, r);
        if (e.Border is { } b)
        {
            // An element-level colorBind tints the border (the accent rule) when it resolves.
            var accent = ResolveColorToken(e.ColorBind, null);
            Border(r.X, r.Y, r.Width, r.Height, _ui.Color(accent ?? b.Color ?? "border", Border0),
                Math.Max(2, b.W * SS), b.Sides);
        }

        switch (e.Type)
        {
            case "text":
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
                Sprite(_assets.Texture(e.Image!), r.X, r.Y, r.Width, r.Height, Color.White);
                break;
            case "button":
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
                if (!ef.Down && _ctrl.IsTargeting(Exp))
                {
                    if (ef.Frame is not null)
                    {
                        var band = r.Height / 4;
                        for (var bd = 1; bd < 4; bd++) Rect(r.X, r.Y + bd * band, r.Width, 1, new Color(Ink, 90));
                        if (FoePartAt(ef, _cursor) is { } hov && FoePartScreenRect(ef, hov.Stat, r) is { } pr)
                        {
                            Border(pr.X, pr.Y, pr.Width, pr.Height, Ink);
                            if (_assets.Reticle("aiming") is { } aim)
                                Sprite(aim, pr.X + pr.Width / 2 - 24, pr.Y + pr.Height / 2 - 24, 48, 48, Color.White);
                        }
                    }
                    else Border(r.X, r.Y, r.Width, r.Height, new Color(Ink, 110));
                }
                else if (!ef.Down && Hover(r)) Border(r.X, r.Y, r.Width, r.Height, Ink);
                // Locked part-aims read as FOCUS reticles on their parts (deduped by stat).
                if (!ef.Down && ef.Frame is not null && _assets.Reticle("focus") is { } focus)
                {
                    var shown = new System.Collections.Generic.HashSet<Stat>();
                    foreach (var t in Exp.Equipment)
                        if (Exp.PartOf(t) is { } aimed && shown.Add(aimed.Stat)
                            && FoePartScreenRect(ef, aimed.Stat, r) is { } fr2)
                            Sprite(focus, fr2.X + fr2.Width / 2 - 20, fr2.Y + fr2.Height / 2 - 20, 40, 40, Color.White);
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
                Border(r.X - 3, r.Y - 3, cw + 6, ch + 6, Amber);
                Text(_assets.Mono, "you are here", r.X - 8, r.Y + ch + 2, Amber);
            }
            else if (oi >= 0) // a reachable onward deployment
            {
                Border(r.X - 2, r.Y - 2, cw + 4, ch + 4, Hover(r) ? Ink : new Color(150, 130, 95));
                Text(_assets.Mono, $"[{oi + 1}] {seen.ToString().ToLower()}", r.X - 6, r.Y + ch + 2, Ink);
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
        "preview.coreEffectDesc" or "preview.coreEffect" => _build.CoreRune.CoreEffectDesc,
        "core" => _build.Race.Name + " " + _build.CoreRune.Title,
        "runes.budget" => _build.Runes.Available + " free / " + _build.Runes.Budget,
        "Body.hp" => InRun ? Exp.Player.Hp + " / " + Exp.Player.MaxHp : null,
        "encounter.foe.hp" => InRun && Exp.Enemy is { } foe ? foe.Hp + " / " + foe.MaxHp : null,
        // Combat verbs (design/01 chips; labels were flattened by extraction -> authored here).
        "combat.autoAttack" => InRun ? (Exp.IsAuto() ? "AUTO-ATTACK ON" : "AUTO-ATTACK") : null,
        "combat.retreat" => InRun ? "RETREAT" : null,
        // §6b shield bar header: standing points / total layers across the body's shield sources.
        "ShieldPool" => InRun && Exp.Player.Body.ShieldLayers > 0
            ? "SHIELD " + Exp.Player.Body.ShieldPoints + "/" + Exp.Player.Body.ShieldLayers : null,
        "combat.paused" => _paused ? "HELD" : null, // badge shows only while the fight is held
        "campaign.taken" => InRun ? _campaign.LegIndex + " / " + _campaign.LegCount : null,
        _ => null,
    };

    // Run state exists only after a march; encounter binds fall back to samples until then.
    private bool InRun => _campaign is not null;

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
            // Positional binds repeat the SAME bind N times per card (attr tiles 4x, attr-bar pips 12x,
            // in template order); count each occurrence per card to pick the right datum slice.
            int valIx = 0, keyIx = 0, pipIx = 0;
            var occ = new System.Collections.Generic.Dictionary<string, int>(); // per-bind occurrence (rune rows)
            foreach (var pp in CardTemplate.Place(tmpl, cell.X, cell.Y))
            {
                if (pp.Binds is { } sel && sel.EndsWith(".selection") && i != selIx)
                    continue; // only the chosen card wears its selection chip
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
                            Math.Max(2, pb.W * SS), pb.Sides);
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
                    Sprite(_assets.Texture(img!), pp.Rect.X, pp.Rect.Y, pp.Rect.W, pp.Rect.H, Color.White);
                    continue;
                }
                var text = isStatePart ? stateText : pp.Binds switch
                {
                    "race.attrs.value" => AttrTile(datum, valIx++)?.value,
                    "race.attrs.key" => AttrTile(datum, keyIx++)?.key,
                    "attr.color" => null, // the swatch is pure fill, no text
                    _ => datum is not null ? ResolveBind(datum, pp.Binds) : null,
                } ?? pp.Sample;
                if (!string.IsNullOrEmpty(text))
                    TextPxWrapped(pp.Font == "display" ? _assets.Display : _assets.Mono,
                        text!, RectOf(pp.Rect), _ui.Color(pp.Color ?? "ink", Ink), pp.FontPx);
            }
        }
    }

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
        "invItems" => _invTab switch
        {
            0 => InRun
                ? Exp.Player.Body.Hands.Cast<object>()
                    .Concat(Exp.Stash.Weapons).Concat(Exp.Stash.Armor).ToList()
                : new List<object>(),
            1 => _build.Palette.Cast<object>().ToList(),
            2 => _build.CoreRune.MinionKit.Concat(_build.Runes.GrantedMinions).Cast<object>().ToList(),
            _ => null,
        },
        // The Rune Bag (design/02): one group per PATH ladder — the MARKS/PATHS/KEYSTONES taxonomy
        // is OPEN (§17), so the model's actual grouping (ladders) is what renders.
        "runeGroups" => _build.Paths.Cast<object>().ToList(),
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
        _ => null,
    };

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

    // A self-styled LEAF template (no parts): the cell itself is the visual — its fill/border restyled
    // by the template's `states` block keyed from the bound datum (shield pips: live/spent). Dashed
    // border styles draw solid for now (a 8x5 design-px pip; dash polish rides the pixel-compare pass).
    private void DrawLeafTemplate(Template t, LayoutRect cell, object? datum)
    {
        var fillTok = t.Fill?.Token;
        var borderTok = t.Border?.Color;
        var key = t.Binds == "point.live" && datum is bool live ? (live ? "live" : "spent") : null;
        if (key is not null && t.States.ValueKind == System.Text.Json.JsonValueKind.Object
            && t.States.TryGetProperty(key, out var st)
            && st.ValueKind == System.Text.Json.JsonValueKind.Object)
        {
            if (st.TryGetProperty("fill", out var f)) fillTok = f.GetString();
            if (st.TryGetProperty("border", out var b)) borderTok = b.GetString();
        }
        var r = new Rectangle(cell.X, cell.Y, cell.W, cell.H);
        if (fillTok is { Length: > 0 }) DrawFill(r, new Fill { Token = fillTok });
        if (borderTok is { Length: > 0 })
            Border(r.X, r.Y, r.Width, r.Height, _ui.Color(borderTok, Border0),
                Math.Max(2, (t.Border?.W ?? 1) * SS), t.Border?.Sides);
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
                     dstCornerScale: 1.0 / SS))
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
            "core.coreEffectDesc" or "core.coreEffect" => c.CoreEffectDesc,
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
        Roguebane.Core.Technique t => bind switch
        {
            "loadout.name" or "invItems.name" or "technique.name" => DisplayName(t.Id),
            "loadout.attr" => t.Stat.ToString().ToUpperInvariant() + " " + t.Reserve,
            "invItems.badgeLabel" or "technique.cost" => t.Stat.ToString().ToUpperInvariant(),
            "invItems.badgeNum" => t.Reserve.ToString(),
            "technique.description" => t.DescText,
            _ => null,
        },
        Roguebane.Core.Weapon w => bind switch
        {
            "invItems.name" => DisplayName(w.Id),
            "invItems.badgeLabel" => w.Stat.ToString().ToUpperInvariant(),
            "invItems.badgeNum" => w.Reserve.ToString(),
            _ => null,
        },
        Roguebane.Core.Armor ar => bind switch
        {
            "invItems.name" => DisplayName(ar.Id),
            "invItems.badgeLabel" => ar.Group.ToString().ToUpperInvariant(),
            "invItems.badgeNum" => ar.Value.ToString(),
            _ => null,
        },
        Roguebane.Core.Minion mn => bind switch
        {
            "loadout.name" or "invItems.name" or "bay.name" => DisplayName(mn.Id),
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
        foreach (var p in NineSlice.Patches(tex.Width, tex.Height, fr.Slice, dst,
                     tile: fr.Repeat == "tile", centerFill: fr.CenterFill))
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
