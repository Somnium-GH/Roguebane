namespace Roguebane.Core.Content;

// Six techniques on the four-stat model, all interpreted by the one tick loop. Cooldowns are in
// COMBAT TICKS at the fixed 10 ticks/sec clock (value/10 = seconds): weak attacks ~4-6s, strong
// ~12-15s, small damage (1-3) so a fight runs 30s+ and stays watchable. DEX further shortens these
// at cast time (haste). Brace alone is Sustained (a held reservation, power 0 — the CON block); the
// damage techniques auto-repeat on their real-second cadence. STR swings, DEX strikes, INT bolts
// (silenced if the head drains), CON braces.
public static class Techniques
{
    public static readonly Technique Jab =
        new("jab", Stat.Str, Reserve: 1, TechniqueKind.Timered, Cooldown: 50, Power: 2);    // ~5s

    public static readonly Technique Cleave =
        new("cleave", Stat.Str, Reserve: 3, TechniqueKind.Timered, Cooldown: 140, Power: 3); // ~14s

    public static readonly Technique Lunge =
        new("lunge", Stat.Dex, Reserve: 1, TechniqueKind.Timered, Cooldown: 45, Power: 2);  // ~4.5s

    public static readonly Technique Ember =
        new("ember", Stat.Int, Reserve: 1, TechniqueKind.Timered, Cooldown: 30, Power: 1);  // ~3s bolt

    public static readonly Technique Drain =
        new("drain", Stat.Int, Reserve: 2, TechniqueKind.Timered, Cooldown: 60, Power: 2);  // ~6s

    public static readonly Technique Brace =
        new("brace", Stat.Con, Reserve: 2, TechniqueKind.Sustained, Cooldown: 0, Power: 0); // held block

    public static readonly IReadOnlyList<Technique> All =
        new[] { Jab, Cleave, Lunge, Ember, Drain, Brace };
}
