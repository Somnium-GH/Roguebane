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
    private void DrawQuestScreen()
    {
        DrawCityMapScreen(); // the chart stays visible under the prompt
        if (Exp.CurrentQuest is not { } quest) return;

        const int pw = 560, ph = 260;
        var px = (W - pw) / 2;
        var py = (H - ph) / 2;
        Panel(px, py, pw, ph);
        DrawCentered(_assets.Display, "QUEST [PLACEHOLDER]", Amber, px + pw / 2, py + 16);
        DrawWrapped(quest.Prompt, px + 20, py + 56, pw - 40, Ink);
        DrawButton($"Y - {quest.AcceptText}", px + 20, py + 180, pw - 40, 32, true, Keys.Y);
        DrawButton($"N - {quest.DeclineText}", px + 20, py + 220, pw - 40, 32, true, Keys.N);
    }

    private static int IndexOf(IReadOnlyList<MapNode> list, MapNode node)
    {
        for (var i = 0; i < list.Count; i++) if (ReferenceEquals(list[i], node)) return i;
        return -1;
    }
}
