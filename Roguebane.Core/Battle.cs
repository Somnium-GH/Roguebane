namespace Roguebane.Core;

public enum BattleOutcome
{
    Ongoing,
    Cleared,
    Fled,
    Lost,
}

// Drives one encounter on the fixed tick. The player side: the boss rallies its front, banked
// support auto-fires, then the player's caster acts on the current target. The foe side (one combat
// grammar): every structured, armed foe runs its OWN caster against the player — and because that
// caster reserves stats on the foe's Frame, smashing the foe's parts cascades its attacks off, the
// mirror of the player's own degradation. The player falling (HP 0) loses the battle.
public sealed class Battle
{
    private readonly Caster _caster;
    private readonly Encounter _encounter;
    private readonly Support _support;
    private readonly Fighter? _player;
    private readonly List<(Foe Foe, Caster Caster)> _foeOffense = new();

    private readonly Rng _rng;

    public Battle(Caster caster, Encounter encounter, Fighter? player = null, ulong seed = 1)
    {
        _caster = caster;
        _encounter = encounter;
        _support = new Support(encounter.SupportAmount, encounter.SupportEvery);
        _player = player;
        _rng = new Rng(seed);          // one shared stream keeps the whole fight reproducible
        _caster.UseRng(_rng);

        if (_player is null) return;
        var foe = encounter.Enemy;
        if (foe.Frame is not null && foe.Arsenal.Count > 0)
        {
            var offense = new Caster(foe.Frame, _player, foeEffect: foe.Effect);
            offense.UseRng(_rng);
            foreach (var tech in foe.Arsenal) offense.Activate(tech); // foes fire unattended (auto on)
            _foeOffense.Add((foe, offense));
        }
    }

    public BattleOutcome Outcome { get; private set; } = BattleOutcome.Ongoing;

    // The encounter under way — the render shell reads its one Enemy to paint combat.
    public Encounter Encounter => _encounter;

    public void Step()
    {
        if (Outcome != BattleOutcome.Ongoing) return;

        if (_encounter.Enemy is { Down: false } target)
        {
            _caster.Retarget(target);
            var rallied = _support.Fire();           // player's banked auto-fire lands on the front
            if (rallied > 0) target.Damage(rallied);
        }

        _caster.Step();

        // The foes strike back: each standing armed foe acts on the player. Where the encounter opts
        // into part-aim (§8), a foe erodes a player PART per its TARGETING PERSONALITY — re-picked every
        // tick so RANDOM varies and SMART/INEPT track the eroding body; no standing part left => the
        // swing spills onto HP. Otherwise it chips restorable HP (the staged-default until part-heals).
        if (_player is { Down: false })
        {
            foreach (var (foe, offense) in _foeOffense)
            {
                if (foe.Down) continue;
                var part = _encounter.FoePartAim ? FoeTargeting.Pick(foe.Aim, _player.Body, _rng) : null;
                foreach (var tech in foe.Arsenal)
                    if (part is not null) offense.Aim(tech, _player, part); else offense.ClearAim(tech);
                offense.Step();
            }
            _player.CapToMax(); // a chest hit this tick may have lowered MaxHp; persist the cap
            if (_player.Down) { Outcome = BattleOutcome.Lost; return; }
        }

        if (_encounter.Cleared) Outcome = BattleOutcome.Cleared;
    }

    public void Retreat()
    {
        if (Outcome == BattleOutcome.Ongoing) Outcome = BattleOutcome.Fled;
    }
}
