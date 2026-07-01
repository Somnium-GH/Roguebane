namespace Roguebane.Core;

// One encounter = ONE enemy (canon, §8/§13): a single structured, possibly multi-PART foe (a human,
// a creature, or the castle boss). The only targeting is PART aim within that one enemy — there is no
// multi-foe list or front. A boss sustains itself ONLY through a real heal TECHNIQUE in its Arsenal
// (run by its own offense caster, §8 symmetry) — there is NO free HP-regen tick. Callers read `Enemy`
// (the one foe) and gate on `Enemy.Down` where they need a live target.
public sealed class Encounter
{
    public string Name { get; }

    private readonly Foe _foe;

    public Encounter(
        string name,
        Foe foe,
        int supportAmount = 0,
        int supportEvery = 0,
        bool foePartAim = false)
    {
        Name = name;
        _foe = foe;
        SupportAmount = supportAmount;
        SupportEvery = supportEvery;
        FoePartAim = foePartAim;
    }

    // The one enemy of this encounter.
    public Foe Enemy => _foe;

    // Whether the foe erodes the player's PARTS (§8) instead of chipping restorable HP.
    public bool FoePartAim { get; }

    // Player-allied rallied support available at this encounter (auto-fire on the enemy).
    public int SupportAmount { get; }
    public int SupportEvery { get; }

    public bool Cleared => _foe.Down;
}
