namespace Roguebane.Core.Content;

public static class Builds
{
    public static readonly BuildSpec AllSix = new("all-six", Techniques.All);

    public static readonly BuildSpec PowerLine =
        new("power-line", new[] { Techniques.Jab, Techniques.Cleave, Techniques.Lunge });

    public static readonly BuildSpec Sustainers =
        new("sustainers", new[] { Techniques.Ember, Techniques.Drain, Techniques.Brace });

    public static readonly BuildSpec GlassEmber =
        new("glass-ember", new[] { Techniques.Ember });

    public static readonly IReadOnlyList<BuildSpec> Sweep =
        new[] { AllSix, PowerLine, Sustainers, GlassEmber };
}
