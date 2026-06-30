namespace Roguebane.Core.Layout;

// Maps the Core anatomy (stat-bearing BodyParts) onto a humanoid figure's VISUAL parts so the
// stage composer can ask "what condition / armored?" per visual part. The stat<->visual
// convention (INT=head, CON=torso, STR=arms, DEX=legs) mirrors the simulation's part-groups;
// kept here (pure) so the shell stays a thin blitter. Paired limbs split L/R across the group's
// two parts in add-order.
public static class FigureBinding
{
    private static readonly Dictionary<string, Stat> PartStat = new()
    {
        ["head"] = Stat.Int,
        ["torso"] = Stat.Con,
        ["armL"] = Stat.Str,
        ["armR"] = Stat.Str,
        ["legL"] = Stat.Dex,
        ["legR"] = Stat.Dex,
        ["boots"] = Stat.Dex,
    };

    // L/R limbs index into the stat group's two parts; the rest read the whole group.
    private static readonly Dictionary<string, int> PairIndex = new()
    {
        ["armL"] = 0, ["armR"] = 1, ["legL"] = 0, ["legR"] = 1,
    };

    // Only these have a BARE (unarmored) sprite row; others always draw their plain art.
    private static readonly HashSet<string> BareCapable = new() { "armL", "armR", "legL", "legR" };

    public static PartCondition Condition(Body body, string visualPart)
    {
        if (!PartStat.TryGetValue(visualPart, out var stat)) return PartCondition.Healthy;
        var group = body.Parts.Where(p => p.Stat == stat).ToList();
        if (group.Count == 0) return PartCondition.Healthy;

        IEnumerable<BodyPart> parts = group;
        if (PairIndex.TryGetValue(visualPart, out var idx))
            parts = new[] { group[Math.Min(idx, group.Count - 1)] };

        return ConditionOf(body, parts);
    }

    // A bare-capable part shows its bare row only while NOTHING armours its group.
    public static bool UseBare(Body body, string visualPart)
        => BareCapable.Contains(visualPart)
           && PartStat.TryGetValue(visualPart, out var stat)
           && body.ArmorOn(stat) is null;

    // Is this visual part's group wearing armour?
    public static bool IsArmored(Body body, string visualPart)
        => PartStat.TryGetValue(visualPart, out var stat) && body.ArmorOn(stat) is not null;

    // Whether this part has a bare/armoured sprite ROW. Parts without one (torso/head/boots) can't show
    // armour through the sprite, so the shell draws a composed indicator instead.
    public static bool HasBareVariant(string visualPart) => BareCapable.Contains(visualPart);

    private static PartCondition ConditionOf(Body body, IEnumerable<BodyPart> parts)
    {
        var cap = 0;
        var live = 0;
        foreach (var p in parts) { cap += p.Capacity; live += body.Contribution(p); }
        if (cap == 0) return PartCondition.Healthy;
        var frac = (float)live / cap;
        return frac <= 0f ? PartCondition.Broken : frac < 0.5f ? PartCondition.Damaged : PartCondition.Healthy;
    }
}
