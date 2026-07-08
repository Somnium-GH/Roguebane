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

    // CHUNK D item 1's last piece (STATUS.md, §8 symmetry): the first FOES.md roster foe with a real
    // Foe Effect wired, not just gear. FOES.md T1 Wraith: no weapon, no armor — its INT part (not an
    // arm) powers Ember, and Insubstantial (Caster.Hit) reads that same part live, so smashing it both
    // silences Ember (the shared DisabledGear-style reserve cascade) AND permanently breaks the
    // damage-reduction — one part, two designed consequences, no bespoke foe-only bookkeeping.
    public static Foe Wraith(string id, int hp = 10, int headInt = 4, string figure = "wraith",
        FoeAim aim = FoeAim.Random)
    {
        var frame = new Body();
        frame.Add(new BodyPart($"{id}-arm", Stat.Str, 1));
        frame.Add(new BodyPart($"{id}-head", Stat.Int, headInt)); // Parts[1]: powers Ember, gates Insubstantial
        frame.Add(new BodyPart($"{id}-legs", Stat.Dex, 2));
        frame.Add(new BodyPart($"{id}-chest", Stat.Con, 2));
        return new Foe(id, hp, frame, new[] { Techniques.Ember }, figure, aim, FoeEffectKind.Insubstantial);
    }

    // CHUNK D item 2's fourth roster foe (FOES.md): the DPS-race teacher. Real Iron Axe (Swing-consulted,
    // §8 symmetry, same as Ogre) PLUS a real self-mend (Bandage, CON-reserved off its own chest) run by
    // its own offense caster -- Battle passes RegenerativeFlesh through as that caster's foeEffect, so
    // the mend doubles automatically (Caster.cs, proven bare in TrollRegenerativeFleshTests). No separate
    // "chest still whole" gate needed: Bandage's own CON reservation cascade already silences it if the
    // chest breaks below Reserve 2 -- FOES.md's "break the chest first" lesson, for free.
    public static Foe Troll(string id, int hp = 16, int arm = 4, string figure = "troll",
        FoeAim aim = FoeAim.Random)
    {
        var frame = new Body();
        frame.Add(new BodyPart($"{id}-arm", Stat.Str, arm));  // Parts[0]: wields the axe, powers Swing
        frame.Add(new BodyPart($"{id}-head", Stat.Int, 1));
        frame.Add(new BodyPart($"{id}-legs", Stat.Dex, 2));
        frame.Add(new BodyPart($"{id}-chest", Stat.Con, 4));  // Parts[3]: powers Bandage, doubled by RegenerativeFlesh
        frame.Wield(Armory.Axes[0]); // Iron Axe: Reserve 1 Str, fits arm STR 4 with headroom to spare
        return new Foe(id, hp, frame, new[] { Armory.Swing, Techniques.Bandage }, figure, aim,
            FoeEffectKind.RegenerativeFlesh);
    }

    // CHUNK D item 2's fifth roster foe (FOES.md, Gargoyle): "no weapon (stone fists" reads as an
    // unbuilt Weapon record, but FOES.md itself qualifies it "~ Iron Axe profile" -- and its arsenal
    // (Jab, Stat.Str/Consults:Primary) matches Iron Axe's own Stat.Str exactly, unlike Skeleton's
    // Dagger(Dex)/Jab(Str) mismatch. Wielding Armory.Axes[0] narratively AS the stone fists is the same
    // trick Ogre/Troll already use for their own weapon fluff -- no new Weapon record needed, no engine
    // work; the earlier "no real Weapon record" blocker note undersold it. Stoneform (already proven
    // bare in GargoyleStoneformTests) reads the CON chest LIVE in Caster.Hit, not a reservation -- so
    // it needs no wiring here either, just a chest part with headroom to spare.
    public static Foe Gargoyle(string id, int hp = 12, int arm = 3, string figure = "gargoyle",
        FoeAim aim = FoeAim.Random)
    {
        var frame = new Body();
        frame.Add(new BodyPart($"{id}-arm", Stat.Str, arm));  // Parts[0]: wields the "fists," powers Jab
        frame.Add(new BodyPart($"{id}-head", Stat.Int, 2));
        frame.Add(new BodyPart($"{id}-legs", Stat.Dex, 1));
        frame.Add(new BodyPart($"{id}-chest", Stat.Con, 4));  // Parts[3]: Stoneform's live "still whole" read
        frame.Wield(Armory.Axes[0]); // Iron Axe: Reserve 1 Str, fits arm STR 3 with headroom to spare
        return new Foe(id, hp, frame, new[] { Techniques.Jab }, figure, aim, FoeEffectKind.Stoneform);
    }

    // CHUNK D item 2's sixth roster foe (FOES.md, Skeleton): the fodder chassis, and the roster's ONE
    // deliberate weapon/technique stat MISMATCH (Iron Dagger is Stat.Dex; Jab is Stat.Str) -- NOTED not
    // fixed (FOES.md "Weapon/technique stat mismatch" section, Doug 2026-07-07): leave it exactly as
    // FOES.md specifies. Verified (FoeSkeletonTests, correcting the note's own technical premise):
    // Caster.Activate's `Consulted(technique).Count == 0 => return false` gate (Caster.cs) fires BEFORE
    // Body.Activate ever runs, for ANY Consults != None technique with nothing to consult -- so Jab
    // never activates at all here, not "activates for 0 weapon-scaled damage" as first assumed. Skeleton
    // ships with zero offense as a result; Brittle (proven bare in BrittleEffectTests) is wired anyway
    // since it's a defensive/part-aim reward, independent of whether Jab ever fires.
    public static Foe Skeleton(string id, int hp = 8, string figure = "skeleton", FoeAim aim = FoeAim.Random)
    {
        var frame = new Body();
        frame.Add(new BodyPart($"{id}-arm", Stat.Str, 2));  // Parts[0]: Jab's Reserve, never Consulted (Dagger is Dex)
        frame.Add(new BodyPart($"{id}-head", Stat.Int, 1));
        frame.Add(new BodyPart($"{id}-legs", Stat.Dex, 2));
        frame.Add(new BodyPart($"{id}-chest", Stat.Con, 1));
        frame.Wield(Armory.Daggers[0]); // Iron Dagger: Stat.Dex -- never Consulted by the Str-keyed Jab
        return new Foe(id, hp, frame, new[] { Techniques.Jab }, figure, aim, FoeEffectKind.Brittle);
    }

    // CHUNK D item 2's seventh roster foe (FOES.md, Bandit): the first SHIELDED foe -- a real Brace
    // (Sustained, CON) run by the foe's own offense Caster alongside Swing, same as the player gets a
    // shield pool. Chest CON raised 2->3 (Doug 2026-07-07, the Foe attribute-for-equipment rule) to fit
    // Wooden Shield's 1 CON equip + Brace's 2 CON reserve exactly (3/3, no headroom -- a deliberately
    // tight fit per the ruling, not a mistake). Plunder is STUBBED (Doug 2026-07-07): ships as
    // FoeEffectKind.None, not the cross-Caster drain wiring FOES.md parks for later.
    public static Foe Bandit(string id, int hp = 12, string figure = "bandit", FoeAim aim = FoeAim.Random)
    {
        var frame = new Body();
        frame.Add(new BodyPart($"{id}-arm", Stat.Str, 3));   // Parts[0]: wields the axe, powers Swing
        frame.Add(new BodyPart($"{id}-head", Stat.Int, 2));
        frame.Add(new BodyPart($"{id}-legs", Stat.Dex, 3));
        frame.Add(new BodyPart($"{id}-chest", Stat.Con, 3)); // Parts[3]: Wooden Shield (1) + Brace (2) -- exact fit
        frame.Wield(Armory.Axes[0]);           // Iron Axe: Reserve 1 Str, fits arm STR 3 with headroom
        frame.Wield(Armory.Shields[0]);        // Wooden Shield: Reserve 1 Con
        frame.Equip(ArmorLines.LeatherChest[0]); // Padded Armor: Leather line, Governing Dex, Requirement 1 -- fits legs DEX 3
        return new Foe(id, hp, frame, new[] { Armory.Swing, Techniques.Brace }, figure, aim, FoeEffectKind.None);
    }

    // CHUNK D item 2's eighth roster foe (FOES.md, Dire Ogre T2): arm STR raised 5->8 (Doug 2026-07-07,
    // the Foe attribute-for-equipment rule) -- Iron Warhammer's 5 STR + STR Breastplate's 2 STR = 7
    // against the old 5-STR arm left zero headroom; 8 gives real T2 headroom per Doug's "up it
    // especially since we're calling that a T2 foe." Overwhelm is STUBBED (Doug 2026-07-07): ships as
    // FoeEffectKind.None, not the shield-pool-drain-on-evade wiring FOES.md parks for later.
    public static Foe DireOgre(string id, int hp = 20, string figure = "ogre", FoeAim aim = FoeAim.Smart)
    {
        var frame = new Body();
        frame.Add(new BodyPart($"{id}-arm", Stat.Str, 8));   // Parts[0]: wields the warhammer, powers Swing+Cleave
        frame.Add(new BodyPart($"{id}-head", Stat.Int, 1));
        frame.Add(new BodyPart($"{id}-legs", Stat.Dex, 2));
        frame.Add(new BodyPart($"{id}-chest", Stat.Con, 4));
        frame.Wield(Armory.Warhammers[0]);          // Iron Warhammer: Reserve 5 Str, 2H
        frame.Equip(ArmorLines.PlateChest[0]);      // Iron Breastplate: Plate line, Governing Str, Requirement 2
        return new Foe(id, hp, frame, new[] { Armory.Swing, Techniques.Cleave }, figure, aim, FoeEffectKind.None);
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
