namespace Roguebane.Core;

public enum SessionState
{
    Fighting,
    Won,
    Fled,
}

// The playable wiring the render shell reads: a player, a loadout, and a run whose encounters
// are fought one at a time. All rules stay in the pieces it composes — Session only sequences
// them on the fixed tick and exposes intents (toggle technique, flee, pause) for input to call.
public sealed class Session
{
    private readonly Caster _caster;
    private readonly IReadOnlyList<Technique> _loadout;

    public Entity Player { get; }
    public Run Run { get; }
    public Battle Battle { get; private set; }
    public bool Paused { get; private set; }
    public SessionState State { get; private set; } = SessionState.Fighting;

    public Session(Entity player, Caster caster, IReadOnlyList<Technique> loadout, Run run)
    {
        Player = player;
        _caster = caster;
        _loadout = loadout;
        Run = run;
        Battle = new Battle(caster, run.Current);
    }

    public IReadOnlyList<Technique> Loadout => _loadout;

    public bool IsActive(Technique technique) => _caster.IsActive(technique);

    public void TogglePause() => Paused = !Paused;

    public void Toggle(Technique technique)
    {
        if (_caster.IsActive(technique)) _caster.Deactivate(technique);
        else _caster.Activate(technique);
    }

    public void Flee()
    {
        if (State != SessionState.Fighting) return;
        Battle.Flee();
        State = SessionState.Fled;
    }

    public void Tick()
    {
        if (Paused || State != SessionState.Fighting) return;

        Battle.Step();
        if (Battle.Outcome != BattleOutcome.Cleared) return;

        if (Run.Completed) State = SessionState.Won;
        else if (Run.TryAdvance()) Battle = new Battle(_caster, Run.Current);
    }
}
