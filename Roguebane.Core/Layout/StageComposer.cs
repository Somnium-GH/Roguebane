namespace Roguebane.Core.Layout;

// Turns a figure manifest + the live Core condition into an ordered list of draw
// placements. Pure geometry/naming — it asks two callbacks for the per-part Core
// state so the shell keeps the anatomy mapping (which visual part reads which Core
// BodyPart) and this layer stays headless-testable. The shell only blits the result.
public enum PartCondition { Healthy, Damaged, Broken }

// A part to draw: sprite content key (figure-local), source rect in figure space, z-order.
public readonly record struct PartPlacement(string Part, string SpriteKey, int[] Rect, int Z);

// A gear piece to mount: drawn at the socket point (figure space), positioned by its own pivot.
public readonly record struct GearPlacement(string Gear, string Socket, int[] Anchor, int Z);

public sealed class StageComposer
{
    private readonly LayoutManifest _m;
    public StageComposer(LayoutManifest manifest) => _m = manifest;

    // Ordered figure parts (by the figure's z list), each resolved to a state-keyed sprite.
    // resolveCondition: visual part name -> Healthy/Damaged/Broken.
    // useBare: visual part name -> draw the BARE variant (no armor over it). Parts with no bare
    //   art (e.g. boots/head) should return false; the shell owns that knowledge.
    public IReadOnlyList<PartPlacement> ComposeFigure(
        string figureId,
        Func<string, PartCondition> resolveCondition,
        Func<string, bool> useBare)
    {
        var fig = _m.Figures[figureId];
        var placed = new List<PartPlacement>();
        for (var z = 0; z < fig.Z.Length; z++)
        {
            var part = fig.Z[z];
            if (!fig.Parts.TryGetValue(part, out var p)) continue; // z slot with no part rect (e.g. frontGear)
            var suffix = StateSuffix(resolveCondition(part), useBare(part));
            placed.Add(new PartPlacement(part, $"sprites/body/{figureId}/{part}_{suffix}", p.Rect, z));
        }
        return placed;
    }

    // Gear mounted on the figure, anchored at its socket point, layered at the figure's frontGear slot.
    public IReadOnlyList<GearPlacement> ComposeGear(string figureId)
    {
        var fig = _m.Figures[figureId];
        var gearZ = Array.IndexOf(fig.Z, "frontGear");
        if (gearZ < 0) gearZ = fig.Z.Length; // default: above all parts
        var mounts = new List<GearPlacement>();
        foreach (var mount in fig.Mounts)
        {
            if (!fig.Sockets.TryGetValue(mount.Socket, out var anchor)) continue;
            mounts.Add(new GearPlacement(mount.Gear, mount.Socket, anchor, gearZ));
        }
        return mounts;
    }

    // The state suffix is DATA (style.partStates), not hardcoded — armored vs bare picks the row.
    private string StateSuffix(PartCondition c, bool bare)
    {
        var key = (c, bare) switch
        {
            (PartCondition.Healthy, false) => "ok",
            (PartCondition.Damaged, false) => "damaged",
            (PartCondition.Broken, false) => "broken",
            (PartCondition.Healthy, true) => "bareOk",
            (PartCondition.Damaged, true) => "bareDmg",
            (PartCondition.Broken, true) => "bareBroke",
            _ => "ok",
        };
        return _m.Style.PartStates.GetValueOrDefault(key, key);
    }
}
