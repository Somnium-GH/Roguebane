namespace Roguebane.Core;

// Race is the ATTRIBUTE + HP source of a build (design/05, §7): a body's stat shares (STR/INT/DEX/CON,
// laid into the standard Head/Chest/Arms x2/Legs x2 anatomy) and its HP come from the Race ALONE — a
// CoreRune adds none. A Race paired with a CoreRune (which carries budget/actions/bays/apex/equipment)
// is the assembled identity the player begins a run as.
public sealed record Race(
    string Id,
    int Str,
    int Int,
    int Dex,
    int Con,
    int Hp,
    string Title = "",
    string Tag = "",    // DISPLAY-ONLY card sub-label (a race's line/flavour), e.g. "THE FOUNDER LINE"
    string Blurb = "")  // DISPLAY-ONLY card pitch
{
    public string Name => string.IsNullOrEmpty(Title) ? Id : Title;

    // The standard anatomy carrying this race's attrs: Head=INT, Chest=CON, Arms x2 split STR, Legs x2
    // split DEX. Part ids are race-keyed so a minted body's parts stay identifiable. Low scale: ~20 huge.
    public IReadOnlyList<BodyPart> BodyParts => new[]
    {
        new BodyPart($"{Id}-head", Stat.Int, Int),
        new BodyPart($"{Id}-chest", Stat.Con, Con),
        new BodyPart($"{Id}-arm-l", Stat.Str, Str / 2),
        new BodyPart($"{Id}-arm-r", Stat.Str, Str - Str / 2),
        new BodyPart($"{Id}-leg-l", Stat.Dex, Dex / 2),
        new BodyPart($"{Id}-leg-r", Stat.Dex, Dex - Dex / 2),
    };

    public Body NewBody()
    {
        var body = new Body();
        foreach (var part in BodyParts) body.Add(part);
        return body;
    }
}
