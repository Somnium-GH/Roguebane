namespace Roguebane.Core;

// One encounter, interpreted from data. A control point is non-structural (focus the weakest
// foe). A castle is structural (break the front before the next layer); its boss may restore the
// standing front by its own means — a DPS race.
public sealed class Encounter
{
    public string Name { get; }

    private readonly List<Foe> _foes;
    private readonly int _restoreAmount;
    private readonly int _restoreEvery;
    private int _tick;

    public Encounter(
        string name,
        IReadOnlyList<Foe> foes,
        bool structural,
        int restoreAmount = 0,
        int restoreEvery = 0)
    {
        Name = name;
        _foes = foes.ToList();
        Structural = structural;
        _restoreAmount = restoreAmount;
        _restoreEvery = restoreEvery;
    }

    public bool Structural { get; }

    public IReadOnlyList<Foe> Foes => _foes;

    public bool Cleared => _foes.All(f => f.Down);

    public Foe? CurrentTarget
    {
        get
        {
            var alive = _foes.Where(f => !f.Down).ToList();
            if (alive.Count == 0) return null;
            if (Structural) return alive[0]; // strict front: layers fall in order
            return alive.OrderBy(f => f.Hp).ThenBy(_foes.IndexOf).First(); // focus weakest
        }
    }

    public void RallyTick()
    {
        _tick++;
        if (_restoreEvery <= 0 || _restoreAmount <= 0) return;
        if (_tick % _restoreEvery != 0) return;
        if (CurrentTarget is { } front) front.Restore(_restoreAmount);
    }
}
