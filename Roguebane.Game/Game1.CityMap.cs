using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Roguebane.Core;
using Roguebane.Core.Layout;

namespace Roguebane.Game;

// The LEGACY CityMap screen half of the shell (SRP split, 2026-07-02): the hand-drawn run-map —
// chart, supply/war-party/legend/castle panels, gear bar, run resources, and the campaign spine
// (the merchant popover stopgap RETIRED 2026-07-02 — the design/07 stall screen replaced it). Retires wholesale when the citymap manifest
// cut-over lands (its Needs-CD/human blockers are in STATUS).
public partial class Game1
{
    // Run-map screen (design/03): the resources, the current beacon, and the charted jumps as cards
    // (fog-aware icons). At a merchant the shop verbs are live instead of a fight ahead.
    private void DrawCityMapScreen()
    {
        Stretch(_assets.Background("map_chart"), 0, 0, W, H);
        Panel(0, 0, W, 40);
        Text(_assets.Display, "REDEPLOY", 16, 8, Ink);
        DrawRunResources(200, 10);
        DrawSpine(720, 12);

        DrawSupplyPanels(16, 48);
        DrawWarParty(300, 64, 300);
        DrawChart();
        DrawMapLegend(756, 64); // top-right; clears the header, war party, and the merchant panel below
        DrawCastlePanel(740, 158);
        DrawGearBar(20, H - 44);
        DrawButton("EQUIPMENT [E]", EquipOpenRect.X, EquipOpenRect.Y,
            EquipOpenRect.Width, EquipOpenRect.Height, true, Keys.E);

        DrawResourceStrip();
        DrawStateOverlay();
    }

    // design/03 right-side card: the run's destination. The castle is the structural boss the whole
    // leg presses toward (its layers fall in order; banked support rallies on it). Display-only.
    private void DrawCastlePanel(int x, int y)
    {
        Panel(x, y, 200, 132);
        Sprite(_assets.Node(NodeType.Castle), x + 10, y + 12, 26, 26, Color.White);
        Text(_assets.Mono, "THE CASTLE", x + 44, y + 14, Ink);
        Text(_assets.Mono, "the exit", x + 44, y + 30, Amber);
        Text(_assets.Mono, "STRUCTURED FOE", x + 12, y + 56, Muted);
        Text(_assets.Mono, "gate / wall / keep", x + 12, y + 74, Muted);
        Text(_assets.Mono, "banked support", x + 12, y + 96, Muted);
        Text(_assets.Mono, "rallies here", x + 12, y + 114, Muted);
    }

    // design/03 signature: the two top-left gauges as PANELS with pip bars + flavor (the jump budget
    // and the support you can rally), in place of bare top-bar counts.
    private void DrawSupplyPanels(int x, int y)
    {
        var map = Exp.Map;
        var holds = map.Nodes.Count(n => n.Type == NodeType.ResourceHold);

        Panel(x, y, 250, 64);
        Text(_assets.Mono, "SUPPLIES", x + 12, y + 8, Muted);
        Text(_assets.Mono, $"{map.Supplies}/{map.MaxSupplies}", x + 200, y + 8, map.Supplies > 0 ? Ink : Blood);
        DrawPipStrip(x + 12, y + 28, map.Supplies, map.MaxSupplies, map.Supplies > 0 ? Amber : Blood);
        Text(_assets.Mono, "1 supply per deployment", x + 12, y + 44, Muted);

        var sy = y + 72;
        Panel(x, sy, 250, 64);
        Text(_assets.Mono, "MUSTERED SUPPORT", x + 12, sy + 8, Muted);
        Text(_assets.Mono, $"{map.SupportBank}/{holds}", x + 200, sy + 8, Ink);
        DrawPipStrip(x + 12, sy + 28, map.SupportBank, holds, new Color(120, 160, 200));
        Text(_assets.Mono, "banked from held beacons", x + 12, sy + 44, Muted);
    }

    // A row of filled/empty segments (design/03 gauges). Filled in col, the remainder a dim outline.
    private void DrawPipStrip(int x, int y, int filled, int total, Color col)
    {
        const int seg = 16, gap = 4, h = 10;
        for (var i = 0; i < total; i++)
        {
            var sx = x + i * (seg + gap);
            if (i < filled) Rect(sx, y, seg, h, col);
            else Border(sx, y, seg, h, new Color(80, 65, 60));
        }
    }

    // Node-type key (design/03): what the chart icons mean. Display-only; tucked top-right where the
    // chart is sparse (hidden behind the merchant panel at a merchant).
    private void DrawMapLegend(int x, int y)
    {
        Text(_assets.Mono, "CHART", x, y - 16, Muted);
        (NodeType Type, string Label)[] rows =
        {
            (NodeType.Castle, "castle / exit"),
            (NodeType.Merchant, "merchant"),
            (NodeType.ResourceHold, "resource hold"),
            (NodeType.Unknown, "unknown/fight"),
        };
        for (var i = 0; i < rows.Length; i++)
        {
            var ry = y + i * 20;
            Sprite(_assets.Node(rows[i].Type), x, ry, 16, 16, Color.White);
            Text(_assets.Mono, rows[i].Label, x + 22, ry + 3, Muted);
        }
    }

    // Out-of-combat gear bar (map screen): the body's EQUIPPED gear (wielded weapons + worn armor) and
    // the carried PACK as click-to-equip chips. Equipping moves a piece pack -> body via Expedition.
    private void DrawGearBar(int x, int y)
    {
        var body = Exp.Player.Body;
        Text(_assets.Mono, "EQUIPPED", x, y - 16, Muted);
        var ex = x;
        foreach (var w in body.Hands) { GearTag(ex, w.Id, Amber); ex += 86; }
        foreach (var (s, _) in StatColors)
            if (body.ArmorOn(s) is { } a) { GearTag(ex, a.Id, StatColor(s)); ex += 86; }
        if (ex == x) Text(_assets.Mono, "-", x, y + 4, Muted);

        Text(_assets.Mono, "PACK  (click to equip)", x + 360, y - 16, Muted);
        for (var i = 0; i < PackCount; i++)
        {
            var r = PackChipRect(i);
            var (id, col) = PackItem(i);
            Panel(r.X, r.Y, r.Width, r.Height);
            Text(_assets.Mono, id, r.X + 6, r.Y + 6, Ink);
            Border(r.X, r.Y, r.Width, r.Height, Hover(r) ? Amber : col);
        }

        void GearTag(int gx, string id, Color col)
        {
            Panel(gx, y, 80, 28);
            Text(_assets.Mono, id, gx + 6, y + 6, col);
        }
    }

    private int PackCount => Exp.Stash.Weapons.Count + Exp.Stash.Armor.Count;
    private static Rectangle PackChipRect(int i) => new(380 + i * 86, H - 44, 80, 28);

    // The pack as one indexed list: weapons first, then armor (matching the click handler's order).
    private (string Id, Color Border) PackItem(int i)
    {
        var ws = Exp.Stash.Weapons;
        if (i < ws.Count) return (ws[i].Id, StatColor(ws[i].Stat));
        var a = Exp.Stash.Armor[i - ws.Count];
        return (a.Id, StatColor(a.Group));
    }

    // The half-blind beacon chart as a GRAPH (design/03): nodes placed by their grid coords, links
    // drawn solid where charted (from a visited beacon) and dotted where still uncharted; fog hides a
    // beacon's true kind behind a `?`. The current beacon reads "you are here"; reachable beacons ring
    // and number as the onward jumps.
    private void DrawChart()
    {
        var map = Exp.Map;

        // Links first, so the beacons sit on top of their connecting lines.
        foreach (var node in map.Nodes)
        {
            var from = NodeRect(node);
            var fx = from.X + ChartIcon / 2;
            var fy = from.Y + ChartIcon / 2;
            foreach (var nid in node.Next)
            {
                var to = NodeRect(map.Node(nid));
                var charted = node.Visited; // a link out of a charted beacon is itself charted
                Line(fx, fy, to.X + ChartIcon / 2, to.Y + ChartIcon / 2, 2,
                    charted ? new Color(150, 130, 95) : new Color(90, 78, 66), dashed: !charted);
            }
        }

        var options = map.Options;
        foreach (var node in map.Nodes)
        {
            var r = NodeRect(node);
            var seen = map.Sees(node);
            var isCurrent = ReferenceEquals(node, map.Current);
            Sprite(_assets.Node(seen), r.X, r.Y, ChartIcon, ChartIcon, isCurrent ? Color.White : new Color(210, 200, 190));

            var oi = IndexOf(options, node);
            if (isCurrent)
            {
                Border(r.X - 3, r.Y - 3, ChartIcon + 6, ChartIcon + 6, Amber);
                Text(_assets.Mono, "you are here", r.X - 8, r.Y + ChartIcon + 2, Amber);
            }
            else if (oi >= 0) // a reachable onward jump
            {
                Border(r.X - 2, r.Y - 2, ChartIcon + 4, ChartIcon + 4, Hover(r) ? Ink : new Color(150, 130, 95));
                Text(_assets.Mono, $"[{oi + 1}] {seen.ToString().ToLower()}", r.X - 6, r.Y + ChartIcon + 2, Ink);
            }
        }
    }

    private static int IndexOf(IReadOnlyList<MapNode> list, MapNode node)
    {
        for (var i = 0; i < list.Count; i++) if (ReferenceEquals(list[i], node)) return i;
        return -1;
    }

    // The forward-pressure track: the war party marches on the camp one step per deployment. The marker
    // slides toward the camp (left) as the distance closes; reaching it overruns the run.
    // 2026-07-02 directive (rev 2): CAMP anchors the LEFT end, the CASTLE the RIGHT; the covered-ground
    // fill loads RIGHT->LEFT IN TANDEM with the host token sliding right->left toward camp — the fill's
    // leading (left) edge tracks the host (§12's castle->camp march).
    private void DrawWarParty(int x, int y, int w)
    {
        var map = Exp.Map;
        Text(_assets.Mono, "WAR PARTY", x + 22, y - 16, Muted);
        Rect(x, y, w, 6, new Color(70, 55, 50));
        var frac = map.MarchLength > 0 ? (float)map.WarPartyDistance / map.MarchLength : 0f;
        Rect(x + (int)(frac * w), y, (int)((1f - frac) * w), 6, new Color(Blood, 190)); // fill loads right->left with the host
        Sprite(_assets.Node(NodeType.Camp), x - 8, y - 8, 20, 20, Color.White);           // camp LEFT
        Sprite(_assets.Node(NodeType.Castle), x + w - 12, y - 8, 20, 20, Color.White);    // castle RIGHT
        var mx = x + (int)(frac * (w - 22)); // distance-to-camp places the host: 1 = castle, 0 = camp
        // The closing war-party host: its own icon, swapping to the "near" variant when it's about to
        // reach camp (the loss timer). Falls back to a blood-tinted castle glyph if the art is missing.
        var near = map.WarPartyDistance <= 2;
        var host = _assets.Texture("icons/map/enemy_host" + (near ? "_near" : ""));
        if (host is not null) Sprite(host, mx, y - 12, 28, 28, Color.White);
        else Sprite(_assets.Node(NodeType.Castle), mx, y - 10, 24, 24, Blood);
        Text(_assets.Mono, map.WarPartyDistance + " to camp", x + w + 12, y - 6, Blood);
    }

    // The compact top-bar readout: gold only. Supplies + mustered support are in their design/03 panels
    // (DrawSupplyPanels); the war-party distance reads off its own track; potions are gone.
    private void DrawRunResources(int x, int y)
    {
        Sprite(_assets.Resource("spoils"), x, y, 22, 22, Color.White);
        Text(_assets.Mono, Exp.Gold.ToString(), x + 26, y + 4, Ink);
    }

    // The campaign spine (design/04): a pip per leg to the Capital, taken cities lit amber.
    // The campaign-spine strip (design/04): the legs to the Capital as a chain of castles — taken
    // (amber), here (white), unreached (dim) — the last leg marked as the Capital/peak, plus a
    // cities-taken counter. (The full branching city-graph picker waits on a branching campaign model.)
    private void DrawSpine(int x, int y)
    {
        Text(_assets.Mono, "SPINE", x, y, Muted);
        var n = _campaign.LegCount;
        for (var i = 0; i < n; i++)
        {
            var left = x + 56 + i * 22;
            var taken = i < _campaign.LegIndex;
            var here = i == _campaign.LegIndex;
            var capital = i == n - 1; // the Capital: the peak castle at the end of the road
            var sz = capital ? 22 : 18;
            Sprite(_assets.Node(NodeType.Castle), left, y - (capital ? 4 : 2), sz, sz,
                taken ? Amber : here ? Color.White : new Color(110, 95, 80));
            if (capital) Text(_assets.Mono, "^", left + 6, y - 14, Amber); // peak marker
        }
        Text(_assets.Mono, _campaign.LegIndex + "/" + n, x + 56 + n * 22 + 8, y, Amber); // cities taken
    }
}
