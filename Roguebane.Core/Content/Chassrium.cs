namespace Roguebane.Core.Content;

public static class Chassrium
{
    // Low base, fat budget, cheap runes — built for nothing in particular, can climb anywhere.
    public static readonly Chassis Grunt = new(
        "grunt",
        new Dictionary<Attribute, int> { [Attribute.Power] = 6 },
        RuneBudget: 24,
        RuneDiscount: 1);

    // High base, tight budget — built around the Resonance ladder it can just afford.
    public static readonly Chassis Adept = new(
        "adept",
        new Dictionary<Attribute, int> { [Attribute.Focus] = 14 },
        RuneBudget: 10,
        RuneDiscount: 0);
}
