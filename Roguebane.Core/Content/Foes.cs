namespace Roguebane.Core.Content;

// Lightly-armed content foes: a small Frame powers a weak Arsenal so combat actually happens and
// can chip the player — but threat stays low and runs stay winnable (the focus is dwell/visibility,
// not difficulty; the real power envelope is a later balance pass). A foe's strike reserves STR on
// its own arm, so smashing the arm cascades the attack off — the same body rule as the player.
public static class Foes
{
    // Cooldown in combat ticks at the 10/sec clock: a foe swings ~ every 5s for 1.
    private static readonly Technique Strike =
        new("foe-strike", Stat.Str, Reserve: 1, TechniqueKind.Timered, Cooldown: 50, Power: 1);

    // A structured, armed foe: HP life total + a one-arm STR frame + the weak strike.
    public static Foe Armed(string id, int hp, int arm = 2)
    {
        var frame = new Body();
        frame.Add(new BodyPart($"{id}-arm", Stat.Str, arm));
        return new Foe(id, hp, frame, new[] { Strike });
    }
}
