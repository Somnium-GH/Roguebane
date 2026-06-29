namespace Roguebane.Core.Content;

// Six techniques on the four-stat model, all interpreted by the one tick loop. Low-scale reserve
// and power. STR swings, DEX strikes, INT spells (silenced if the head drains), CON braces (a
// defensive hold — reserves CON for block, deals no damage).
public static class Techniques
{
    public static readonly Technique Jab =
        new("jab", Stat.Str, Reserve: 1, TechniqueKind.Timered, Cooldown: 2, Power: 2);

    public static readonly Technique Cleave =
        new("cleave", Stat.Str, Reserve: 3, TechniqueKind.Timered, Cooldown: 4, Power: 4);

    public static readonly Technique Lunge =
        new("lunge", Stat.Dex, Reserve: 1, TechniqueKind.Timered, Cooldown: 3, Power: 2);

    public static readonly Technique Ember =
        new("ember", Stat.Int, Reserve: 1, TechniqueKind.Sustained, Cooldown: 0, Power: 1);

    public static readonly Technique Drain =
        new("drain", Stat.Int, Reserve: 2, TechniqueKind.Sustained, Cooldown: 0, Power: 2);

    public static readonly Technique Brace =
        new("brace", Stat.Con, Reserve: 2, TechniqueKind.Sustained, Cooldown: 0, Power: 0);

    public static readonly IReadOnlyList<Technique> All =
        new[] { Jab, Cleave, Lunge, Ember, Drain, Brace };
}
