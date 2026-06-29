namespace Roguebane.Core;

// The pre-run build flow as a headless model: cycle the chassis, climb rune ladders, toggle the
// technique loadout, preview the minted body, then launch into a run. The shell renders this and
// turns input into these intents; all rules (rune economy, body minting, assembly) stay in the
// pieces it composes.
public sealed class BuildSession
{
    private readonly IReadOnlyList<Chassis> _chassis;
    private readonly IReadOnlyList<Technique> _palette;
    private readonly HashSet<string> _selected = new();
    private RuneLoadout _runes;

    public BuildSession(
        IReadOnlyList<Chassis> chassis,
        IReadOnlyList<IReadOnlyList<Mark>> paths,
        IReadOnlyList<Technique> palette)
    {
        if (chassis.Count == 0) throw new ArgumentException("a build needs at least one chassis", nameof(chassis));
        _chassis = chassis;
        Paths = paths;
        _palette = palette;
        _runes = _chassis[0].NewLoadout();
    }

    public int ChassisIndex { get; private set; }
    public int ChassisCount => _chassis.Count;
    public Chassis Chassis => _chassis[ChassisIndex];
    public RuneLoadout Runes => _runes;
    public IReadOnlyList<IReadOnlyList<Mark>> Paths { get; }
    public IReadOnlyList<Technique> Palette => _palette;

    // Cycling the chassis resets the rune allocation — a fresh budget for the new body.
    public void CycleChassis(int direction)
    {
        var n = _chassis.Count;
        ChassisIndex = ((ChassisIndex + direction) % n + n) % n;
        _runes = Chassis.NewLoadout();
        // The body changes underfoot, but the technique pick is the player's intent — keep it.
    }

    // Climb one rung up a path's ladder if the budget allows. Rungs are taken in order (no skips).
    public bool Climb(IReadOnlyList<Mark> ladder)
    {
        if (ladder.Count == 0) return false;
        var held = _runes.CurrentRank(ladder[0].Path); // 0 if none; the next rung sits at this index
        if (held >= ladder.Count) return false;
        return _runes.TryTake(ladder[held]);
    }

    public void Toggle(Technique technique)
    {
        if (!_selected.Remove(technique.Id)) _selected.Add(technique.Id);
    }

    public bool IsSelected(Technique technique) => _selected.Contains(technique.Id);

    public IReadOnlyList<Technique> Loadout =>
        _palette.Where(t => _selected.Contains(t.Id)).ToList();

    // The body as it stands now: chassis parts plus everything the allocated runes grant.
    public Body Preview() => Chassis.NewBody(_runes);

    public Session Launch(Run run) => Forge.Assemble(Chassis, _runes, Loadout, run);

    // Launch into the real map+combat loop: mint the chosen body and embark on the leg.
    public Expedition Embark(RunMap map) => Forge.Embark(Chassis, _runes, Loadout, map);

    // March the whole campaign: the chosen body carries through every leg to the Capital.
    public Campaign March(IReadOnlyList<Func<RunMap>> legs) =>
        Forge.EmbarkCampaign(Chassis, _runes, Loadout, legs);
}
