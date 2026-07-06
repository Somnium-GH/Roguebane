namespace Roguebane.Core;

// The pre-run build flow as a headless model: cycle the chassis, climb rune ladders, toggle the
// technique equipment, preview the minted body, then launch into a run. The shell renders this and
// turns input into these intents; all rules (rune economy, body minting, assembly) stay in the
// pieces it composes.
public sealed class BuildSession
{
    private readonly IReadOnlyList<Race> _races;
    private readonly IReadOnlyList<CoreRune> _chassis;
    private readonly HashSet<string> _selected = new();
    private RuneLoadout _runes;

    public BuildSession(
        IReadOnlyList<Race> races,
        IReadOnlyList<CoreRune> chassis,
        IReadOnlyList<IReadOnlyList<Mark>> paths)
    {
        if (races.Count == 0) throw new ArgumentException("a build needs at least one race", nameof(races));
        if (chassis.Count == 0) throw new ArgumentException("a build needs at least one chassis", nameof(chassis));
        _races = races;
        _chassis = chassis;
        Paths = paths;
        _runes = _chassis[0].NewLoadout();
        SeedKit();
    }

    // The current chassis ships a FIXED starting equipment (data) — pre-slot it so the bar is never
    // empty and Launch needs no "pick a technique" gate. Every kit technique is on the Palette by
    // construction, so no filtering is needed here (unlike the old external-palette intersection).
    private void SeedKit()
    {
        _selected.Clear();
        foreach (var t in CoreRune.Kit)
            _selected.Add(t.Id);
    }

    public int CoreRuneIndex { get; private set; }
    public int CoreRuneCount => _chassis.Count;
    public CoreRune CoreRune => _chassis[CoreRuneIndex];
    public IReadOnlyList<CoreRune> Roster => _chassis; // the whole line-up, for the New Run core grid
    public RuneLoadout Runes => _runes;
    public IReadOnlyList<IReadOnlyList<Mark>> Paths { get; }

    // What's actually available to slot: the chassis's fixed kit plus whatever the runes taken so far
    // grant (§7a). NOT the whole game's technique roster — a build can only ever field what its core
    // and climbed ladders actually unlock.
    public IReadOnlyList<Technique> Palette => CoreRune.Kit.Concat(_runes.GrantedTechniques).ToList();

    // The chosen Race supplies the body's attrs + HP (§7) — the NewGame two-step's first column.
    public int RaceIndex { get; private set; }
    public int RaceCount => _races.Count;
    public Race Race => _races[RaceIndex];
    public IReadOnlyList<Race> RaceRoster => _races;

    // Cycling the race swaps the body's attrs only; the core keeps its budget + fixed kit, so the rune
    // allocation and slotted techniques are untouched (all race<->core combos are allowed, design/05).
    public void CycleRace(int direction)
    {
        var n = _races.Count;
        RaceIndex = ((RaceIndex + direction) % n + n) % n;
    }

    // Cycling the chassis resets the rune allocation — a fresh budget for the new body.
    public void CycleCoreRune(int direction)
    {
        var n = _chassis.Count;
        CoreRuneIndex = ((CoreRuneIndex + direction) % n + n) % n;
        _runes = CoreRune.NewLoadout();
        SeedKit(); // a fresh chassis brings its own fixed kit
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

    public IReadOnlyList<Technique> Equipment =>
        Palette.Where(t => _selected.Contains(t.Id)).ToList();

    // The body as it stands now: the race's anatomy/attrs plus everything the allocated runes grant.
    public Body Preview() => CoreRune.NewBody(Race, _runes);

    public Session Launch(Run run) => Forge.Assemble(Race, CoreRune, _runes, Equipment, run);

    // Launch into the real map+combat loop: mint the chosen body and embark on the leg.
    public Expedition Embark(CityMap map) => Forge.Embark(Race, CoreRune, _runes, Equipment, map);

    // March the whole campaign: the chosen body carries through every leg to the Capital.
    public Campaign Redeploy(IReadOnlyList<Func<CityMap>> legs) =>
        Forge.EmbarkCampaign(Race, CoreRune, _runes, Equipment, legs);
}
