namespace Roguebane.Core;

// FOES.md "Foe-effect design rules": ONE effect per foe, built ONLY from designed mechanics (here:
// live part damage via Body.Damaged, same read Body.ArmorSustained/DisabledGear already use). Data
// (this enum) + the one interpreter site (Caster.Hit) -- CLAUDE.md's "one code path interprets
// data." Only the effects actually wired to a foe belong here; the rest of FOES.md's roster stays
// unbuilt (STATUS.md) rather than adding dead enum cases ahead of the content that needs them.
public enum FoeEffectKind
{
    None,
    Insubstantial, // Wraith: while its INT part is undamaged, a landed hit deals 1 less HP damage (min 1); breaks on the first hit that damages that part.
    Brittle, // Skeleton: the first hit that breaks this foe's STR (arm) part refunds the attacker's aimed Timered technique's cooldown (ready to fire again immediately). One-shot per foe (Foe.EffectTriggered).
    Stoneform, // Gargoyle: while its CON part (chest) is undamaged, a landed hit deals 1 less PART damage (min 1) to whatever part it's aimed at; HP still lands full. Gone for good once the chest takes any damage.
    RegenerativeFlesh, // Troll: its mend (Bandage/Suture) restores DOUBLE the part-points instead of the base amount. Break its CON part (chest) to drop its CON below the mend's Reserve -- the existing reservation cascade silences the mend outright, same as breaking any other consulted stat.
}
