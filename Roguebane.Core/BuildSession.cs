namespace Roguebane.Core;

// The pre-run build flow as a headless model: cycle the chassis, climb rune ladders, toggle the
// technique equipment, preview the minted body, then launch into a run. The shell renders this and
// turns input into these intents; all rules (rune economy, body minting, assembly) stay in the
// pieces it composes.
public sealed class BuildSession
{
    private readonly IReadOnlyList<CoreRune> _chassis;
    private readonly IReadOnlyList<Technique> _palette;
    private readonly HashSet<string> _selected = new();
    private RuneLoadout _runes;

    // The chosen Race supplies attrs + HP (§7). Race selection (the NewGame two-step) is not wired to
    // input yet, so default to Human; the shell sets it once the race column lands.
    public Race Race { get; set; } = Content.Races.Human;

    public BuildSession(
        IReadOnlyList<CoreRune> chassis,
        IReadOnlyList<IReadOnlyList<Mark>> paths,
        IReadOnlyList<Technique> palette)
    {
        if (chassis.Count == 0) throw new ArgumentException("a build needs at least one chassis", nameof(chassis));
        _chassis = chassis;
        Paths = paths;
        _palette = palette;
        _runes = _chassis[0].NewLoadout();
        SeedKit();
    }

    // The current chassis ships a FIXED starting equipment (data) — pre-slot it so the bar is never
    // empty and Launch needs no "pick a technique" gate. Only techniques on the palette are slotted.
    private void SeedKit()
    {
        _selected.Clear();
        var ids = _palette.Select(t => t.Id).ToHashSet(StringComparer.Ordinal);
        foreach (var t in CoreRune.Kit)
            if (ids.Contains(t.Id)) _selected.Add(t.Id);
    }

    public int CoreRuneIndex { get; private set; }
    public int CoreRuneCount => _chassis.Count;
    public CoreRune CoreRune => _chassis[CoreRuneIndex];
    public IReadOnlyList<CoreRune> Roster => _chassis; // the whole line-up, for the New Run core grid
    public RuneLoadout Runes => _runes;
    public IReadOnlyList<IReadOnlyList<Mark>> Paths { get; }
    public IReadOnlyList<Technique> Palette => _palette;

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
        _palette.Where(t => _selected.Contains(t.Id)).ToList();

    // The body as it stands now: the race's anatomy/attrs plus everything the allocated runes grant.
    public Body Preview() => CoreRune.NewBody(Race, _runes);

    public Session Launch(Run run) => Forge.Assemble(Race, CoreRune, _runes, Equipment, run);

    // Launch into the real map+combat loop: mint the chosen body and embark on the leg.
    public Expedition Embark(CityMap map) => Forge.Embark(Race, CoreRune, _runes, Equipment, map);

    // March the whole campaign: the chosen body carries through every leg to the Capital.
    public Campaign Redeploy(IReadOnlyList<Func<CityMap>> legs) =>
        Forge.EmbarkCampaign(Race, CoreRune, _runes, Equipment, legs);
}
