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

    private static int IndexOf(IReadOnlyList<MapNode> list, MapNode node)
    {
        for (var i = 0; i < list.Count; i++) if (ReferenceEquals(list[i], node)) return i;
        return -1;
    }
}
