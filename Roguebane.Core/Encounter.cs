namespace Roguebane.Core;

// One encounter = ONE enemy (canon, §8/§13): a single structured, possibly multi-PART foe (a human,
// a creature, or the castle boss). The only targeting is PART aim within that one enemy — there is no
// multi-foe list or front. A boss may restore its own parts/HP by its own means (a DPS race). Callers
// read `Enemy` (the one foe) and gate on `Enemy.Down` where they need a live target.
public sealed class Encounter
{
    public string Name { get; }

    private readonly Foe _foe;
    private readonly int _restoreAmount;
    private readonly int _restoreEvery;
    private int _tick;

    public Encounter(
        string name,
        Foe foe,
        int restoreAmount = 0,
        int restoreEvery = 0,
        int supportAmount = 0,
        int supportEvery = 0,
        bool foePartAim = false)
    {
        Name = name;
        _foe = foe;
        _restoreAmount = restoreAmount;
        _restoreEvery = restoreEvery;
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

    // The boss restoring itself by its own means (self-repair) — distinct from player rallied support.
    public void BossRestoreTick()
    {
        _tick++;
        if (_restoreEvery <= 0 || _restoreAmount <= 0) return;
        if (_tick % _restoreEvery != 0) return;
        if (!_foe.Down) _foe.Restore(_restoreAmount);
    }
}
