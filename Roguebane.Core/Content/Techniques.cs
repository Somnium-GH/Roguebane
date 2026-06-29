namespace Roguebane.Core.Content;

// Six techniques, all interpreted by the one tick loop. Timered burst on cooldown; sustained
// output every tick. Cost is the allocation reserved while active.
public static class Techniques
{
    private static Dictionary<Attribute, int> Cost(Attribute a, int n, Attribute? b = null, int m = 0)
    {
        var d = new Dictionary<Attribute, int> { [a] = n };
        if (b is { } x) d[x] = m;
        return d;
    }

    public static readonly Technique Jab =
        new("jab", TechniqueKind.Timered, Cost(Attribute.Power, 2), Cooldown: 2, Power: 3);

    public static readonly Technique Cleave =
        new("cleave", TechniqueKind.Timered, Cost(Attribute.Power, 4), Cooldown: 4, Power: 8);

    public static readonly Technique Volley =
        new("volley", TechniqueKind.Timered, Cost(Attribute.Power, 2, Attribute.Focus, 1), Cooldown: 3, Power: 5);

    public static readonly Technique Ember =
        new("ember", TechniqueKind.Sustained, Cost(Attribute.Focus, 2), Cooldown: 0, Power: 1);

    public static readonly Technique Drain =
        new("drain", TechniqueKind.Sustained, Cost(Attribute.Focus, 3), Cooldown: 0, Power: 2);

    public static readonly Technique Ward =
        new("ward", TechniqueKind.Sustained, Cost(Attribute.Vigor, 2), Cooldown: 0, Power: 0);

    public static readonly IReadOnlyList<Technique> All =
        new[] { Jab, Cleave, Volley, Ember, Drain, Ward };
}
