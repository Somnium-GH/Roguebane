namespace Roguebane.Core.Layout;

// §12a worn-armor PART SELECTION (race-first, full-part sprites — CORRECTION #3, 2026-07-04:
// supersedes the earlier line-first overlay draft). Each candidate key names a COMPLETE part sprite
// (body + armor drawn in), not a runtime overlay — so this resolver picks ONE key, it doesn't
// composite several. GENERIC + bare selection only: THEMED (a core's own favored line, §7a) is the
// same slice's second half once CD's B12 art lands. How the selected part composes with the
// existing per-core figure geometry (Warden's bulkier torso etc.) is still an OPEN engine question
// (STATUS §17 #15, deferred) — this class only answers "which key", not "draw instead of what".
public static class WornArmorBinding
{
    // §12a slot set: head/chest/arms/legs only. Boots shares the DEX group for condition/border
    // purposes (FigureBinding) but isn't one of the four worn-part slots — no key of its own.
    private static readonly Dictionary<Stat, string> SlotWord = new()
    {
        [Stat.Int] = "head",
        [Stat.Con] = "chest",
        [Stat.Str] = "arms",
        [Stat.Dex] = "legs",
    };

    private static readonly Dictionary<ArmorLine, string> TypeWord = new()
    {
        [ArmorLine.Plate] = "str",
        [ArmorLine.Leather] = "dex",
        [ArmorLine.Robe] = "int",
    };

    private static string ConditionWord(PartCondition c) => c switch
    {
        PartCondition.Damaged => "damaged",
        PartCondition.Broken => "broken",
        _ => "healthy",
    };

    // Ordered candidate keys, generic tier down to bare (no themed lookup yet): armored same-
    // condition -> armored healthy -> bare same-condition -> bare healthy. Empty only when this
    // visual part has no §12a slot at all (boots, or anything FigureBinding doesn't map to a stat).
    public static IReadOnlyList<string> SpriteKeys(Body body, string visualPart, string race)
    {
        if (visualPart == "boots") return Array.Empty<string>();

        var stat = FigureBinding.StatOf(visualPart);
        if (stat is not { } s || !SlotWord.TryGetValue(s, out var slot)) return Array.Empty<string>();

        var cond = ConditionWord(FigureBinding.Condition(body, visualPart));
        var baseKey = $"sprites/gear/worn/{race}/{slot}";
        var keys = new List<string>();

        if (body.ArmorOn(s) is { } worn && body.ArmorSustained(worn))
        {
            var type = TypeWord[worn.Line];
            keys.Add($"{baseKey}/{type}_{worn.Tier}_{cond}");
            if (cond != "healthy") keys.Add($"{baseKey}/{type}_{worn.Tier}_healthy");
        }
        keys.Add($"{baseKey}/bare_{cond}");
        if (cond != "healthy") keys.Add($"{baseKey}/bare_healthy");
        return keys;
    }
}
