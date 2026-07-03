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
    // Run-map screen (design/03) — VISUAL CUT-OVER (2026-07-02): the manifest screen is the render
    // (header/gauges/war-party/legend/chart, all live-bound); node clicks share its geometry via
    // NodeRect. The legacy hand-drawn chart/panels are deleted. What remains hand-drawn here are the
    // elements design/03 gives NO HOME to (FLAGGED stopgap overlays, Needs-CD/human in STATUS):
    // the gear bar, the EQUIPMENT button, the castle panel and the campaign spine.
    private void DrawCityMapScreen()
    {
        DrawManifestScreen("citymap");
        // The manifest topbar already carries the run resources (incl. gold) — the legacy strip and
        // gold readout would double it. The spine sits bottom-left, clear of the chart's bottom lane.
        DrawSpine(20, H - 96);
        DrawCastlePanel(740, 158);
        DrawGearBar(20, H - 44);
        DrawButton("EQUIPMENT [E]", EquipOpenRect.X, EquipOpenRect.Y,
            EquipOpenRect.Width, EquipOpenRect.Height, true, Keys.E);

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

    private static int IndexOf(IReadOnlyList<MapNode> list, MapNode node)
    {
        for (var i = 0; i < list.Count; i++) if (ReferenceEquals(list[i], node)) return i;
        return -1;
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
