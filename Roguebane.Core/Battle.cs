namespace Roguebane.Core;

public enum BattleOutcome
{
    Ongoing,
    Cleared,
    Fled,
}

// Drives one encounter on the fixed tick: the boss rallies first, the caster focuses the current
// target, then acts. Fleeing ends the battle without clearing it.
public sealed class Battle
{
    private readonly Caster _caster;
    private readonly Encounter _encounter;
    private readonly Support _support;

    public Battle(Caster caster, Encounter encounter)
    {
        _caster = caster;
        _encounter = encounter;
        _support = new Support(encounter.SupportAmount, encounter.SupportEvery);
    }

    public BattleOutcome Outcome { get; private set; } = BattleOutcome.Ongoing;

    public void Step()
    {
        if (Outcome != BattleOutcome.Ongoing) return;

        _encounter.BossRestoreTick();
        if (_encounter.CurrentTarget is { } target)
        {
            _caster.Retarget(target);
            var rallied = _support.Fire();           // player's banked auto-fire lands on the front
            if (rallied > 0) target.Damage(rallied);
        }

        _caster.Step();

        if (_encounter.Cleared) Outcome = BattleOutcome.Cleared;
    }

    public void Flee()
    {
        if (Outcome == BattleOutcome.Ongoing) Outcome = BattleOutcome.Fled;
    }
}
