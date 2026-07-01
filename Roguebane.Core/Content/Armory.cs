namespace Roguebane.Core.Content;

// Weapons + the verbs that consult them. Weapons are stat-sticks (zero abilities); a consulting
// technique sources its cost and power from whatever is wielded. Kept apart from Techniques.All so
// the self-contained palette stays the default.
public static class Armory
{
    public static readonly Weapon Sword = new("sword", Stat.Str, Reserve: 3, Power: 3);
    public static readonly Weapon Axe = new("axe", Stat.Str, Reserve: 4, Power: 4);
    public static readonly Weapon Dagger = new("dagger", Stat.Dex, Reserve: 1, Power: 1);

    // A DEX stat-stick that strikes from range: its consulting verb (Shot) IGNORES shields (§6b).
    public static readonly Weapon Bow = new("bow", Stat.Dex, Reserve: 2, Power: 2);

    // Swing consults the primary STR weapon; Frenzy consults BOTH (cost = sum of their reserves).
    public static readonly Technique Swing =
        new("swing", Stat.Str, Reserve: 0, TechniqueKind.Timered, Cooldown: 2, Power: 0, Consults: WeaponUse.Primary);

    public static readonly Technique Frenzy =
        new("frenzy", Stat.Str, Reserve: 0, TechniqueKind.Timered, Cooldown: 3, Power: 0, Consults: WeaponUse.Both);

    // Shot looses the primary DEX weapon (the BOW): it BYPASSES the shield pool and spends 1 Charge per
    // loose (§6b Charge = the shield-pierce resource); dry => it holds. Power/cost come from the bow.
    public static readonly Technique Shot =
        new("shot", Stat.Dex, Reserve: 0, TechniqueKind.Timered, Cooldown: 3, Power: 0,
            Consults: WeaponUse.Primary, ChargeCost: 1, ShieldPiercing: true);

    public static readonly IReadOnlyList<Weapon> All = new[] { Sword, Axe, Dagger, Bow };
}
