namespace Roguebane.Core;

// Race is the ATTRIBUTE + HP source of a build (design/05, §7): a body's stat shares (STR/INT/DEX/CON,
// laid into the standard Head/Chest/Arms x2/Legs x2 anatomy) and its HP come from the Race. A socketed
// CoreRune adds an additive stat bonus on top (CORE_RUNES.md) — see the bonus-taking NewBody overload
// below. A Race paired with a CoreRune (which carries budget/actions/minion capacity/Core Effect/equipment)
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

    // The standard anatomy carrying this race's attrs (plus any CoreRune bonus, folded in before the
    // arm/leg split): Head=INT, Chest=CON, Arms x2 split STR, Legs x2 split DEX. Part ids are race-keyed
    // so a minted body's parts stay identifiable. Low scale: ~20 huge.
    public IReadOnlyList<BodyPart> BodyPartsWithBonus(int strBonus, int intBonus, int dexBonus, int conBonus)
    {
        var str = Str + strBonus;
        var dex = Dex + dexBonus;
        return new[]
        {
            new BodyPart($"{Id}-head", Stat.Int, Int + intBonus),
            new BodyPart($"{Id}-chest", Stat.Con, Con + conBonus),
            new BodyPart($"{Id}-arm-l", Stat.Str, str / 2),
            new BodyPart($"{Id}-arm-r", Stat.Str, str - str / 2),
            new BodyPart($"{Id}-leg-l", Stat.Dex, dex / 2),
            new BodyPart($"{Id}-leg-r", Stat.Dex, dex - dex / 2),
        };
    }

    public IReadOnlyList<BodyPart> BodyParts => BodyPartsWithBonus(0, 0, 0, 0);

    public Body NewBody(int strBonus = 0, int intBonus = 0, int dexBonus = 0, int conBonus = 0)
    {
        var body = new Body();
        foreach (var part in BodyPartsWithBonus(strBonus, intBonus, dexBonus, conBonus)) body.Add(part);
        return body;
    }
}
