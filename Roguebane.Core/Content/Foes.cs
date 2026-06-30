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

    // A structured, armed foe: HP life total + a MULTI-PART frame the player can aim at limb-by-limb.
    // Only the STR arm powers the Strike (smash it -> the strike cascades off); the head/legs/chest are
    // passive targetable structure so part-aim has real choices. No armor is fitted, so no part grants
    // evasion or plate — threat stays light, the run stays winnable (the locked LIGHT envelope).
    public static Foe Armed(string id, int hp, int arm = 2, string figure = "ogre",
        FoeAim aim = FoeAim.Random)
    {
        var frame = new Body();
        frame.Add(new BodyPart($"{id}-arm", Stat.Str, arm)); // Parts[0]: the only STR part, powers Strike
        frame.Add(new BodyPart($"{id}-head", Stat.Int, 2));
        frame.Add(new BodyPart($"{id}-legs", Stat.Dex, 2));
        frame.Add(new BodyPart($"{id}-chest", Stat.Con, 2));
        return new Foe(id, hp, frame, new[] { Strike }, figure, aim);
    }
}
