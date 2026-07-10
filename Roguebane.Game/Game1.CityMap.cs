using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Roguebane.Core;
using Roguebane.Core.Layout;

namespace Roguebane.Game;

// The CityMap screen shell: a pure manifest render since the 07-03 drop homed the last overlays.
// (History: hand-drawn chart/panels deleted at the 07-02 visual cut-over; spine/castle panel/
// gear bar/EQUIPMENT button deleted 2026-07-03 when their manifest elements went live.)
public partial class Game1
{
    // Run-map screen (design/03): pure manifest render. The 07-03 drop homed every ex-overlay
    // (castlePanel+fortRows, campaignStrip, packChips, equipmentBtn) and they resolve live, so the
    // legacy hand-draws (spine/castle panel/gear bar/EQUIPMENT button) are DELETED (P0-C.9).
    private void DrawCityMapScreen()
    {
        DrawManifestScreen("citymap");
        DrawStateOverlay();
    }

    // Ad-hoc placeholder popover (2026-07-09 Doug crash fix): NodeType.Quest had NO Game-layer
    // rendering/interaction at all -- entering one crashed on AssetRegistry's node-icon lookup (fixed
    // separately, AssetRegistry.cs) and, even past that, had no prompt to accept/decline through. No
    // manifest template exists yet for a real quest card (a CD content ask, same split as the Merchant
    // popover before its 07-03 manifest cut-over) -- flagged [PLACEHOLDER] like the quest content
    // itself so this doesn't read as finished design.
    // Shared with UpdateChoosing so the mouse hit-rects can never drift from where these actually
    // draw (2026-07-09 Doug fix: the buttons rendered hover/down states but nothing checked Click()
    // on them -- only the Y/N keys worked, so a mouse click on the quest popover did nothing).
    private const int QuestPanelW = 560, QuestPanelH = 260;
    private Rectangle QuestAcceptRect => new((W - QuestPanelW) / 2 + 20, (H - QuestPanelH) / 2 + 180, QuestPanelW - 40, 32);
    private Rectangle QuestDeclineRect => new((W - QuestPanelW) / 2 + 20, (H - QuestPanelH) / 2 + 220, QuestPanelW - 40, 32);

    private void DrawQuestScreen()
    {
        DrawCityMapScreen(); // the chart stays visible under the prompt
        if (Exp.CurrentQuest is not { } quest) return;

        var px = (W - QuestPanelW) / 2;
        var py = (H - QuestPanelH) / 2;
        Panel(px, py, QuestPanelW, QuestPanelH);
        DrawCentered(_assets.Display, "QUEST [PLACEHOLDER]", Amber, px + QuestPanelW / 2, py + 16);
        DrawWrapped(quest.Prompt, px + 20, py + 56, QuestPanelW - 40, Ink);
        var ar = QuestAcceptRect;
        var dr = QuestDeclineRect;
        DrawButton($"Y - {quest.AcceptText}", ar.X, ar.Y, ar.Width, ar.Height, true, Keys.Y);
        DrawButton($"N - {quest.DeclineText}", dr.X, dr.Y, dr.Width, dr.Height, true, Keys.N);
    }

    private static int IndexOf(IReadOnlyList<MapNode> list, MapNode node)
    {
        for (var i = 0; i < list.Count; i++) if (ReferenceEquals(list[i], node)) return i;
        return -1;
    }
}
