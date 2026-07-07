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

    // CHUNK D item 1 (STATUS.md, §8 symmetry): a GEARED foe that WIELDS a real Weapon and fights with a
    // real weapon-consulting verb (Consults: Primary) instead of a flat hardcoded Power -- the SAME
    // Body.Wield/Consulted/DisabledGear paths the player uses, so its damage scales off the actual
    // Weapon record and a smashed weapon-arm silences it via the shared disable cascade (no foe-special-
    // cased damage math). FOES.md T1 Ogre is the first case: Iron Mace + Swing. Armor-wiring needs no
    // extra engine work either (Body.Equip/ArmorSustained are already foe-agnostic) -- the remaining
    // FOES.md roster, Foe Effects (data + interpreter), and encounter-table wiring stay open (item 2-4).
    public static Foe Ogre(string id, int hp = 14, int arm = 4, string figure = "ogre",
        FoeAim aim = FoeAim.Random)
    {
        var frame = new Body();
        frame.Add(new BodyPart($"{id}-arm", Stat.Str, arm));  // Parts[0]: wields the mace, powers Swing
        frame.Add(new BodyPart($"{id}-head", Stat.Int, 1));
        frame.Add(new BodyPart($"{id}-legs", Stat.Dex, 2));
        frame.Add(new BodyPart($"{id}-chest", Stat.Con, 3));
        frame.Wield(Armory.Maces[0]); // Iron Mace: Reserve 3 Str, fits arm STR 4 with headroom to spare
        return new Foe(id, hp, frame, new[] { Armory.Swing }, figure, aim);
    }

    // The castle boss's heavier strike: a real timered attack (STR arm), harder + faster than a raider's.
    private static readonly Technique BossStrike =
        new("boss-strike", Stat.Str, Reserve: 1, TechniqueKind.Timered, Cooldown: 25, Power: 3);

    // A structured boss that MENDS itself through a REAL technique (§8 symmetry, never a free HP tick):
    // its Arsenal carries the §10 part-heal (Bandage) alongside BossStrike, run by its OWN offense caster
    // — so smashing its arm is answered by a mend, sustaining its offense. A slow build takes too many
    // strikes to break it in time; a fast (and shielded) build clears it first. Numbers placeholder.
    public static Foe ArmedHealing(string id, int hp, int arm = 4, string figure = "ogre",
        FoeAim aim = FoeAim.Smart)
    {
        var frame = new Body();
        frame.Add(new BodyPart($"{id}-arm", Stat.Str, arm));  // powers BossStrike
        frame.Add(new BodyPart($"{id}-head", Stat.Int, 2));
        frame.Add(new BodyPart($"{id}-legs", Stat.Dex, 2));
        frame.Add(new BodyPart($"{id}-chest", Stat.Con, 3));  // powers the mend (Bandage reserves CON)
        return new Foe(id, hp, frame, new[] { BossStrike, Techniques.Bandage }, figure, aim);
    }
}
